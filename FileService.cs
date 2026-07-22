using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileShare
{
    public record MessageInput(string? Text, string? From);
    public record DeleteInput(long[] Ids);
    public record FileItem(string Name, long Size, DateTime Modified, string Source, bool CanDelete, bool IsDirectory, string Path);
    public record MessageItem(long Id, string From, string Text, DateTimeOffset Created, DateTimeOffset? Edited);

    public class FileService
    {
        public string UploadDir { get; }
        public string SharedFolderConfig { get; }
        public string SharedFolder { get; set; }

        public FileService(string appDir)
        {
            UploadDir = Path.Combine(appDir, "uploads");
            SharedFolderConfig = Path.Combine(appDir, "shared-folder.txt");
            SharedFolder = File.Exists(SharedFolderConfig) ? File.ReadAllText(SharedFolderConfig, System.Text.Encoding.UTF8).Trim() : string.Empty;

            Directory.CreateDirectory(UploadDir);
            if (!string.IsNullOrWhiteSpace(SharedFolder))
            {
                Directory.CreateDirectory(SharedFolder);
            }
        }

        public void UpdateSharedFolder(string newPath)
        {
            SharedFolder = newPath;
            Directory.CreateDirectory(SharedFolder);
            File.WriteAllText(SharedFolderConfig, SharedFolder, System.Text.Encoding.UTF8);
        }

        public IEnumerable<FileItem> GetVisibleFiles(string subPath = "")
        {
            var (relative, source) = ParseSubPath(subPath);

            if (string.IsNullOrEmpty(relative) && (string.IsNullOrEmpty(subPath) || subPath == "/" || subPath == "root"))
            {
                if (Directory.Exists(UploadDir))
                {
                    foreach (var dir in Directory.GetDirectories(UploadDir))
                    {
                        var info = new DirectoryInfo(dir);
                        yield return new FileItem(info.Name, 0, info.LastWriteTimeUtc, "upload", false, true, $"upload/{info.Name}");
                    }
                    foreach (var file in Directory.GetFiles(UploadDir))
                    {
                        var info = new FileInfo(file);
                        yield return new FileItem(info.Name, info.Length, info.LastWriteTimeUtc, "upload", true, false, $"upload/{info.Name}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(SharedFolder) && Directory.Exists(SharedFolder))
                {
                    foreach (var dir in Directory.GetDirectories(SharedFolder))
                    {
                        var info = new DirectoryInfo(dir);
                        yield return new FileItem(info.Name, 0, info.LastWriteTimeUtc, "shared", false, true, $"shared/{info.Name}");
                    }
                    foreach (var file in Directory.GetFiles(SharedFolder))
                    {
                        var info = new FileInfo(file);
                        yield return new FileItem(info.Name, info.Length, info.LastWriteTimeUtc, "shared", false, false, $"shared/{info.Name}");
                    }
                }
                yield break;
            }

            var targetFolder = GetFolderPath(relative, source);

            if (!string.IsNullOrEmpty(targetFolder) && Directory.Exists(targetFolder))
            {
                foreach (var dir in Directory.GetDirectories(targetFolder))
                {
                    var info = new DirectoryInfo(dir);
                    var relPath = string.IsNullOrEmpty(relative) ? $"{source}/{info.Name}" : $"{source}/{relative}/{info.Name}";
                    yield return new FileItem(info.Name, 0, info.LastWriteTimeUtc, source, false, true, relPath);
                }
                foreach (var file in Directory.GetFiles(targetFolder))
                {
                    var info = new FileInfo(file);
                    var relPath = string.IsNullOrEmpty(relative) ? $"{source}/{info.Name}" : $"{source}/{relative}/{info.Name}";
                    yield return new FileItem(info.Name, info.Length, info.LastWriteTimeUtc, source, source == "upload", false, relPath);
                }
            }
        }

        public string CleanSubPath(string subPath)
        {
            if (string.IsNullOrWhiteSpace(subPath)) return string.Empty;
            var parts = subPath.Split('/', '\\')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p) && p != "." && p != "..")
                .Select(SafeName);
            return string.Join("/", parts);
        }

        public (string RelativePath, string Source) ParseSubPath(string rawSubPath)
        {
            var cleaned = CleanSubPath(rawSubPath);
            if (string.IsNullOrEmpty(cleaned)) return (string.Empty, "upload");

            var separator = cleaned.IndexOf('/');
            if (separator < 0)
            {
                if (cleaned == "shared" || cleaned == "upload")
                    return (string.Empty, cleaned);
                return (cleaned, "upload");
            }

            var first = cleaned[..separator];
            var rest = cleaned[(separator + 1)..];
            if (first == "shared" || first == "upload")
            {
                return (rest, first);
            }
            return (cleaned, "upload");
        }

        public string? GetFolderPath(string relativePath, string source)
        {
            var root = source == "shared" ? SharedFolder : UploadDir;
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return null;

            if (string.IsNullOrEmpty(relativePath)) return root;

            var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            var fullRoot = Path.GetFullPath(root);

            if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath;
            }
            return null;
        }

        public string? GetFilePath(string relativePath, string source)
        {
            var folder = GetFolderPath(Path.GetDirectoryName(relativePath.Replace('/', Path.DirectorySeparatorChar)) ?? "", source);
            if (folder is null) return null;
            var fileName = Path.GetFileName(relativePath);
            var fullPath = Path.Combine(folder, fileName);
            return File.Exists(fullPath) ? fullPath : null;
        }

        public (string RelativePath, string Source) ParseFileRequest(string value)
        {
            var raw = Uri.UnescapeDataString(value);
            return ParseSubPath(raw);
        }

        public string UniquePath(string fileName)
        {
            var target = Path.Combine(UploadDir, fileName);
            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            var count = 1;

            while (File.Exists(target))
            {
                target = Path.Combine(UploadDir, $"{name}-{count}{ext}");
                count++;
            }

            return target;
        }

        public string SafeName(string? name)
        {
            var baseName = Path.GetFileName(string.IsNullOrWhiteSpace(name) ? "file" : name);
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(baseName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned;
        }
    }
}
