
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TL;
using WTelegram;


namespace Telegram.User.Net
{
    /// <summary>
    /// High-performance wrapper for the Binjie GPT endpoint.
    /// Reuses a single HttpClient and minimizes allocations.
    /// </summary>
    public class AiChatService
    {
        private const string BaseUrl = "https://api.binjie.fun/";
        private static readonly string[] _origins = new[]
        {
            "https://c2.binjie.fun/",
            "https://c.binjie.fun/",
            "https://cht18.aichatosd2.com",
            "https://chat18.aichatos68.com/"
        };

        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;
        readonly IServiceProvider _services;
        readonly Client _client;
        readonly ConfigurationService _configuration;

        public AiChatService(IServiceProvider services)
        {
            _services = services;
            _client = services.GetRequiredService<Client>();
            _configuration = services.GetRequiredService<ConfigurationService>();

            _http = new();
            _http.BaseAddress = new Uri(BaseUrl);
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _http.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            _http.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _http.DefaultRequestHeaders.Add("Access-Control-Allow-Origin", "*");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }



        // ── Rate-limiting state ───────────────────────────────────────────
        // Holds the UTC timestamps of the last 60 requests.
        /// <summary>
        /// Sliding-window rate limiter: max 60 requests in any 60-minute span.
        /// </summary>
        private readonly object _rateLock = new object();
        private readonly Queue<DateTimeOffset> _timestamps = new Queue<DateTimeOffset>();
        private DateTimeOffset _lastSuccess = DateTimeOffset.MinValue;

        public bool TryAcquireSlot()
        {
            var now = DateTimeOffset.UtcNow;

            lock (_rateLock)
            {
                // Remove timestamps older than 60 minutes
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > TimeSpan.FromMinutes(60))
                {
                    _timestamps.Dequeue();
                }

                // Rule 1: at least 2 minutes since last success
                if (now - _lastSuccess < TimeSpan.FromSeconds(60))
                {
                    return false;
                }

                // Rule 2: no more than 60 in the last 60 minutes
                if (_timestamps.Count >= 60)
                {
                    return false;
                }

                // Passed both checks → success
                _timestamps.Enqueue(now);
                _lastSuccess = now;
                return true;
            }
        }
        public async Task<bool> ReplyMessageWithAi(Message Message, TL.User user)
        {
            if (TryAcquireSlot() is false)
                return false;

            var historyResp = await _client.Messages_GetHistory(user.ToInputPeer(), offset_id: Message.id, limit: 10);
            var historyMsgs = historyResp.Messages
                .OfType<Message>()
                .Where(m => m.id != Message.id && !string.IsNullOrEmpty(m.message))
                .OrderBy(m => m.id)
                .ToList();


            string historyText = historyMsgs.Count > 0
                ? BuildHistoryText(historyMsgs)
                : string.Empty;

            const int MAX_HISTORY_CHARS = 7000;
            historyText = TruncateKeepTail(historyText, MAX_HISTORY_CHARS);

            var prompet = $"{_configuration.Prompt}\nContext:{historyText}\nNew User message:{Message.message}";

            var aiResponse = await QueryAsync(null, prompet, user.id.ToString());
            if (aiResponse is not null)
            {
                await _client.SendMessageAsync(user.ToInputPeer(), aiResponse, null, Message.id);
                return true;
            }

            return false;
        }
        string BuildHistoryText(List<Message> msgs)
        {
            // each entry: "Sender: text"
            var lines = msgs.Select(m =>
            {
                var sender = m.from_id == _client.UserId ? "Me" : "Contact";
                var text = m.message.Replace("\r\n", " ").Replace("\n", " ");
                if (text.Length > 500) text = text.Substring(0, 500) + "…";
                return $"{sender}: {text}";
            });

            return string.Join("\n", lines);
        }

        string TruncateKeepTail(string input, int maxChars)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxChars) return input;
            // keep the last maxChars chars, but try to start at a word boundary
            int start = input.Length - maxChars;
            string tail = input.Substring(start);
            int firstWs = tail.IndexOfAny(new char[] { ' ', '\n', '\r', '\t' });
            if (firstWs > 0 && firstWs < 64) tail = tail.Substring(firstWs + 1);
            return tail;
        }
        /// <summary>
        /// Posts system+user prompts to Binjie and returns the AI’s raw text.
        /// </summary>
        /// <param name="systemPrompt">Your system instructions.</param>
        /// <param name="userPrompt">The user’s message.</param>
        /// <param name="userId">Unique Chat ID (default “1”).</param>
        /// 
        public async Task<string> QueryAsync(string systemPrompt, string userPrompt, string userId)
        {
            var aiReply = await PostQueryAsync(systemPrompt, userPrompt, userId).TryAsync();
            if (aiReply.isSuccessful is false || string.IsNullOrWhiteSpace(aiReply.result) || aiReply.result.Contains(".com") || aiReply.result.Contains("https://"))
            {
                return null;
            }
            else return aiReply.result;
        }
        private async Task<string> PostQueryAsync(string systemPrompt, string userPrompt, string userId)
        {
            // Pick a random Origin header per request
            string origin = _origins[Random.Shared.Next(_origins.Length)];

            // Build payload
            var payload = new
            {
                system = systemPrompt,
                prompt = userPrompt,
                userId = $"#/chat/{userId}",
                network = true,
                stream = false
            };
            string json = JsonSerializer.Serialize(payload, _jsonOptions);

            // Prepare HTTP request
            using var request = new HttpRequestMessage(
                HttpMethod.Post, "api/generateStream");
            request.Headers.UserAgent.ParseAdd(
                UserAgentGenerator.GenerateUserAgent());
            request.Headers.Add("Origin", origin);
            request.Content = new StringContent(
                json, Encoding.UTF8, "application/json");

            // Send and read
            using var response = await _http
                .SendAsync(request)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);
        }
    }
}

