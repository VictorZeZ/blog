using blog.Application.Categories.Commands.CreateCategory;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Categories.Commands
{
    public class CreateCategoryCommandHandlerTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly CreateCategoryCommandHandler _handler;

        public CreateCategoryCommandHandlerTests()
        {
            _handler = new CreateCategoryCommandHandler(
                _categoryRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        private static CreateCategoryCommand ValidCommand => new()
        {
            ActorId = Guid.NewGuid(),
            Name = "Technology"
        };

        [Fact]
        public async Task Handle_ValidCommand_ReturnsCreateCategoryResponse()
        {
            // Arrange
            var command = ValidCommand;

            _categoryRepositoryMock
                .Setup(x => x.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(command.Name);
            result.Slug.Should().Be("technology");
            result.Id.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_DuplicateName_ThrowsAlreadyExistsException()
        {
            // Arrange
            var command = ValidCommand;

            _categoryRepositoryMock
                .Setup(x => x.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_AddsCategoryToRepository()
        {
            // Arrange
            var command = ValidCommand;

            _categoryRepositoryMock
                .Setup(x => x.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _categoryRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var command = ValidCommand;

            _categoryRepositoryMock
                .Setup(x => x.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_DuplicateName_DoesNotAddOrSaveChanges()
        {
            // Arrange
            var command = ValidCommand;

            _categoryRepositoryMock
                .Setup(x => x.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<AlreadyExistsException>();

            // Assert
            _categoryRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
