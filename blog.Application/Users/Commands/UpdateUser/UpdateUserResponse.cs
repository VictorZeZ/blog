namespace blog.Application.Users.Commands.UpdateUser
{
    public class UpdateUserResponse
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = string.Empty;
    }
}
