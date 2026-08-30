using blog.Application.Posts.Commands.PublishDraft;
using blog.Domain.Categories.Entities;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Repository;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Commands
{
    public class PublishDraftCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private readonly PublishDraftCommandHandler _handler;

        public PublishDraftCommandHandlerTests()
        {
            _handler = new PublishDraftCommandHandler(
                _userRepositoryMock.Object,
                _postRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        private static User CreateUser(
            string email,
            UserLevel level)
        {
            var user = new User(
                email,
                "Ali",
                "Rezaei",
                "hashed_password");

            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        private static Post CreateDraft(User author)
        {
            return Post.CreateDraft(
                "My Draft",
                "Draft summary",
                null,
                "Draft content",
                ["dotnet"],
                author,
                new Category("Technology"));
        }

        private void SetupUser(User user)
        {
            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    user.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
        }

        private void SetupPost(Post post)
        {
            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    post.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);
        }

        [Fact]
        public async Task Handle_ValidDraftOwnedByActor_PublishesDraft()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraft(actor);

            var command = new PublishDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value
            };

            SetupUser(actor);
            SetupPost(post);

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(post.Id.Value);
            result.Title.Should().Be(post.Title);
            result.Slug.Should().Be(post.Slug);

            // Normal author remains subject to approval.
            result.Status.Should().Be(PostStatus.PendingApproval);
            post.Status.Should().Be(PostStatus.PendingApproval);
        }

        [Fact]
        public async Task Handle_DraftOwnedByAuthorLevelUser_PublishesDirectly()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Author);

            var post = CreateDraft(actor);

            var command = new PublishDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value
            };

            SetupUser(actor);
            SetupPost(post);

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.Status.Should().Be(PostStatus.Published);
            post.Status.Should().Be(PostStatus.Published);
        }

        [Fact]
        public async Task Handle_ElevatedActorCanPublishAnotherUsersDraft()
        {
            // Arrange
            var author = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var admin = CreateUser(
                "admin@test.com",
                UserLevel.Admin);

            var post = CreateDraft(author);

            var command = new PublishDraftCommand
            {
                ActorId = admin.Id.Value,
                PostId = post.Id.Value
            };

            SetupUser(admin);
            SetupPost(post);

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.Status.Should().Be(PostStatus.PendingApproval);
        }

        [Fact]
        public async Task Handle_ActorNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actorId = Guid.NewGuid();

            var command = new PublishDraftCommand
            {
                ActorId = actorId,
                PostId = Guid.NewGuid()
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    new UserId(actorId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DeletedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var actor = CreateUser(
                "actor@test.com",
                UserLevel.Author);

            actor.SoftDelete();

            var command = new PublishDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = Guid.NewGuid()
            };

            SetupUser(actor);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_BannedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var actor = CreateUser(
                "actor@test.com",
                UserLevel.Author);

            actor.Ban();

            var command = new PublishDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = Guid.NewGuid()
            };

            SetupUser(actor);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_PostNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actor = CreateUser(
                "actor@test.com",
                UserLevel.Author);

            var postId = Guid.NewGuid();

            var command = new PublishDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = postId
            };

            SetupUser(actor);

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    new PostId(postId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Post?)null);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_UnrelatedNormalActor_ThrowsForbiddenException()
        {
            // Arrange
            var author = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var actor = CreateUser(
                "other@test.com",
                UserLevel.Normal);

            var post = CreateDraft(author);

            var command = new PublishDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value
            };

            SetupUser(actor);
            SetupPost(post);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_PublishedPost_ThrowsInvalidStateException()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Author);

            var post = new Post(
                "Published Post",
                "Summary",
                null,
                "Content",
                ["dotnet"],
                actor,
                new Category("Technology"));

            var command = new PublishDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value
            };

            SetupUser(actor);
            SetupPost(post);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesRepository()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Author);

            var post = CreateDraft(actor);

            var command = new PublishDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value
            };

            SetupUser(actor);
            SetupPost(post);

            // Act
            await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.Update(post),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Author);

            var post = CreateDraft(actor);

            var command = new PublishDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value
            };

            SetupUser(actor);
            SetupPost(post);

            // Act
            await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}