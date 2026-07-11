using blog.Api.Common;
using blog.Api.DTOs.Categories;
using blog.Application.Categories.Commands.CreateCategory;
using blog.Application.Categories.Commands.DeleteCategory;
using blog.Application.Categories.Commands.UpdateCategory;
using blog.Application.Categories.Queries.GetAllCategories;
using blog.Application.Categories.Queries.GetCategoryBySlug;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace blog.Api.Controllers
{
    public class CategoriesController(IMediator mediator) : ApiController(mediator)
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories(CancellationToken ct)
        {
            var result = await Mediator.Send(new GetAllCategoriesQuery(), ct);
            return Ok(result);
        }

        [HttpGet("{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryBySlug(string slug, CancellationToken ct)
        {
            var query = new GetCategoryBySlugQuery { Slug = slug };
            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken ct)
        {
            var command = new CreateCategoryCommand
            {
                ActorId = CurrentUserId,
                Name = request.Name
            };

            var result = await Mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetCategoryBySlug), new { slug = result.Slug }, result);
        }

        [HttpPut("{categoryId:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateCategory(Guid categoryId, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
        {
            var command = new UpdateCategoryCommand
            {
                ActorId = CurrentUserId,
                CategoryId = categoryId,
                Name = request.Name
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpDelete("{categoryId:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteCategory(Guid categoryId, CancellationToken ct)
        {
            var command = new DeleteCategoryCommand
            {
                ActorId = CurrentUserId,
                CategoryId = categoryId
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }
    }
}
