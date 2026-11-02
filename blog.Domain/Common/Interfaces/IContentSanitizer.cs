namespace blog.Domain.Common.Interfaces
{
    public interface IContentSanitizer
    {
        string Sanitize(string html);
    }
}