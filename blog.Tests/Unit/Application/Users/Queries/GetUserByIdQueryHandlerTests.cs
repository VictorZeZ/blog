using blog.Application.Users.Queries.GetUserById;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Queries
{
    public class GetUserByIdQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly GetUserByIdQueryHandler _handler;

        public GetUserByIdQueryHandlerTests()
        {
            _handler = new GetUserByIdQueryHandler(
                _userRepositoryMock.Object);
        }

        private static GetUserByIdQuery ValidQuery => new()
        {
            UserId = Guid.NewGuid()
        };

        private static User ValidUser => new(
            "test@test.com",
            "Ali",
            "Rezaei",
            "hashed_password");

        [Fact]
        public async Task Handle_ValidQuery_ReturnsGetUserByIdResponse()
        {
            // Arrange
            var query = ValidQuery;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(query.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(user.Email);
            result.FullName.Should().Be(user.FullName);
            result.Level.Should().Be(UserLevel.Normal);
            result.IsBanned.Should().BeFalse();
            result.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var query = ValidQuery;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(query.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DeletedUser_ReturnsResponseWithIsDeletedTrue()
        {
            // Arrange
            var query = ValidQuery;
            var user = ValidUser;
            user.SoftDelete();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(query.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_BannedUser_ReturnsResponseWithIsBannedTrue()
        {
            // Arrange
            var query = ValidQuery;
            var user = ValidUser;
            user.Ban();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(query.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsBanned.Should().BeTrue();
        }
    }
}
