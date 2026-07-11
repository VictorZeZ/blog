using blog.Application.Categories.Queries.GetAllCategories;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Categories.Queries
{
    public class GetAllCategoriesQueryHandlerTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly GetAllCategoriesQueryHandler _handler;

        public GetAllCategoriesQueryHandlerTests()
        {
            _handler = new GetAllCategoriesQueryHandler(_categoryRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_NoCategories_ReturnsEmptyList()
        {
            // Arrange
            _categoryRepositoryMock
                .Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _handler.Handle(new GetAllCategoriesQuery(), CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsCorrectCategoryData()
        {
            // Arrange
            var categories = new List<Category>
            {
                new("Technology"),
                new("Lifestyle")
            };

            _categoryRepositoryMock
                .Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(categories);

            // Act
            var result = (await _handler.Handle(new GetAllCategoriesQuery(), CancellationToken.None)).ToList();

            // Assert
            result.Should().HaveCount(2);
            result[0].Name.Should().Be("Technology");
            result[0].Slug.Should().Be("technology");
            result[1].Name.Should().Be("Lifestyle");
        }
    }
}
