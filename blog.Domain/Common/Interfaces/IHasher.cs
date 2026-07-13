namespace blog.Domain.Common.Interfaces
{
    public interface IHasher
    {
        string Hash(string token);
    }
}
