using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using QRCoder;

namespace FileShare
{
    public class HttpServer
    {
        private readonly int _port;
        private readonly string _localUrl;
        private readonly FileService _fileService;
        private readonly MessageService _messageService;
        private readonly JsonSerializerOptions _jsonOptions;
        private HttpListener _listener;
        private bool _networkSharingEnabled;

        public string LocalUrl => _localUrl;
        public bool NetworkSharingEnabled => _networkSharingEnabled;

        public HttpServer(int port, FileService fileService, MessageService messageService, JsonSerializerOptions jsonOptions)
        {
            _port = port;
            _localUrl = $"http://localhost:{_port}";
            _fileService = fileService;
            _messageService = messageService;
            _jsonOptions = jsonOptions;
            _listener = StartHttpListener(out _networkSharingEnabled);
        }

        public void StartAcceptLoop()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = Task.Run(() => HandleRequestAsync(context));
                    }
                    catch (HttpListenerException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            });
        }

        public void Stop()
        {
            try { _listener.Stop(); } catch { }
        }

        private HttpListener StartHttpListener(out bool sharingEnabled)
        {
            var l = new HttpListener();
            try
            {
                l.Prefixes.Add($"http://+:{_port}/");
                l.Start();
                sharingEnabled = true;
                return l;
            }
            catch
            {
                l.Close();
                var fallback = new HttpListener();
                fallback.Prefixes.Add($"http://localhost:{_port}/");
                fallback.Start();
                sharingEnabled = false;
                return fallback;
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;
            var path = req.Url?.AbsolutePath ?? "/";

            try
            {
                if (req.HttpMethod == "GET" && path == "/")
                {
                    await SendResourceAsync(res, "wwwroot.index.html", "text/html; charset=utf-8");
                    return;
                }

                if (req.HttpMethod == "GET" && path == "/icon.ico")
                {
                    await SendResourceAsync(res, "wwwroot.icon.ico", "image/x-icon");
                    return;
                }

                if (req.HttpMethod == "GET" && path == "/api/network")
                {
                    await SendJsonAsync(res, new
                    {
                        port = _port,
                        local = _localUrl,
                        links = _networkSharingEnabled
                            ? GetLocalIps().Select(ip => new { ip, url = $"http://{ip}:{_port}" })
                            : []
                    });
                    return;
                }

                if (req.HttpMethod == "GET" && path == "/api/qrcode")
                {
                    var text = req.QueryString["text"] ?? _localUrl;
                    if (text.Length > 512)
                    {
                        await SendTextAsync(res, 400, "QR text is too long");
                        return;
                    }

                    await SendQrPngAsync(res, text);
                    return;
                }

                if (req.HttpMethod == "GET" && path == "/api/files")
                {
                    var dir = req.QueryString["path"] ?? string.Empty;
                    var files = _fileService.GetVisibleFiles(dir)
                        .OrderByDescending(file => file.IsDirectory)
                        .ThenByDescending(file => file.Modified);
                    await SendJsonAsync(res, files);
                    return;
                }

                if (req.HttpMethod == "GET" && path.StartsWith("/stream/", StringComparison.Ordinal))
                {
                    var request = _fileService.ParseFileRequest(path["/stream/".Length..]);
                    var filePath = _fileService.GetFilePath(request.RelativePath, request.Source);
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        await SendTextAsync(res, 404, "File not found");
                        return;
                    }

                    var ext = Path.GetExtension(filePath).ToLowerInvariant();
                    var contentType = ext switch
                    {
                        ".mp4" => "video/mp4",
                        ".webm" => "video/webm",
                        ".mkv" => "video/x-matroska",
                        ".mov" => "video/quicktime",
                        ".avi" => "video/x-msvideo",
                        ".mp3" => "audio/mpeg",
                        ".wav" => "audio/wav",
                        ".ogg" => "audio/ogg",
                        _ => "application/octet-stream"
                    };

                    res.ContentType = contentType;
                    res.AddHeader("Accept-Ranges", "bytes");

                    var fileInfo = new FileInfo(filePath);
                    long totalLength = fileInfo.Length;
                    var rangeHeader = req.Headers["Range"];

                    if (string.IsNullOrEmpty(rangeHeader) || !rangeHeader.StartsWith("bytes="))
                    {
                        res.StatusCode = 200;
                        res.ContentLength64 = totalLength;
                        await using var fs = File.OpenRead(filePath);
                        await fs.CopyToAsync(res.OutputStream);
                        return;
                    }

                    var range = rangeHeader["bytes=".Length..].Split('-');
                    long start = long.TryParse(range[0], out var parsedStart) ? parsedStart : 0;
                    long end = (range.Length > 1 && long.TryParse(range[1], out var parsedEnd)) ? parsedEnd : totalLength - 1;

                    if (start >= totalLength || end >= totalLength || start > end)
                    {
                        res.StatusCode = 416;
                        res.AddHeader("Content-Range", $"bytes */{totalLength}");
                        return;
                    }

                    long contentLength = end - start + 1;
                    res.StatusCode = 206;
                    res.AddHeader("Content-Range", $"bytes {start}-{end}/{totalLength}");
                    res.ContentLength64 = contentLength;

                    await using var stream = File.OpenRead(filePath);
                    stream.Seek(start, SeekOrigin.Begin);

                    byte[] buffer = new byte[64 * 1024];
                    long bytesRemaining = contentLength;

                    while (bytesRemaining > 0)
                    {
                        int bytesToRead = (int)Math.Min(buffer.Length, bytesRemaining);
                        int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, bytesToRead));
                        if (bytesRead == 0) break;

                        await res.OutputStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        bytesRemaining -= bytesRead;
                    }
                    return;
                }

                if ((req.HttpMethod == "GET" || req.HttpMethod == "HEAD") && path.StartsWith("/download/", StringComparison.Ordinal))
                {
                    var request = _fileService.ParseFileRequest(path["/download/".Length..]);
                    var filePath = _fileService.GetFilePath(request.RelativePath, request.Source);
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        await SendTextAsync(res, 404, "File not found");
                        return;
                    }

                    var fileName = Path.GetFileName(filePath);
                    var fileInfo = new FileInfo(filePath);
                    long totalLength = fileInfo.Length;
                    var lastModified = fileInfo.LastWriteTimeUtc.ToString("R");
                    var etag = $"\"{fileInfo.LastWriteTimeUtc.Ticks:x}-{totalLength:x}\"";

                    res.AddHeader("Accept-Ranges", "bytes");
                    res.AddHeader("ETag", etag);
                    res.AddHeader("Last-Modified", lastModified);
                    res.ContentType = "application/octet-stream";
                    res.AddHeader("Content-Disposition", $"attachment; filename*=UTF-8''{Uri.EscapeDataString(fileName)}");

                    if (req.HttpMethod == "HEAD")
                    {
                        res.StatusCode = 200;
                        res.ContentLength64 = totalLength;
                        return;
                    }

                    var rangeHeader = req.Headers["Range"];
                    if (string.IsNullOrEmpty(rangeHeader) || !rangeHeader.StartsWith("bytes="))
                    {
                        res.StatusCode = 200;
                        res.ContentLength64 = totalLength;
                        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read, 64 * 1024, useAsync: true);
                        await fs.CopyToAsync(res.OutputStream);
                        return;
                    }

                    var range = rangeHeader["bytes=".Length..].Split('-');
                    long start = long.TryParse(range[0], out var parsedStart) ? parsedStart : 0;
                    long end = (range.Length > 1 && long.TryParse(range[1], out var parsedEnd)) ? parsedEnd : totalLength - 1;

                    if (start >= totalLength || end >= totalLength || start > end)
                    {
                        res.StatusCode = 416;
                        res.AddHeader("Content-Range", $"bytes */{totalLength}");
                        return;
                    }

                    long contentLength = end - start + 1;
                    res.StatusCode = 206;
                    res.AddHeader("Content-Range", $"bytes {start}-{end}/{totalLength}");
                    res.ContentLength64 = contentLength;

                    await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read, 64 * 1024, useAsync: true))
                    {
                        stream.Seek(start, SeekOrigin.Begin);
                        byte[] buffer = new byte[64 * 1024];
                        long bytesRemaining = contentLength;

                        while (bytesRemaining > 0)
                        {
                            int bytesToRead = (int)Math.Min(buffer.Length, bytesRemaining);
                            int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, bytesToRead));
                            if (bytesRead == 0) break;

                            await res.OutputStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                            bytesRemaining -= bytesRead;
                        }
                    }
                    return;
                }

                if (req.HttpMethod == "DELETE" && path.StartsWith("/api/files/", StringComparison.Ordinal))
                {
                    var request = _fileService.ParseFileRequest(path["/api/files/".Length..]);
                    if (request.Source != "upload")
                    {
                        await SendTextAsync(res, 403, "Shared files cannot be deleted here");
                        return;
                    }

                    var filePath = _fileService.GetFilePath(request.RelativePath, request.Source);
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        await SendTextAsync(res, 404, "File not found");
                        return;
                    }

                    File.Delete(filePath);
                    await SendTextAsync(res, 200, "OK");
                    return;
                }

                if (req.HttpMethod == "POST" && path == "/upload")
                {
                    await SaveMultipartUploadAsync(req);
                    await SendTextAsync(res, 200, "OK");
                    return;
                }

                if (req.HttpMethod == "GET" && path == "/api/messages")
                {
                    var after = long.TryParse(req.QueryString["after"], out var value) ? value : 0;
                    await SendJsonAsync(res, _messageService.ReadMessages().Where(message => message.Id > after));
                    return;
                }

                if (req.HttpMethod == "GET" && path == "/api/messages/download")
                {
                    var body = string.Join("\n\n---\n\n", _messageService.ReadMessages().Select(message =>
                    {
                        var from = string.IsNullOrWhiteSpace(message.From) ? string.Empty : $" ({message.From})";
                        return $"[{message.Created:yyyy-MM-dd HH:mm:ss}{from}]\n{message.Text}";
                    }));
                    res.AddHeader("Content-Disposition", $"attachment; filename*=UTF-8''{Uri.EscapeDataString("fileshare-messages.txt")}");
                    await SendTextAsync(res, 200, body, "text/plain; charset=utf-8");
                    return;
                }

                if (req.HttpMethod == "POST" && path == "/api/messages")
                {
                    var data = await JsonSerializer.DeserializeAsync<MessageInput>(req.InputStream, _jsonOptions);
                    if (string.IsNullOrWhiteSpace(data?.Text))
                    {
                        await SendTextAsync(res, 400, "No message text");
                        return;
                    }

                    var message = await _messageService.AddMessageAsync(data.From, data.Text);
                    await SendJsonAsync(res, message);
                    return;
                }

                if (req.HttpMethod == "DELETE" && path == "/api/messages")
                {
                    await _messageService.WriteMessagesAsync(new List<MessageItem>());
                    await SendTextAsync(res, 200, "OK");
                    return;
                }

                if (req.HttpMethod == "POST" && path == "/api/messages/delete")
                {
                    var input = await JsonSerializer.DeserializeAsync<DeleteInput>(req.InputStream, _jsonOptions);
                    var ids = (input?.Ids ?? []).ToHashSet();
                    var messages = _messageService.ReadMessages();
                    var next = messages.Where(message => !ids.Contains(message.Id)).ToList();
                    await _messageService.WriteMessagesAsync(next);
                    await SendJsonAsync(res, new { deleted = messages.Count - next.Count });
                    return;
                }

                if ((req.HttpMethod == "PUT" || req.HttpMethod == "DELETE") && path.StartsWith("/api/messages/", StringComparison.Ordinal))
                {
                    if (!long.TryParse(path["/api/messages/".Length..], out var id))
                    {
                        await SendTextAsync(res, 400, "Bad message id");
                        return;
                    }

                    var messages = _messageService.ReadMessages();
                    var index = messages.FindIndex(message => message.Id == id);
                    if (index < 0)
                    {
                        await SendTextAsync(res, 404, "Message not found");
                        return;
                    }

                    if (req.HttpMethod == "DELETE")
                    {
                        messages.RemoveAt(index);
                        await _messageService.WriteMessagesAsync(messages);
                        await SendTextAsync(res, 200, "OK");
                        return;
                    }

                    var data = await JsonSerializer.DeserializeAsync<MessageInput>(req.InputStream, _jsonOptions);
                    if (string.IsNullOrWhiteSpace(data?.Text))
                    {
                        await SendTextAsync(res, 400, "No message text");
                        return;
                    }

                    messages[index] = messages[index] with { Text = data.Text, Edited = DateTimeOffset.UtcNow };
                    await _messageService.WriteMessagesAsync(messages);
                    await SendJsonAsync(res, messages[index]);
                    return;
                }

                await SendTextAsync(res, 404, "Not found");
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                if (res.OutputStream.CanWrite)
                {
                    await SendTextAsync(res, 500, "Server error");
                }
            }
            finally
            {
                res.OutputStream.Close();
            }
        }

        private async Task SendResourceAsync(HttpListenerResponse res, string resourceName, string contentType)
        {
            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                await SendTextAsync(res, 404, "Resource not found");
                return;
            }

            res.StatusCode = 200;
            res.ContentType = contentType;
            res.ContentLength64 = stream.Length;
            await stream.CopyToAsync(res.OutputStream);
        }

        private async Task SendTextAsync(HttpListenerResponse res, int status, string body, string contentType = "text/plain; charset=utf-8")
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            res.StatusCode = status;
            res.ContentType = contentType;
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
        }

        private async Task SendJsonAsync(HttpListenerResponse res, object value)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
            res.StatusCode = 200;
            res.ContentType = "application/json; charset=utf-8";
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
        }

        private async Task SendQrPngAsync(HttpListenerResponse res, string text)
        {
            var bytes = CreateQrPngBytes(text, 8);
            res.StatusCode = 200;
            res.ContentType = "image/png";
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
        }

        public static byte[] CreateQrPngBytes(string text, int pixelsPerModule)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qr = new PngByteQRCode(data);
            return qr.GetGraphic(pixelsPerModule);
        }

        private async Task SaveMultipartUploadAsync(HttpListenerRequest req)
        {
            var contentType = req.ContentType ?? string.Empty;
            var match = Regex.Match(contentType, "boundary=(.+)$");
            if (!match.Success)
            {
                throw new InvalidOperationException("No boundary");
            }

            using var memory = new MemoryStream();
            await req.InputStream.CopyToAsync(memory);
            var body = memory.ToArray();
            var headerEnd = IndexOf(body, Encoding.UTF8.GetBytes("\r\n\r\n"));
            if (headerEnd < 0)
            {
                throw new InvalidOperationException("Bad headers");
            }

            var headers = Encoding.UTF8.GetString(body, 0, headerEnd);
            var fileMatch = Regex.Match(headers, "filename=\"([^\"]*)\"");
            if (!fileMatch.Success)
            {
                throw new InvalidOperationException("No file");
            }

            var start = headerEnd + 4;
            var ending = Encoding.UTF8.GetBytes($"\r\n--{match.Groups[1].Value}");
            var end = LastIndexOf(body, ending);
            if (end < start)
            {
                throw new InvalidOperationException("Bad ending");
            }

            var targetPath = _fileService.UniquePath(_fileService.SafeName(fileMatch.Groups[1].Value));
            await File.WriteAllBytesAsync(targetPath, body[start..end]);
        }

        public static IEnumerable<string> GetLocalIps()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(item => item.OperationalStatus == OperationalStatus.Up)
                .SelectMany(item => item.GetIPProperties().UnicastAddresses)
                .Where(item => item.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(item.Address))
                .Select(item => item.Address.ToString())
                .Distinct();
        }

        private static int IndexOf(byte[] source, byte[] pattern)
        {
            for (var i = 0; i <= source.Length - pattern.Length; i++)
            {
                if (pattern.SequenceEqual(source.AsSpan(i, pattern.Length).ToArray()))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int LastIndexOf(byte[] source, byte[] pattern)
        {
            for (var i = source.Length - pattern.Length; i >= 0; i--)
            {
                if (pattern.SequenceEqual(source.AsSpan(i, pattern.Length).ToArray()))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
