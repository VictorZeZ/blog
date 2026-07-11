using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryCommand : IRequest<DeleteCategoryResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public Guid CategoryId { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}
