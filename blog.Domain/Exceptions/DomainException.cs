namespace blog.Domain.Exceptions
{
    public abstract class DomainException : Exception
    {
        public int StatusCode { get; }
        public string ErrorCode { get; }
        public string Title { get; }
        public object? Details { get; }

        protected DomainException(int statusCode, string errorCode, string title, object? details = null) : base(title)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Title = title;
            Details = details;
        }
    }
}
