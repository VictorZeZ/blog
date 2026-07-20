using blog.Application.Users.Commands.ConfirmNewEmail;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class ConfirmNewEmailCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IHasher> _hasherMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ConfirmNewEmailCommandHandler _handler;

        private const string TargetEmail = "new@test.com";

        private readonly EmailVerificationSettings _emailVerificationSettings = new()
        {
            RegistrationExpiryMinutes = 15,
            RegistrationMaxAttempts = 5,
            LoginVerificationExpiryMinutes = 10,
            LoginVerificationMaxAttempts = 5,
            ChangeEmailExpiryMinutes = 15,
            ChangeEmailMaxAttempts = 5,
            ResetPasswordExpiryMinutes = 15,
            ResetPasswordMaxAttempts = 5,
            ConfirmNewEmailExpiryMinutes = 15,
            ConfirmNewEmailMaxAttempts = 5
        };

        public ConfirmNewEmailCommandHandlerTests()
        {
            _handler = new ConfirmNewEmailCommandHandler(
                _userRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
                _hasherMock.Object,
                Options.Create(_emailVerificationSettings),
                _unitOfWorkMock.Object);
        }

        private static User CreateConfirmedUser()
        {
            var user = new User("current@test.com", "Ali", "Rezaei", "hashed_password");
            user.ConfirmEmail();
            return user;
        }

        private static ConfirmNewEmailCommand CreateCommand(Guid userId) => new()
        {
            UserId = userId,
            Code = "ABC123"
        };

        // ── User not found ──────────────────────────────────────────────

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = CreateCommand(Guid.NewGuid());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── Missing / wrong-purpose verification ────────────────────────

        [Fact]
        public async Task Handle_NoActiveVerification_ThrowsNotFoundException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ActiveVerificationHasDifferentPurpose_ThrowsNotFoundException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.ChangeEmail, 15, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── Expired ──────────────────────────────────────────────────────

        [Fact]
        public async Task Handle_ExpiredVerification_ThrowsExpiredException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.ConfirmNewEmail, -1, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ExpiredException>();
        }

        // ── Exceeded attempts ────────────────────────────────────────────

        [Fact]
        public async Task Handle_ExceededMaxAttempts_ThrowsLockedException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, TargetEmail);
            for (var i = 0; i < _emailVerificationSettings.ConfirmNewEmailMaxAttempts; i++)
                verification.RegisterFailedAttempt();

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<LockedException>();
        }

        // ── Invalid code ─────────────────────────────────────────────────

        [Fact]
        public async Task Handle_InvalidCode_ThrowsValidationException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("wrong_hash");

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_InvalidCode_RegistersFailedAttemptAndSavesChanges()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("wrong_hash");

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            verification.AttemptCount.Should().Be(1);
            _emailVerificationRepositoryMock.Verify(x => x.Update(verification), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidCode_DoesNotChangeUserEmail()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var originalEmail = user.Email;
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("wrong_hash");

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            user.Email.Should().Be(originalEmail);
            _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        }

        // ── Target email taken by someone else in the meantime ──────────

        [Fact]
        public async Task Handle_TargetEmailTakenInTheMeantime_ThrowsAlreadyExistsException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("correct_hash");

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(TargetEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_TargetEmailTakenInTheMeantime_DoesNotMarkVerificationAsVerifiedOrChangeEmail()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var originalEmail = user.Email;
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("correct_hash");

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(TargetEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<AlreadyExistsException>();

            // Assert — the code itself must remain usable for a retry once the conflict is resolved
            verification.IsValid().Should().BeTrue();
            user.Email.Should().Be(originalEmail);
            _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        }

        // ── Successful confirmation ──────────────────────────────────────

        [Fact]
        public async Task Handle_ValidCode_ReturnsUpdatedEmail()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(TargetEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Id.Should().Be(user.Id.Value);
            result.Email.Should().Be(TargetEmail);
        }

        [Fact]
        public async Task Handle_ValidCode_ChangesUserEmail()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(TargetEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            user.Email.Should().Be(TargetEmail);
            user.IsEmailConfirmed.Should().BeTrue();
            _userRepositoryMock.Verify(x => x.Update(user), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCode_MarksVerificationAsVerified()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(TargetEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            verification.IsValid().Should().BeFalse();
            _emailVerificationRepositoryMock.Verify(x => x.Update(verification), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCode_SavesChanges()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(TargetEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}