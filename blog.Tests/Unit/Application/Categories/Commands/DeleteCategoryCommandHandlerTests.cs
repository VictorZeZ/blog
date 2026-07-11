using blog.Application.Categories.Commands.DeleteCategory;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Categories.Commands
{
    public class DeleteCategoryCommandHandlerTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly DeleteCategoryCommandHandler _handler;

        public DeleteCategoryCommandHandlerTests()
        {
            _handler = new DeleteCategoryCommandHandler(
                _categoryRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        private static DeleteCategoryCommand ValidCommand(Guid categoryId) => new()
        {
            ActorId = Guid.NewGuid(),
            CategoryId = categoryId
        };

        private static Category ValidCategory => new("Technology");

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            var category = ValidCategory;
            var command = ValidCommand(category.Id.Value);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = ValidCommand(Guid.NewGuid());

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(new CategoryId(command.CategoryId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Category?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadyDeletedCategory_ThrowsInvalidStateException()
        {
            // Arrange
            var category = ValidCategory;
            category.SoftDelete();
            var command = ValidCommand(category.Id.Value);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_SoftDeletesCategory()
        {
            // Arrange
            var category = ValidCategory;
            var command = ValidCommand(category.Id.Value);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _categoryRepositoryMock.Verify(
                x => x.SoftDelete(category),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var category = ValidCategory;
            var command = ValidCommand(category.Id.Value);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_AlreadyDeletedCategory_DoesNotSaveChanges()
        {
            // Arrange
            var category = ValidCategory;
            category.SoftDelete();
            var command = ValidCommand(category.Id.Value);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidStateException>();

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
