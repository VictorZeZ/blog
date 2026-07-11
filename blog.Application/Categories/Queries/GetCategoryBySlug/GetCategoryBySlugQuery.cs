using MediatR;

namespace blog.Application.Categories.Queries.GetCategoryBySlug
{
    public class GetCategoryBySlugQuery : IRequest<GetCategoryBySlugResponse>
    {
        public string Slug { get; init; } = string.Empty;
    }
}
