using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using MediatR;
using RefreshTokenEntity = blog.Domain.Tokens.Entities.RefreshToken;

namespace blog.Application.Users.Commands.Register
{
    public class RegisterCommandHandler(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IPasswordHasher passwordHasher, IJwtService jwtService, IUnitOfWork unitOfWork) : IRequestHandler<RegisterCommand, RegisterResponse>
    {
        public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var exists = await userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
            if (exists)
                throw new AlreadyExistsException("User", request.Email);

            var passwordHash = passwordHasher.Hash(request.Password);

            var user = new User(
                request.Email,
                request.FirstName,
                request.LastName,
                passwordHash);

            var accessToken = jwtService.GenerateAccessToken(user);
            var refreshToken = jwtService.GenerateRefreshToken();

            var token = new RefreshTokenEntity(
                refreshToken,
                user.Id,
                request.DeviceInfo);

            await userRepository.AddAsync(user, cancellationToken);
            await refreshTokenRepository.AddAsync(token, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RegisterResponse
            {
                Id = user.Id.Value,
                Email = user.Email,
                FullName = user.FullName,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
    }
}
