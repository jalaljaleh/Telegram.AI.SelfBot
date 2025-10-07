

namespace Telegram.User.Net
{
    using System;
    using System.Collections.Concurrent;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    // A small helper that holds a user's buffer and a timer cancellation token.
    class UserMessageBuffer : IDisposable
    {
        public readonly object Lock = new();
        public StringBuilder Buffer = new();
        public DateTime LastUpdated = DateTime.UtcNow;
        public CancellationTokenSource? CancelSource;

        public void Dispose()
        {
            CancelSource?.Cancel();
            CancelSource?.Dispose();
        }
    }

    static class MessageAggregator
    {
        // Configurable
        private static readonly TimeSpan InactivityTimeout = TimeSpan.FromSeconds(4.0); // debounce window
        private const int MaxBufferLength = 5000; // safety limit

        // Per-user buffers
        private static readonly ConcurrentDictionary<long, UserMessageBuffer> Buffers = new();

        // Call this from your message handler
        public static async Task<bool> HandleIncomingMessageAsync(
            long userId,
            string text,
            Func<string, Task<bool>> aiHandler // returns true if AI produced a response
        )
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Get or create the buffer for this user
            var buffer = Buffers.GetOrAdd(userId, _ => new UserMessageBuffer());

            // Append and update timestamp in a lock so flush and append don't race
            lock (buffer.Lock)
            {
                // Append with a space to keep words separated when messages are fragments
                if (buffer.Buffer.Length > 0 && !char.IsWhiteSpace(buffer.Buffer[^1]))
                    buffer.Buffer.Append(' ');
                buffer.Buffer.Append(text.Trim());

                // Trim if too long
                if (buffer.Buffer.Length > MaxBufferLength)
                    buffer.Buffer.Length = MaxBufferLength;

                buffer.LastUpdated = DateTime.UtcNow;

                // Cancel previous debounce timer and create a new one
                buffer.CancelSource?.Cancel();
                buffer.CancelSource = new CancellationTokenSource();
                var token = buffer.CancelSource.Token;

                // Fire-and-forget debounce task
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(InactivityTimeout, token);
                        // If not cancelled, flush
                        if (!token.IsCancellationRequested)
                            await FlushBufferAsync(userId, aiHandler);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        // Log as needed
                        Console.Error.WriteLine($"Debounce task error: {ex}");
                    }
                }, CancellationToken.None);
            }

            // Quick heuristic: if message ends with terminal punctuation, flush immediately
            if (EndsWithSentenceTerminator(text))
            {
                await FlushBufferAsync(userId, aiHandler);
            }

            return true; // indicates message processed by aggregator
        }

        // Flush and remove buffer after processing
        private static async Task FlushBufferAsync(long userId, Func<string, Task<bool>> aiHandler)
        {
            if (!Buffers.TryGetValue(userId, out var buffer))
                return;

            string toProcess;
            lock (buffer.Lock)
            {
                toProcess = buffer.Buffer.ToString().Trim();
                buffer.Buffer.Clear();
                buffer.CancelSource?.Cancel();
                buffer.CancelSource?.Dispose();
                buffer.CancelSource = null;
                buffer.LastUpdated = DateTime.UtcNow;
            }

            // If empty after trimming, nothing to do
            if (string.IsNullOrEmpty(toProcess))
                return;

            try
            {
                // Call AI once with aggregated text
                bool responded = await aiHandler(toProcess);

                // Optionally: if the AI responded you might want to clear state or mark user as replied
                // If you want to log or persist, do it here
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"AI handler failed: {ex}");
                // Optionally requeue or log
            }
            finally
            {
                // Remove empty buffers to keep dictionary small
                lock (buffer.Lock)
                {
                    if (buffer.Buffer.Length == 0)
                    {
                        buffer.Dispose();
                        Buffers.TryRemove(userId, out _);
                    }
                }
            }
        }

        private static bool EndsWithSentenceTerminator(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var last = text.TrimEnd();
            char c = last[^1];
            return c == '.' || c == '!' || c == '?' || c == '؟' /* Arabic question mark */;
        }

        // Optional: call on shutdown to dispose remaining resources
        public static void DisposeAll()
        {
            foreach (var kv in Buffers)
            {
                kv.Value.Dispose();
            }
            Buffers.Clear();
        }
    }

}
