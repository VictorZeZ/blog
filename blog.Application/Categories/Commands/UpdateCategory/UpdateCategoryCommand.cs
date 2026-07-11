using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommand : IRequest<UpdateCategoryResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public Guid CategoryId { get; init; }
        public string Name { get; init; } = string.Empty;

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}
