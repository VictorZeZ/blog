using blog.Api.Common;
using blog.Api.DTOs.Posts;
using blog.Application.Posts.Commands.CreatePost;
using blog.Application.Posts.Commands.DeletePost;
using blog.Application.Posts.Commands.UpdatePost;
using blog.Application.Posts.Queries.GetAllPublishedPosts;
using blog.Application.Posts.Queries.GetPostBySlug;
using blog.Application.Posts.Queries.GetPostsByAuthor;
using blog.Application.Posts.Queries.GetPostsByTag;
using blog.Application.Posts.Queries.SearchPosts;
using blog.Domain.Common;
using blog.Domain.Posts.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace blog.Api.Controllers
{
    public class PostsController(IMediator mediator) : ApiController(mediator)
    {
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostRequest request, CancellationToken ct)
        {
            var command = new CreatePostCommand
            {
                AuthorId = CurrentUserId,
                Title = request.Title,
                Content = request.Content,
                Tags = request.Tags,
                TitleImageStream = request.TitleImage?.OpenReadStream(),
                TitleImageFileName = request.TitleImage?.FileName
            };

            var result = await Mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetPostBySlug), new { slug = result.Slug }, result);
        }

        [HttpPut("{postId:guid}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(Guid postId, [FromForm] UpdatePostRequest request, CancellationToken ct)
        {
            var command = new UpdatePostCommand
            {
                ActorId = CurrentUserId,
                PostId = postId,
                Title = request.Title,
                Content = request.Content,
                Tags = request.Tags,
                TitleImageStream = request.TitleImage?.OpenReadStream(),
                TitleImageFileName = request.TitleImage?.FileName,
                RemoveTitleImage = request.RemoveTitleImage
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpDelete("{postId:guid}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(Guid postId, CancellationToken ct)
        {
            var command = new DeletePostCommand
            {
                ActorId = CurrentUserId,
                PostId = postId
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllPublishedPosts([FromQuery] PagedRequest paging, [FromQuery] PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = new GetAllPublishedPostsQuery { Paging = paging, SortBy = sortBy };
            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPosts([FromQuery] string term, [FromQuery] PagedRequest paging, [FromQuery] PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = new SearchPostsQuery { Term = term, Paging = paging, SortBy = sortBy };
            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("tag/{tag}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPostsByTag(string tag, [FromQuery] PagedRequest paging, [FromQuery] PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = new GetPostsByTagQuery { Tag = tag, Paging = paging, SortBy = sortBy };
            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("author/{authorId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPostsByAuthor(Guid authorId, [FromQuery] PagedRequest paging, [FromQuery] PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = new GetPostsByAuthorQuery
            {
                AuthorId = authorId,
                ActorId = CurrentUserIdOrNull,
                Paging = paging,
                SortBy = sortBy
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPostBySlug(string slug, CancellationToken ct)
        {
            var query = new GetPostBySlugQuery { Slug = slug, ActorId = CurrentUserIdOrNull };
            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }
    }
}
