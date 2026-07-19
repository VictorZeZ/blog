using blog.Application.Users.Commands.Register;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class RegisterCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly RegisterCommandHandler _handler;

        private readonly EmailVerificationSettings _emailVerificationSettings = new()
        {
            RegistrationExpiryMinutes = 15,
            RegistrationMaxAttempts = 5,
            LoginVerificationExpiryMinutes = 10,
            LoginVerificationMaxAttempts = 5,
            ChangeEmailExpiryMinutes = 15,
            ChangeEmailMaxAttempts = 5,
            ResetPasswordExpiryMinutes = 15,
            ResetPasswordMaxAttempts = 5
        };

        public RegisterCommandHandlerTests()
        {
            _handler = new RegisterCommandHandler(
                _userRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
                _passwordHasherMock.Object,
                _emailServiceMock.Object,
                Options.Create(_emailVerificationSettings),
                _unitOfWorkMock.Object);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("hashed_code");
        }

        private static RegisterCommand CreateValidCommand() => new()
        {
            Email = "test@test.com",
            FirstName = "Ali",
            LastName = "Rezaei",
            Password = "Password123!"
        };

        private void SetupHappyPath(RegisterCommand command)
        {
            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _passwordHasherMock
                .Setup(x => x.Hash(command.Password))
                .Returns("hashed_password");
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsRegisterResponse()
        {
            // Arrange
            var command = CreateValidCommand();
            SetupHappyPath(command);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(command.Email);
            result.Id.Should().NotBeEmpty();
            result.ExpiryMinutes.Should().Be(_emailVerificationSettings.RegistrationExpiryMinutes);
        }

        [Fact]
        public async Task Handle_ExistingEmail_ThrowsAlreadyExistsException()
        {
            // Arrange
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_ExistingEmail_DoesNotAddUser()
        {
            // Arrange
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<AlreadyExistsException>();

            // Assert
            _userRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ExistingEmail_DoesNotSendVerificationCode()
        {
            // Arrange
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<AlreadyExistsException>();

            // Assert
            _emailServiceMock.Verify(
                x => x.SendVerificationCodeAsync(It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ExistingEmail_DoesNotSaveChanges()
        {
            // Arrange
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<AlreadyExistsException>();

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ValidCommand_HashesPassword()
        {
            // Arrange
            var command = CreateValidCommand();
            SetupHappyPath(command);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _passwordHasherMock.Verify(x => x.Hash(command.Password), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_AddsUserToRepository()
        {
            // Arrange
            var command = CreateValidCommand();
            SetupHappyPath(command);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_AddsUserWithCorrectData()
        {
            // Arrange
            var command = CreateValidCommand();
            SetupHappyPath(command);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u =>
                u.Email == command.Email.ToLowerInvariant() &&
                u.FirstName == command.FirstName &&
                u.LastName == command.LastName &&
                u.PasswordHash == "hashed_password" &&
                u.IsEmailConfirmed == false), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SendsVerificationCodeWithRegistrationPurpose()
        {
            // Arrange
            var command = CreateValidCommand();
            SetupHappyPath(command);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                command.Email,
                EmailVerificationPurpose.Registration,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_AddsEmailVerificationWithCorrectData()
        {
            // Arrange
            var command = CreateValidCommand();
            SetupHappyPath(command);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerification>(v =>
                v.Purpose == EmailVerificationPurpose.Registration &&
                v.CodeHash == "hashed_code" &&
                v.TargetEmail == null &&
                v.IsValid()), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var command = CreateValidCommand();
            SetupHappyPath(command);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsCorrectExpiryMinutesForPurpose()
        {
            // Arrange
            var command = CreateValidCommand();
            SetupHappyPath(command);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.ExpiryMinutes.Should().Be(_emailVerificationSettings.RegistrationExpiryMinutes);
        }
    }
}
