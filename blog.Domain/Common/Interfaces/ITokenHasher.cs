namespace blog.Domain.Common.Interfaces
{
    public interface ITokenHasher
    {
        string Hash(string token);
    }
}
