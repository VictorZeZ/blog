using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Categories.Queries.GetDeletedCategories
{
    public class GetDeletedCategoriesQuery : IRequest<IEnumerable<GetDeletedCategoriesResponse>>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}