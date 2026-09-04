# blog

A modern blog platform **API** built with **.NET 10**, following **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS** principles.

The API provides authentication, account management, post authoring, drafts, publishing and moderation workflows, categories, tags, search, user administration, status reporting, and a role-aware dashboard.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Domain Model](#domain-model)
- [User Levels](#user-levels)
- [Post Workflow](#post-workflow)
- [Setup](#setup)
- [Configuration Reference](#configuration-reference)
- [API Endpoints](#api-endpoints)
- [Dashboard](#dashboard)
- [Error Handling](#error-handling)
- [Security](#security)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

## Features

### Authentication and accounts

- Registration with email verification using OTP codes.
- Login with email/password.
- Optional email-based two-factor login verification.
- JWT access tokens and refresh tokens.
- Refresh-token rotation.
- Refresh-token revocation on logout and security-sensitive account changes.
- Account lockout after repeated failed login attempts.
- Password change.
- Password reset through email OTP.
- Email change with a two-step verification flow.
- Resend registration verification codes.
- Resend login verification codes.
- Resend password-reset codes.
- Resend email-change verification codes.
- Profile update.
- Account deletion with soft deletion.

### Posts

- Create posts with an optional title image.
- Create and manage drafts.
- Update posts and drafts.
- Publish drafts.
- Delete posts.
- Post approval workflow.
- Approve or reject pending posts.
- Categories.
- Multiple tags per post.
- Related-post lookup by tags.
- PostgreSQL full-text search.
- View counting for published posts.
- Pagination.
- Sorting.
- Filtering by author, category, tag, and post status.
- Author-aware visibility for unpublished content.
- Cloudinary image storage.
- Server-side image validation.

### Users and administration

- Public user search.
- Authenticated user profile management.
- Admin user listing.
- User search/filter/sort support.
- Ban and unban users.
- User-level promotion and demotion.
- Role-aware access control.
- Dedicated administration endpoints for content moderation.
- Platform and post status reports.

### Dashboard

The authenticated dashboard returns data based on the current user's level.

It can include:

- Profile information.
- Personal content statistics.
- Draft count.
- Pending-approval count.
- Published count.
- Rejected count.
- Total views of the user's posts.
- Author insights with posts per day.
- Moderation queue statistics.
- Active category count.
- Platform statistics.
- Total users.
- Banned users.
- Total posts.
- Total views.
- Registrations per day.
- Owner-only user-level overview.

### Cross-cutting concerns

- Clean Architecture.
- Domain-Driven Design.
- CQRS with MediatR.
- FluentValidation.
- Pipeline behaviors.
- Strongly typed IDs.
- Repository pattern.
- Unit of Work.
- Centralized domain exception handling.
- Development request/response logging.
- Security response headers.
- CORS.
- Global rate limiting.
- Stricter rate limiting for authentication endpoints.
- Kestrel request-size limits.
- JWT bearer authentication and authorization.
- OpenAPI documentation through Scalar.

## Architecture

The solution follows Clean Architecture with this dependency direction:

```text
blog.Api
    ↓
blog.Application
    ↓
blog.Domain
    ↑
blog.Infrastructure
```

The `Domain` layer is independent of the other layers. Infrastructure implements domain abstractions such as repositories and services.

| Project | Responsibility |
|---|---|
| `blog.Domain` | Entities, value objects, strongly typed IDs, enums, domain exceptions, repository interfaces, settings, and domain abstractions. |
| `blog.Application` | CQRS commands and queries, MediatR handlers, validators, and pipeline behaviors. |
| `blog.Infrastructure` | EF Core/PostgreSQL persistence, repositories, JWT services, password hashing, Cloudinary, email delivery, and other external integrations. |
| `blog.Api` | ASP.NET Core Web API, controllers, DTOs, middleware, dependency injection, authentication, authorization, rate limiting, CORS, and OpenAPI/Scalar. |
| `blog.Tests` | Unit and automated tests for application use cases, including CQRS commands and queries. |

### Key patterns

- **CQRS with MediatR** — each use case is represented by a command or query and its handler.
- **Validation pipeline** — FluentValidation runs before handlers.
- **Actor authorization pipeline** — privileged requests can require a minimum `UserLevel`.
- **Domain exceptions** — business-rule failures use typed exceptions such as `NotFoundException`, `ForbiddenException`, and `ValidationException`.
- **Strongly typed IDs** — IDs such as `UserId`, `PostId`, and `CategoryId` wrap `Guid` values.
- **Repository pattern** — aggregate access is isolated behind domain repository interfaces.
- **Unit of Work** — persistence changes are committed through `IUnitOfWork`.
- **Soft deletion** — applicable entities can be removed without immediately deleting their database records.

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL
- Npgsql
- MediatR
- FluentValidation
- JWT Bearer authentication
- Argon2 password hashing
- SHA-256 hashing for stored refresh tokens and verification codes
- Cloudinary
- MailKit / SMTP
- Scalar / OpenAPI

## Project Structure

```text
blog/
├── blog.Api/
│   ├── Controllers/
│   │   ├── AccountController.cs
│   │   ├── AdminController.cs
│   │   ├── AuthController.cs
│   │   ├── CategoriesController.cs
│   │   ├── DashboardController.cs
│   │   ├── PostsController.cs
│   │   └── UsersController.cs
│   ├── DTOs/
│   │   ├── Users/
│   │   ├── Posts/
│   │   └── Categories/
│   ├── Middlewares/
│   ├── Extensions/
│   ├── Common/
│   ├── Properties/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── user-secrets.md
│   └── Program.cs
│
├── blog.Application/
│   ├── Users/
│   │   ├── Commands/
│   │   │   ├── Register
│   │   │   ├── Login
│   │   │   ├── ConfirmEmail
│   │   │   ├── ConfirmLogin
│   │   │   ├── RefreshToken
│   │   │   ├── Logout
│   │   │   ├── ResendRegistrationCode
│   │   │   ├── ResendLoginVerificationCode
│   │   │   ├── ResendResetPasswordCode
│   │   │   ├── ChangePassword
│   │   │   ├── ChangeEmail
│   │   │   ├── ConfirmChangeEmail
│   │   │   ├── ResendChangeEmailCode
│   │   │   ├── ConfirmNewEmail
│   │   │   ├── ForgotPassword
│   │   │   ├── ResetPassword
│   │   │   ├── TwoFactor
│   │   │   ├── UpdateUser
│   │   │   ├── DeleteAccount
│   │   │   ├── BanUser
│   │   │   └── ChangeUserLevel
│   │   └── Queries/
│   │       ├── GetUserById
│   │       ├── GetUsers
│   │       └── SearchUsers
│   │
│   ├── Posts/
│   │   ├── Commands/
│   │   │   ├── CreatePost
│   │   │   ├── UpdatePost
│   │   │   ├── DeletePost
│   │   │   ├── CreateDraft
│   │   │   ├── UpdateDraft
│   │   │   ├── PublishDraft
│   │   │   └── ChangePostStatus
│   │   └── Queries/
│   │       ├── GetAllPosts
│   │       ├── GetAllPublishedPosts
│   │       ├── GetPostBySlug
│   │       ├── GetPostsByAuthor
│   │       ├── GetPostsByCategory
│   │       ├── GetPostsByTag
│   │       ├── GetPendingApprovalPosts
│   │       ├── GetPostStatusReport
│   │       ├── GetUserPostStatusReport
│   │       └── SearchPosts
│   │
│   ├── Categories/
│   │   ├── Commands/
│   │   │   ├── CreateCategory
│   │   │   ├── UpdateCategory
│   │   │   └── DeleteCategory
│   │   └── Queries/
│   │       ├── GetAllCategories
│   │       └── GetCategoryBySlug
│   │
│   ├── Dashboard/
│   │   └── Queries/
│   │       └── GetDashboard/
│   │
│   └── Common/
│       ├── ValidationBehavior
│       └── ActorAuthorizationBehavior
│
├── blog.Domain/
│   ├── Users/
│   ├── Posts/
│   ├── Categories/
│   ├── Tokens/
│   ├── EmailVerifications/
│   ├── Common/
│   └── Exceptions/
│
├── blog.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   ├── Extensions/
│   │   ├── Migrations/
│   │   └── AppDbContext.cs
│   ├── Repositories/
│   ├── Services/
│   └── DependencyInjection.cs
│
├── blog.Tests/
│   └── Unit/
│       └── Application/
│
├── blog.slnx
├── .gitignore
├── .gitattributes
└── README.md
```

## Domain Model

| Aggregate | Important properties | Notes |
|---|---|---|
| `User` | `Email`, `FirstName`, `LastName`, `PasswordHash`, `Level`, `IsBanned`, `IsDeleted`, `FailedLoginAttempts`, `LockedOutUntil`, `IsEmailConfirmed`, `TwoFactorEnabled` | Uses the `Normal` → `Author` → `Admin` → `Owner` level hierarchy. |
| `Post` | `Title`, `Summary`, `Slug`, `Content`, `TitleImageUrl`, `Tags`, `Status`, `CategoryId`, `AuthorId`, `ViewCount`, `SearchVector` | Supports drafts, publishing, moderation, categories, tags, search, and view counting. |
| `Category` | `Name`, `Slug` | Soft-deletable and slug-based. |
| `RefreshToken` | `TokenHash`, `ExpiresAt`, `Status`, `DeviceInfo` | Rotated on refresh and revoked by security-sensitive account operations. |
| `EmailVerification` | `CodeHash`, `Purpose`, `TargetEmail`, `ExpiresAt`, `Status`, `AttemptCount` | Used for registration, login verification, email changes, and password reset. |

## User Levels

The application uses four user levels:

```text
Normal
  ↓
Author
  ↓
Admin
  ↓
Owner
```

The level determines which privileged operations an actor can perform.

- **Normal** — standard account features.
- **Author** — authoring features and personal content management.
- **Admin** — user moderation, category management, and post approval.
- **Owner** — highest privilege level and platform-level administration/reporting.

The exact authorization rules are enforced by the application layer and actor-authorization pipeline.

## Post Workflow

Posts can move through the following states:

```text
Draft
  │
  └── Publish ──→ PendingApproval
                       │
                 ┌─────┴─────┐
                 ↓           ↓
             Published    Rejected
```

### Drafts

Drafts allow authors to save incomplete content without sending it to the public or the moderation queue.

Available operations:

- Create a draft.
- Update a draft.
- Publish a draft.
- Delete a draft/post according to authorization rules.

### Publishing

A published post is visible through the public post endpoints.

Posts created by users with elevated privileges can follow the project's direct-publishing rules. Posts that require moderation enter the `PendingApproval` state.

### Moderation

Admins can inspect pending posts and approve or reject them.

## Setup

This section takes you from a clean machine to a running API.

### 1. Install prerequisites

| Tool | Purpose |
|---|---|
| .NET 10 SDK | Build and run the API |
| PostgreSQL 13+ | Application database |
| Git | Clone the repository |
| Cloudinary account | Store post title images |
| SMTP-capable email account | Send OTP and verification emails |

Verify the .NET SDK:

```bash
dotnet --version
```

The command should return a `10.x.x` version.

### 2. Clone the repository

```bash
git clone https://github.com/VictorZeZ/blog.git
cd blog
```

### 3. Restore dependencies

```bash
dotnet restore
```

### 4. Create the PostgreSQL database

Create an empty database:

```bash
psql -U postgres -c "CREATE DATABASE blog;"
```

Do not create the application tables manually. EF Core migrations create them.

The application also uses PostgreSQL `pg_trgm` for trigram-based user search. The migrations create the required extension and indexes.

### 5. Configure user secrets

Sensitive configuration is kept outside source control through .NET User Secrets.

See [`blog.Api/user-secrets.md`](blog.Api/user-secrets.md) for the complete configuration guide.

Required configuration includes:

- PostgreSQL connection string.
- JWT settings.
- Cloudinary settings.
- SMTP/email settings.

### 6. Install EF Core CLI

```bash
dotnet tool install --global dotnet-ef
```

If the tool is already installed:

```bash
dotnet tool update --global dotnet-ef
```

### 7. Apply migrations

```bash
dotnet ef database update \
  --project blog.Infrastructure \
  --startup-project blog.Api
```

### 8. Build

```bash
dotnet build
```

### 9. Run

```bash
dotnet run --project blog.Api
```

The default local URLs are:

```text
http://localhost:5005
https://localhost:7007
```

In Development, Scalar is available at:

```text
/scalar
```

It provides interactive OpenAPI documentation.

### 10. Verify registration

Example:

```bash
curl -X POST http://localhost:5005/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","firstName":"Test","lastName":"User","password":"Password123!"}'
```

A successful `201 Created` response confirms that the API can reach the database and create an account.

Check the configured mailbox for the verification code.

## Configuration Reference

| Setting | Location | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | User Secrets | PostgreSQL connection string. |
| `JwtSettings` | User Secrets | JWT signing key, issuer, audience, and token lifetimes. |
| `CloudinarySettings` | User Secrets | Cloudinary credentials for post images. |
| `EmailSettings` | User Secrets | SMTP configuration and sender address. |
| `AccountLockoutSettings` | `appsettings.json` | Failed-login threshold and lockout duration. |
| `EmailVerificationSettings` | `appsettings.json` | OTP expiry and attempt limits by verification purpose. |
| `Cors:AllowedOrigins` | `appsettings.json` | Allowed frontend origins. |

Secret-bound settings are validated at application startup.

## API Endpoints

All routes are relative to `/api/{controller}`.

### Auth — `/api/auth`

Anonymous endpoints are rate-limited by the `auth` policy.

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/register` | Anonymous | Register a new account and send a registration OTP. |
| POST | `/login` | Anonymous | Authenticate. May return a 2FA challenge. |
| POST | `/logout` | Authorized | Revoke a refresh token. |
| POST | `/refresh` | Anonymous | Rotate a refresh token and issue new tokens. |
| POST | `/confirm-email` | Anonymous | Confirm registration with an OTP. |
| POST | `/confirm-login` | Anonymous | Complete a 2FA login challenge. |
| POST | `/forgot-password` | Anonymous | Start password reset. |
| POST | `/reset-password` | Anonymous | Reset the password using an OTP. |
| POST | `/resend-registration-code` | Anonymous | Resend the registration OTP. |
| POST | `/resend-login-code` | Anonymous | Resend the login verification OTP. |
| POST | `/resend-reset-password-code` | Anonymous | Resend the password-reset OTP. |

### Account — `/api/account`

All account endpoints require authentication.

| Method | Route | Description |
|---|---|---|
| GET | `/me` | Get the current user's profile. |
| PUT | `/me` | Update first and last name. |
| PUT | `/me/password` | Change the current password and revoke active refresh tokens. |
| PUT | `/me/two-factor` | Enable or disable two-factor login verification. |
| POST | `/me/change-email` | Start an email-change flow. |
| POST | `/me/change-email/confirm` | Confirm the current identity using an OTP. |
| POST | `/me/change-email/confirm-new` | Confirm the new email using an OTP. |
| POST | `/me/change-email/resend` | Resend the current email-change verification code. |
| DELETE | `/me` | Soft-delete the account. |

### Posts — `/api/posts`

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/` | Authorized | Create a post. Supports `multipart/form-data` and an optional title image. |
| PUT | `/{postId}` | Authorized | Update a post. |
| DELETE | `/{postId}` | Authorized | Delete a post. |
| POST | `/drafts` | Authorized | Create a draft. |
| PUT | `/drafts/{postId}` | Authorized | Update a draft. |
| POST | `/drafts/{postId}/publish` | Authorized | Publish a draft. |
| GET | `/` | Anonymous | List published posts with paging and sorting. |
| GET | `/search` | Anonymous | Search published posts using PostgreSQL full-text search. |
| GET | `/related` | Anonymous | Find posts related to one or more tags. |
| GET | `/category/{categorySlug}` | Anonymous | List published posts in a category. |
| GET | `/author/{authorId}` | Anonymous | List posts by an author. Visibility depends on the caller. |
| GET | `/{slug}` | Anonymous | Get a post by slug and increment its view count when published. |

#### Related posts query parameters

`GET /api/posts/related` accepts:

| Parameter | Description |
|---|---|
| `tags` | One or more tags used to find related posts. |
| `groupingMode` | Controls how multiple tags are combined. |
| `paging` | Standard paging parameters. |
| `sortBy` | Post sorting option. |

### Categories — `/api/categories`

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/` | Anonymous | List active categories. |
| GET | `/{slug}` | Anonymous | Get a category by slug. |
| POST | `/` | Authorized | Create a category. Requires the appropriate elevated level. |
| PUT | `/{categoryId}` | Authorized | Rename a category. Requires the appropriate elevated level. |
| DELETE | `/{categoryId}` | Authorized | Soft-delete a category. Requires the appropriate elevated level. |

### Users — `/api/users`

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/search` | Anonymous | Search users by name or email. Email visibility depends on the caller. |

### Admin — `/api/admin`

The endpoint requires authentication. Individual operations enforce the required actor level.

| Method | Route | Minimum level | Description |
|---|---|---|---|
| GET | `/users` | Admin | List users with paging, sorting, and filtering. |
| PUT | `/users/{targetUserId}/ban` | Admin | Ban or unban a user. |
| PUT | `/users/{targetUserId}/level` | Admin | Change a user's level within the allowed hierarchy. |
| GET | `/posts` | Owner | List all posts regardless of publication status. |
| GET | `/posts/pending` | Admin | List posts waiting for approval. |
| PUT | `/posts/{postId}/status` | Admin | Approve or reject a post. |
| GET | `/posts/status-report` | Admin/Owner | Get platform post-status statistics for a date range. |
| GET | `/posts/{authorId}/status-report` | Admin/Owner | Get post-status statistics for one author and a date range. |

For both status-report endpoints, `from` and `to` are required `DateOnly` query parameters.

Example:

```text
GET /api/admin/posts/status-report?from=2026-09-01&to=2026-09-30
```

## Dashboard

### `GET /api/dashboard`

The dashboard is authenticated and uses the current user's ID.

The response contains a base profile and personal content data. Additional sections are returned when the user's level allows them.

Conceptually, the response contains:

```text
GetDashboardResponse
├── Profile
├── MyContent
├── AuthorInsights?
├── ModerationQueue?
├── PlatformStats?
└── OwnerOverview?
```

### Profile

Contains the current user's profile and level information.

### MyContent

Contains:

- `DraftCount`
- `PendingApprovalCount`
- `PublishedCount`
- `RejectedCount`
- `TotalViewCount`

### AuthorInsights

Available for applicable author-level users.

Contains:

- `PostsPerDay`

### ModerationQueue

Available for applicable moderation-level users.

Contains:

- `PendingApprovalCount`
- `ActiveCategoryCount`

### PlatformStats

Available for platform administrators.

Contains:

- `TotalUserCount`
- `BannedUserCount`
- `TotalPostCount`
- `TotalViewCount`
- `RegistrationsPerDay`

### OwnerOverview

Available to the owner.

Contains counts for:

- Normal users.
- Authors.
- Admins.
- Owners.

## Error Handling

Domain exceptions are converted into a consistent JSON response by `DomainExceptionResponseWriter`.

Example:

```json
{
  "statusCode": 404,
  "errorCode": "NOT_FOUND",
  "title": "Post not found",
  "details": {
    "resource": "Post",
    "id": "..."
  }
}
```

Unhandled exceptions are logged and returned as a generic `500 UNKNOWN_ERROR`.

The underlying exception message is exposed only in Development.

## Security

- JWT bearer authentication with issuer, audience, lifetime, and signing-key validation.
- Zero clock skew for JWT validation.
- Argon2 password hashing.
- SHA-256 hashing for refresh tokens and verification codes stored in the database.
- Account lockout after repeated failed login attempts.
- Optional email-based two-factor login verification.
- Role/level-aware authorization.
- CQRS actor-authorization behavior for privileged requests.
- Global sliding-window rate limiting.
- Stricter rate limiting for authentication endpoints.
- HTTPS redirection and HSTS.
- Security response headers.
- Configurable CORS.
- Kestrel request-body size limits.
- Image size validation.
- Image content-type validation.
- Image magic-byte validation before upload.
- Cloudinary-backed image storage.

## Testing

The automated test suite is located in `blog.Tests`.

Run all tests with:

```bash
dotnet test
```

The test suite includes coverage for application CQRS commands and queries, including newer draft, publishing, dashboard, and reporting use cases.

## Troubleshooting

### `dotnet ef` command not found

Install the EF Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

Restart the terminal if the command is still not available.

### Startup fails because configuration is invalid

Check the values configured with:

```bash
dotnet user-secrets list
```

Run the command from the `blog.Api` project directory.

Review [`blog.Api/user-secrets.md`](blog.Api/user-secrets.md).

### Database connection fails

Check that:

- PostgreSQL is running.
- The `blog` database exists.
- `ConnectionStrings:DefaultConnection` is correct.
- The PostgreSQL host and port are correct.
- The configured database user has access to the database.

### `pg_trgm` migration fails

The PostgreSQL user may not have permission to create extensions.

Run:

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

with a sufficiently privileged database user, then run the migration again.

### `401 Unauthorized`

Make sure the request contains:

```text
Authorization: Bearer <access-token>
```

Access tokens are short-lived. Use:

```text
POST /api/auth/refresh
```

to rotate a valid refresh token and obtain a new access token.

### `429 Too Many Requests`

Authentication endpoints use a stricter rate-limit policy.

Wait for the sliding window to reset before sending more requests.

### Verification emails are not received

Check:

- SMTP host.
- SMTP port.
- SMTP username.
- SMTP password.
- `FromAddress`.
- Spam/junk folders.

For Gmail, use an App Password instead of the normal account password.

### Post image upload fails

The API validates:

- File size.
- Allowed content type.
- File signature/magic bytes.

The current maximum image size is **5 MB**.

Allowed image formats are:

- JPEG
- PNG
- WEBP
- GIF

Cloudinary credentials must also be configured correctly.

### CORS errors

`Cors:AllowedOrigins` is empty by default.

Add the frontend origin to the configuration.

For local development, an example is:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000"
    ]
  }
}
```

### Ports are already in use

The default ports are:

```text
HTTP  5005
HTTPS 7007
```

Change them in:

```text
blog.Api/Properties/launchSettings.json
```

### HTTPS certificate warning

Trust the ASP.NET Core development certificate:

```bash
dotnet dev-certs https --trust
```

### Entity changes are not reflected in PostgreSQL

Create a new migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project blog.Infrastructure \
  --startup-project blog.Api
```

Then apply it:

```bash
dotnet ef database update \
  --project blog.Infrastructure \
  --startup-project blog.Api
```
