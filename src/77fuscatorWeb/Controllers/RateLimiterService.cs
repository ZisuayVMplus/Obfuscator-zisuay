using System.Collections.Concurrent;

namespace _77fuscatorWeb.Controllers
{
    /// <summary>
    /// Simple sliding-window rate limiter (5 requests per minute per IP).
    /// </summary>
    public class RateLimiterService
    {
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _windows = new();
        private const int MaxRequests = 5;
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        public bool Allow(string key)
        {
            var now = DateTime.UtcNow;
            var queue = _windows.GetOrAdd(key, _ => new Queue<DateTime>());

            lock (queue)
            {
                // Remove expired entries
                while (queue.Count > 0 && now - queue.Peek() > Window)
                    queue.Dequeue();

                if (queue.Count >= MaxRequests)
                    return false;

                queue.Enqueue(now);
                return true;
            }
        }
    }
}
