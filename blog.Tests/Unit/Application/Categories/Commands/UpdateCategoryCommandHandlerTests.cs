using blog.Application.Categories.Commands.UpdateCategory;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Categories.Commands
{
    public class UpdateCategoryCommandHandlerTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly UpdateCategoryCommandHandler _handler;

        public UpdateCategoryCommandHandlerTests()
        {
            _handler = new UpdateCategoryCommandHandler(
                _categoryRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        private static UpdateCategoryCommand ValidCommand(Guid categoryId) => new()
        {
            ActorId = Guid.NewGuid(),
            CategoryId = categoryId,
            Name = "Programming"
        };

        private static Category ValidCategory => new("Technology");

        [Fact]
        public async Task Handle_ValidCommand_ReturnsUpdateCategoryResponse()
        {
            // Arrange
            var category = ValidCategory;
            var command = ValidCommand(category.Id.Value);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            _categoryRepositoryMock
                .Setup(x => x.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(command.Name);
            result.Slug.Should().Be("programming");
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
        public async Task Handle_NameChangedToExistingName_ThrowsAlreadyExistsException()
        {
            // Arrange
            var category = ValidCategory;
            var command = ValidCommand(category.Id.Value);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            _categoryRepositoryMock
                .Setup(x => x.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_NameUnchanged_DoesNotCheckNameExistence()
        {
            // Arrange
            var category = ValidCategory;
            var command = new UpdateCategoryCommand
            {
                ActorId = Guid.NewGuid(),
                CategoryId = category.Id.Value,
                Name = category.Name
            };

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _categoryRepositoryMock.Verify(
                x => x.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesCategoryInRepository()
        {
            // Arrange
            var category = ValidCategory;
            var command = ValidCommand(category.Id.Value);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            _categoryRepositoryMock
                .Setup(x => x.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _categoryRepositoryMock.Verify(
                x => x.Update(It.IsAny<Category>()),
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
    }
}
