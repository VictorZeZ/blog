using blog.Application.Users.Commands.Register;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class RegisterCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly RegisterCommandHandler _handler;

        public RegisterCommandHandlerTests()
        {
            _handler = new RegisterCommandHandler(
                _userRepositoryMock.Object,
                _passwordHasherMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsRegisterResponse()
        {
            // Arrange
            var command = new RegisterCommand
            {
                Email = "test@test.com",
                FirstName = "Ali",
                LastName = "Rezaei",
                Password = "Password123"
            };

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _passwordHasherMock
                .Setup(x => x.Hash(command.Password))
                .Returns("hashed_password");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(command.Email);
            result.FullName.Should().Be($"{command.FirstName} {command.LastName}");
            result.Id.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_ExistingEmail_ThrowsAlreadyExistsException()
        {
            // Arrange
            var command = new RegisterCommand
            {
                Email = "existing@test.com",
                FirstName = "Ali",
                LastName = "Rezaei",
                Password = "Password123"
            };

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_HashesPassword()
        {
            // Arrange
            var command = new RegisterCommand
            {
                Email = "test@test.com",
                FirstName = "Ali",
                LastName = "Rezaei",
                Password = "Password123"
            };

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _passwordHasherMock
                .Setup(x => x.Hash(command.Password))
                .Returns("hashed_password");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _passwordHasherMock.Verify(x => x.Hash(command.Password), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var command = new RegisterCommand
            {
                Email = "test@test.com",
                FirstName = "Ali",
                LastName = "Rezaei",
                Password = "Password123"
            };

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _passwordHasherMock
                .Setup(x => x.Hash(command.Password))
                .Returns("hashed_password");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_AddsUserToRepository()
        {
            // Arrange
            var command = new RegisterCommand
            {
                Email = "test@test.com",
                FirstName = "Ali",
                LastName = "Rezaei",
                Password = "Password123"
            };

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _passwordHasherMock
                .Setup(x => x.Hash(command.Password))
                .Returns("hashed_password");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
