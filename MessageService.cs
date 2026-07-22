using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileShare
{
    public class MessageService
    {
        private readonly string _messageLog;
        private readonly JsonSerializerOptions _jsonOptions;

        public MessageService(string appDir, JsonSerializerOptions jsonOptions)
        {
            _messageLog = Path.Combine(appDir, "messages.jsonl");
            _jsonOptions = jsonOptions;
            File.AppendAllText(_messageLog, string.Empty, Encoding.UTF8);
        }

        public List<MessageItem> ReadMessages()
        {
            return File.ReadLines(_messageLog, Encoding.UTF8)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    try { return JsonSerializer.Deserialize<MessageItem>(line, _jsonOptions); }
                    catch { return null; }
                })
                .Where(message => message is not null)
                .Cast<MessageItem>()
                .ToList();
        }

        public async Task WriteMessagesAsync(IEnumerable<MessageItem> messages)
        {
            var body = string.Join("\n", messages.Select(message => JsonSerializer.Serialize(message, _jsonOptions)));
            await File.WriteAllTextAsync(_messageLog, string.IsNullOrEmpty(body) ? string.Empty : body + "\n", Encoding.UTF8);
        }

        public async Task<MessageItem> AddMessageAsync(string? from, string text)
        {
            var message = new MessageItem(
                System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + System.Random.Shared.Next(1000),
                (from ?? string.Empty)[..System.Math.Min(from?.Length ?? 0, 80)],
                text,
                System.DateTimeOffset.UtcNow,
                null
            );
            await File.AppendAllTextAsync(_messageLog, JsonSerializer.Serialize(message, _jsonOptions) + "\n", Encoding.UTF8);
            return message;
        }
    }
}
