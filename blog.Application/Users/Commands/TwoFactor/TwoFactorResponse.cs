namespace blog.Application.Users.Commands.TwoFactor
{
    public class TwoFactorResponse
    {
        public Guid Id { get; init; }
        public bool TwoFactorEnabled { get; init; }
    }
}
