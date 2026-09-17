using blog.Domain.Exceptions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Queries.GetUserDetails
{
    public class GetUserDetailsQueryHandler(IUserRepository userRepository) : IRequestHandler<GetUserDetailsQuery, GetUserDetailsResponse>
    {
        public async Task<GetUserDetailsResponse> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
        {
            var target = await userRepository.GetByIdAsync(new UserId(request.TargetUserId), cancellationToken);
            if (target is null)
                throw new NotFoundException("User", request.TargetUserId);

            return new GetUserDetailsResponse
            {
                Id = target.Id.Value,
                Email = target.Email,
                FullName = target.FullName,
                Level = target.Level,
                IsBanned = target.IsBanned,
                IsDeleted = target.IsDeleted,
                IsEmailConfirmed = target.IsEmailConfirmed,
                TwoFactorEnabled = target.TwoFactorEnabled,
                CreatedAt = target.CreatedAt,
                UpdatedAt = target.UpdatedAt
            };
        }
    }
}