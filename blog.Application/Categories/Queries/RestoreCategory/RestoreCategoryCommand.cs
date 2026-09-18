using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Categories.Commands.RestoreCategory
{
    public class RestoreCategoryCommand : IRequest<RestoreCategoryResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public Guid CategoryId { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}