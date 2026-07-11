using MediatR;

namespace blog.Application.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQuery : IRequest<IEnumerable<GetAllCategoriesResponse>> { }
}
