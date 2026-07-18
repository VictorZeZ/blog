namespace blog.Domain.Common.Interfaces
{
    public interface IEmailTemplateRenderer
    {
        string RenderVerificationCode(string title, string description, string code, int expiryMinutes);
    }
}
