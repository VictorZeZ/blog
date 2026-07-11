using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommand : IRequest<CreateCategoryResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public string Name { get; init; } = string.Empty;

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}
