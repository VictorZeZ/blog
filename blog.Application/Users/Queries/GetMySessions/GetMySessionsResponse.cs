namespace blog.Application.Users.Queries.GetMySessions
{
    public class GetMySessionsResponse
    {
        public Guid Id { get; init; }
        public string DeviceInfo { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
    }
}