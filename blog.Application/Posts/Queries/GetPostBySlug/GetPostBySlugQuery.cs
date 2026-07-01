using MediatR;

namespace blog.Application.Posts.Queries.GetPostBySlug
{
    public class GetPostBySlugQuery : IRequest<GetPostBySlugResponse>
    {
        public string Slug { get; init; } = string.Empty;
        public Guid? ActorId { get; init; }
    }
}
