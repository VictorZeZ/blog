namespace blog.Application.Users.Commands.BanUser
{
    public class BanUserResponse
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public bool IsBanned { get; init; }
    }
}
