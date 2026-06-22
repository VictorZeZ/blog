namespace blog.Domain.Exceptions
{
    public class RateLimitException : DomainException
    {
        public RateLimitException(string action, int retryAfterSeconds)
            : base(429, "RATE_LIMIT_EXCEEDED", $"Too many requests for {action}", new { Action = action, RetryAfterSeconds = retryAfterSeconds })
        { }
    }
}
