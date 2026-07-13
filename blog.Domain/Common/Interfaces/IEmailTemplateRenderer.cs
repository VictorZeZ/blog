namespace blog.Domain.Common.Interfaces
{
    public interface IEmailTemplateRenderer
    {
        string RenderVerificationCode(string code, int expiryMinutes);
    }
}
