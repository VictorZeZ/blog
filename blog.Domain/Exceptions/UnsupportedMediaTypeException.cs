namespace blog.Domain.Exceptions
{
    public class UnsupportedMediaTypeException : DomainException
    {
        public UnsupportedMediaTypeException(string mediaType, IEnumerable<string> supported)
            : base(415, "UNSUPPORTED_MEDIA_TYPE", $"Media type {mediaType} is not supported", new { MediaType = mediaType, Supported = supported })
        { }
    }
}
