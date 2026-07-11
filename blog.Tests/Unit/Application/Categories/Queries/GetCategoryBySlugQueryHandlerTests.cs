using blog.Application.Categories.Queries.GetCategoryBySlug;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Categories.Queries
{
    public class GetCategoryBySlugQueryHandlerTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly GetCategoryBySlugQueryHandler _handler;

        public GetCategoryBySlugQueryHandlerTests()
        {
            _handler = new GetCategoryBySlugQueryHandler(_categoryRepositoryMock.Object);
        }

        private static Category ValidCategory => new("Technology");

        [Fact]
        public async Task Handle_ValidQuery_ReturnsResponse()
        {
            // Arrange
            var category = ValidCategory;
            var query = new GetCategoryBySlugQuery { Slug = category.Slug };

            _categoryRepositoryMock
                .Setup(x => x.GetBySlugAsync(category.Slug, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(category.Name);
            result.Slug.Should().Be(category.Slug);
        }

        [Fact]
        public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var query = new GetCategoryBySlugQuery { Slug = "non-existent" };

            _categoryRepositoryMock
                .Setup(x => x.GetBySlugAsync(query.Slug, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Category?)null);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DeletedCategory_ThrowsNotFoundException()
        {
            // Arrange
            var category = ValidCategory;
            category.SoftDelete();
            var query = new GetCategoryBySlugQuery { Slug = category.Slug };

            _categoryRepositoryMock
                .Setup(x => x.GetBySlugAsync(category.Slug, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
