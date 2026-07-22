# blog

A blog platform **API** built with **.NET 10**, following **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS** principles.

It provides everything a blogging platform needs on the backend: JWT-based authentication with email verification and optional two-factor login, role-based authorization (`Normal` → `Author` → `Admin` → `Owner`), post authoring with an approval workflow, categories, tagging, full-text search, and an admin surface for moderating users and content — all built on PostgreSQL and EF Core.

This README covers the architecture, how to get the project running from scratch, the full API surface, and common issues you may run into along the way.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Domain Model](#domain-model)
- [Setup](#setup)
- [Configuration Reference](#configuration-reference)
- [API Endpoints](#api-endpoints)
- [Error Handling](#error-handling)
- [Security](#security)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

## Features

- **Authentication & Accounts** — registration with email verification (OTP), login with optional two-factor email verification, JWT access/refresh tokens with rotation, account lockout after repeated failed logins, password change/reset, email change with two-step confirmation, and account deletion.
- **Posts** — creation, update, and deletion with image upload (Cloudinary), tagging, categories, an approval workflow (pending approval / published / rejected), full-text search (PostgreSQL `tsvector`), view counting, and multiple listing/sort/filter options (by author, category, tag, or search term).
- **Categories** — CRUD management for post categories.
- **Users** — profile management, admin user listing/search, banning, and role/level promotion.
- **Admin** — dedicated endpoints for managing users, posts, and the approval queue based on actor level.
- **Cross-cutting concerns** — centralized domain exception handling, request/response logging (development only), security headers, CORS, rate limiting (with a stricter policy on auth endpoints), request size limits, and JWT bearer authentication/authorization.

## Architecture

The solution follows Clean Architecture with a strict dependency flow: `Api` → `Application` → `Domain` ← `Infrastructure`. The `Domain` project has no dependency on anything else; every other project depends inward on it.

| Project | Responsibility |
|---|---|
| `blog.Domain` | Entities, value objects (strongly-typed IDs), enums, domain exceptions, repository interfaces, and domain settings/abstractions. Framework-agnostic. |
| `blog.Application` | CQRS commands/queries and their handlers (MediatR), FluentValidation validators, and cross-cutting pipeline behaviors (validation, actor authorization). |
| `blog.Infrastructure` | EF Core persistence (PostgreSQL/Npgsql), repository implementations, JWT/password hashing services, Cloudinary file storage, and email sending (MailKit). |
| `blog.Api` | ASP.NET Core Web API — controllers, DTOs, middlewares, and application composition (DI, auth, rate limiting, OpenAPI). |
| `blog.Tests` | Automated test suite covering the CQRS commands and queries. |

### Key patterns

- **CQRS with MediatR** — every use case is an `IRequest`/`IRequestHandler` pair under `Commands`/`Queries` folders per feature.
- **Pipeline behaviors** — `ValidationBehavior` runs FluentValidation before every handler; `ActorAuthorizationBehavior` enforces a minimum `UserLevel` for requests implementing `IRequireActorLevel`.
- **Domain exceptions** — every business-rule violation throws a typed `DomainException` (e.g. `NotFoundException`, `ForbiddenException`, `ValidationException`), mapped to a consistent JSON error response by `ExceptionMiddleware`.
- **Strongly-typed IDs** — `record struct` wrappers (e.g. `UserId`, `PostId`) around a `Guid` (v7), avoiding primitive obsession.
- **Repository + Unit of Work** — repositories expose read/write operations per aggregate; `IUnitOfWork` commits changes through EF Core's `SaveChangesAsync`.

## Tech Stack

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 + Npgsql (PostgreSQL)
- MediatR (CQRS mediator)
- FluentValidation
- JWT Bearer authentication
- Argon2 password hashing (`Isopoh.Cryptography.Argon2`)
- Cloudinary (image storage)
- MailKit (SMTP email delivery)
- Scalar (OpenAPI documentation UI)

## Project Structure

```
blog/
├── blog.Api/                        # Presentation layer (ASP.NET Core Web API)
│   ├── Controllers/                 # Auth, Account, Admin, Categories, Posts, Users
│   ├── DTOs/
│   │   ├── Users/
│   │   ├── Posts/
│   │   └── Categories/
│   ├── Middlewares/                 # Exception, RequestLogging, SecurityHeaders
│   ├── Extensions/                  # Auth, Cors, Kestrel, OpenApi, RateLimiting, Scalar setup
│   ├── Common/                      # ApiController base, DomainExceptionResponseWriter, RateLimitPolicies
│   ├── Properties/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── user-secrets.md              # Full user secrets setup guide
│   └── Program.cs                   # Composition root / middleware pipeline
│
├── blog.Application/                 # Application layer (CQRS + validation)
│   ├── Users/
│   │   ├── Commands/                # Register, Login, ConfirmEmail, ConfirmLogin, RefreshToken, Logout,
│   │   │                            # ChangePassword, ChangeEmail, ConfirmChangeEmail, ConfirmNewEmail,
│   │   │                            # ForgotPassword, ResetPassword, TwoFactor, UpdateUser, DeleteAccount,
│   │   │                            # BanUser, ChangeUserLevel
│   │   └── Queries/                 # GetUserById, GetUsers, SearchUsers
│   ├── Posts/
│   │   ├── Commands/                # CreatePost, UpdatePost, DeletePost, ChangePostStatus
│   │   └── Queries/                 # GetAllPosts, GetAllPublishedPosts, GetPostBySlug, GetPostsByAuthor,
│   │                                # GetPostsByCategory, GetPostsByTag, GetPendingApprovalPosts, SearchPosts
│   ├── Categories/
│   │   ├── Commands/                # CreateCategory, UpdateCategory, DeleteCategory
│   │   └── Queries/                 # GetAllCategories, GetCategoryBySlug
│   └── Common/                      # ValidationBehavior, ActorAuthorizationBehavior
│
├── blog.Domain/                      # Domain layer (no external dependencies)
│   ├── Users/                        # Entities, Enums, Types (UserId), Repository, Extensions, Common
│   ├── Posts/                        # Entities, Enums, Types (PostId), Repository, Extensions, Common
│   ├── Categories/                   # Entities, Types (CategoryId), Repository
│   ├── Tokens/                       # RefreshToken entity, Enums, Types, Repository
│   ├── EmailVerifications/           # Entity, Enums, Types, Repository, Extensions
│   ├── Common/
│   │   ├── Interfaces/               # IUnitOfWork, IJwtService, IPasswordHasher, IHasher,
│   │   │                             # IFileStorageService, IEmailSender, IEmailService, …
│   │   ├── Settings/                 # JwtSettings, CloudinarySettings, EmailSettings,
│   │   │                             # AccountLockoutSettings, EmailVerificationSettings
│   │   ├── Helpers/                  # EmailNormalizer, SlugGenerator
│   │   ├── Entity.cs / SoftDeletableEntity.cs
│   │   └── PagedRequest.cs / PagedResult.cs
│   └── Exceptions/                   # DomainException + typed subclasses (NotFound, Forbidden, Validation, …)
│
├── blog.Infrastructure/               # Infrastructure layer
│   ├── Persistence/
│   │   ├── Configurations/           # EF Core entity type configurations
│   │   ├── Extensions/               # IQueryable paging/sorting extensions
│   │   ├── Migrations/
│   │   └── AppDbContext.cs
│   ├── Repositories/                 # EF Core repository implementations
│   ├── Services/                     # JwtService, PasswordHasher, Hasher, CloudinaryService,
│   │                                 # SmtpEmailSender, EmailService, EmailTemplateRenderer,
│   │                                 # VerificationCodeGenerator
│   └── DependencyInjection.cs
│
├── blog.Tests/                       # Automated tests (covers all CQRS commands and queries)
│
├── blog.slnx                         # Solution file
├── .gitignore
├── .gitattributes
└── README.md
```

## Domain Model

| Aggregate | Key properties | Notes |
|---|---|---|
| `User` | `Email`, `FirstName`, `LastName`, `PasswordHash`, `Level`, `IsBanned`, `IsDeleted`, `FailedLoginAttempts`/`LockedOutUntil`, `IsEmailConfirmed`, `TwoFactorEnabled` | `UserLevel`: `Normal` → `Author` → `Admin` → `Owner`. Soft-deletable. |
| `Post` | `Title`, `Slug`, `Content`, `TitleImageUrl`, `Tags`, `Status`, `CategoryId`, `AuthorId`, `ViewCount`, `SearchVector` | `PostStatus`: `Draft` → `PendingApproval` → `Published` / `Rejected`. New posts by non-elevated authors start as `PendingApproval`; posts by `Admin`/`Owner` publish immediately. Full-text search via PostgreSQL `tsvector` (GIN index on title + content). |
| `Category` | `Name`, `Slug` | Soft-deletable, unique slug. |
| `RefreshToken` | `TokenHash`, `ExpiresAt`, `Status`, `DeviceInfo` | Rotated on refresh (`Active` → `Used`), revoked on logout/password change/account deletion. |
| `EmailVerification` | `CodeHash`, `Purpose`, `TargetEmail`, `ExpiresAt`, `Status`, `AttemptCount` | Purposes: `Registration`, `LoginVerification`, `ChangeEmail`, `ConfirmNewEmail`, `ResetPassword`. Attempt-limited and time-boxed per purpose via `EmailVerificationSettings`. |

## Setup

This section takes you from a clean machine to a running API. No prior familiarity with the project is assumed.

### 1. Install prerequisites

| Tool | Purpose | Link |
|---|---|---|
| .NET 10 SDK | Build and run the project | https://dotnet.microsoft.com/download |
| PostgreSQL (13+) | Application database | https://www.postgresql.org/download/ |
| Git | Clone the repository | https://git-scm.com/downloads |
| A Cloudinary account (free tier works) | Post image uploads | https://cloudinary.com/users/register/free |
| An SMTP-capable email account | Sending OTP/verification emails | e.g. Gmail with an App Password |

Verify the SDK is installed:

```bash
dotnet --version
# should print a 10.x.x version
```

### 2. Clone the repository

```bash
git clone <repository-url>
cd blog
```

### 3. Restore dependencies

```bash
dotnet restore
```

### 4. Create the database

Create an empty PostgreSQL database that the application will manage (EF Core migrations create the tables for you — do not create tables manually):

```bash
psql -U postgres -c "CREATE DATABASE blog;"
```

The application also relies on the PostgreSQL `pg_trgm` extension for trigram-based user search. It is created automatically by the migrations, but this requires the connecting database user to have sufficient privileges (see [Troubleshooting](#troubleshooting) if this fails).

### 5. Configure user secrets

This project keeps all sensitive configuration (database connection string, JWT secret, SMTP credentials, Cloudinary keys) out of source control using **.NET User Secrets**.

➡️ Follow **[`blog.Api/user-secrets.md`](blog.Api/user-secrets.md)** for the complete, step-by-step guide to configuring every required secret before continuing.

Do not proceed to the next step until all secrets listed in that guide are set — the application will fail to start otherwise (see [Troubleshooting](#troubleshooting)).

### 6. Install the EF Core CLI tool

Required to apply database migrations:

```bash
dotnet tool install --global dotnet-ef
```

If it's already installed, make sure it's up to date:

```bash
dotnet tool update --global dotnet-ef
```

### 7. Apply database migrations

```bash
dotnet ef database update --project blog.Infrastructure --startup-project blog.Api
```

This creates all tables (`Users`, `Posts`, `Categories`, `RefreshTokens`, `EmailVerifications`) and their indexes.

### 8. Build the solution

```bash
dotnet build
```

### 9. Run the API

```bash
dotnet run --project blog.Api
```

By default the API listens on:

- `http://localhost:5005`
- `https://localhost:7007`

In the `Development` environment, a browser window opens automatically to `/scalar`, which serves interactive OpenAPI documentation where you can explore and call every endpoint directly.

### 10. Verify the setup

With the API running, register a test account:

```bash
curl -X POST http://localhost:5005/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","firstName":"Test","lastName":"User","password":"Password123!"}'
```

A `201 Created` response confirms the database connection, migrations, and password hashing are all working. Check your configured mailbox for the verification email to confirm SMTP is working end-to-end.

## Configuration Reference

| Section | Location | Description |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | User secrets | PostgreSQL connection string. See [`user-secrets.md`](blog.Api/user-secrets.md). |
| `JwtSettings` | User secrets | JWT signing key, issuer, audience, and token lifetimes. See [`user-secrets.md`](blog.Api/user-secrets.md). |
| `CloudinarySettings` | User secrets | Image upload/delete provider for post title images. See [`user-secrets.md`](blog.Api/user-secrets.md). |
| `EmailSettings` | User secrets | SMTP delivery for verification/notification emails. See [`user-secrets.md`](blog.Api/user-secrets.md). |
| `AccountLockoutSettings` | `appsettings.json` | `MaxFailedAttempts`, `LockoutDurationMinutes` — login lockout policy. Ships with working defaults. |
| `EmailVerificationSettings` | `appsettings.json` | Per-purpose OTP expiry (`{Purpose}ExpiryMinutes`) and attempt limits (`{Purpose}MaxAttempts`) for `Registration`, `LoginVerification`, `ChangeEmail`, `ResetPassword`, `ConfirmNewEmail`. Ships with working defaults. |
| `Cors:AllowedOrigins` | `appsettings.json` | Allowed origins for the API's CORS policy. Empty by default — add your frontend's origin here. |

All secret-bound settings above are validated at application startup (`ValidateOnStart`); the app will refuse to start if any required value is missing or invalid, with a clear error naming the offending setting.

## API Endpoints

All routes are relative to `/api/{controller}`.

### Auth (`/api/auth`) — anonymous, rate-limited (`auth` policy)

| Method | Route | Description |
|---|---|---|
| POST | `register` | Register a new account; sends an email verification OTP. |
| POST | `login` | Authenticate with email/password; returns tokens or a 2FA challenge. |
| POST | `logout` | Revoke a refresh token. *(Authorized)* |
| POST | `refresh` | Rotate a refresh token for a new access/refresh token pair. |
| POST | `confirm-email` | Confirm registration via OTP; returns tokens. |
| POST | `confirm-login` | Complete a 2FA login challenge via OTP; returns tokens. |
| POST | `forgot-password` | Request a password-reset OTP (always returns success to prevent enumeration). |
| POST | `reset-password` | Reset the password using the OTP. |

### Account (`/api/account`) — authenticated

| Method | Route | Description |
|---|---|---|
| GET | `me` | Get the current user's profile. |
| PUT | `me` | Update first/last name. |
| PUT | `me/password` | Change password (revokes all active refresh tokens). |
| PUT | `me/two-factor` | Enable/disable two-factor login verification. |
| POST | `me/change-email` | Request an email change (verifies current password + identity via OTP). |
| POST | `me/change-email/confirm` | Confirm identity OTP; sends a confirmation OTP to the new address. |
| POST | `me/change-email/confirm-new` | Confirm the new email address via OTP to finalize the change. |
| DELETE | `me` | Soft-delete the account (revokes all active refresh tokens). |

### Posts (`/api/posts`)

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `` | Authorized | Create a post (multipart/form-data, optional title image). |
| PUT | `{postId}` | Authorized | Update a post (author or elevated user only). |
| DELETE | `{postId}` | Authorized | Delete a post (author or elevated user only). |
| GET | `` | Anonymous | List published posts (paged, sortable). |
| GET | `search` | Anonymous | Full-text search over published posts. |
| GET | `tag/{tag}` | Anonymous | List published posts by tag. |
| GET | `category/{categorySlug}` | Anonymous | List published posts by category. |
| GET | `author/{authorId}` | Anonymous | List posts by author (unpublished posts included only if the caller can manage them). |
| GET | `{slug}` | Anonymous | Get a single post by slug; increments view count when published. |

### Categories (`/api/categories`)

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `` | Anonymous | List all active categories. |
| GET | `{slug}` | Anonymous | Get a category by slug. |
| POST | `` | Authorized (`Admin`+) | Create a category. |
| PUT | `{categoryId}` | Authorized (`Admin`+) | Rename a category. |
| DELETE | `{categoryId}` | Authorized (`Admin`+) | Soft-delete a category. |

### Users (`/api/users`)

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `search` | Anonymous | Search users by name/email (email only visible to elevated actors). |

### Admin (`/api/admin`) — authorized, level-gated per action

| Method | Route | Minimum level | Description |
|---|---|---|---|
| GET | `users` | `Admin` | Paged/filterable/sortable user listing. |
| PUT | `users/{targetUserId}/ban` | `Admin` | Ban/unban a user. |
| PUT | `users/{targetUserId}/level` | `Admin` | Change a user's level (cannot target/assign `Owner`). |
| GET | `posts` | `Owner` | Paged/filterable listing of all posts regardless of status. |
| GET | `posts/pending` | `Admin` | List posts awaiting approval. |
| PUT | `posts/{postId}/status` | `Admin` | Approve or reject a pending post. |

## Error Handling

Every `DomainException` is translated into a consistent JSON error body by `DomainExceptionResponseWriter`:

```json
{
  "statusCode": 404,
  "errorCode": "NOT_FOUND",
  "title": "Post not found",
  "details": { "resource": "Post", "id": "..." }
}
```

Unhandled exceptions are logged and returned as a generic `500 UNKNOWN_ERROR` (with the underlying message included only in `Development`).

## Security

- JWT bearer authentication with issuer/audience/lifetime validation and zero clock skew.
- Argon2 password hashing; SHA-256 hashing for refresh tokens and email verification codes at rest.
- Account lockout after repeated failed logins; optional email-based two-factor verification.
- Per-endpoint authorization via `[Authorize]`/`[AllowAnonymous]` plus a `MinimumLevel`-based `ActorAuthorizationBehavior` for privileged CQRS requests.
- Global and auth-specific sliding-window rate limiting.
- Security response headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`), HTTPS redirection/HSTS, and a configurable CORS policy.
- Server-side validation of uploaded post images (size limit, allowed content types, and magic-byte signature checks) plus request body size limits at the Kestrel level.

## Testing

Automated tests for all CQRS commands and queries live in `blog.Tests`.

```bash
dotnet test
```

## Troubleshooting

**`dotnet ef` command not found**
The EF Core CLI tool isn't installed. Run `dotnet tool install --global dotnet-ef`, then restart your terminal so the tool is on your `PATH`.

**Startup fails with a validation error about `JwtSettings`, `CloudinarySettings`, `EmailSettings`, `AccountLockoutSettings`, or `EmailVerificationSettings`**
One or more required settings are missing or invalid. These are validated eagerly at startup. Re-check the values you set with `dotnet user-secrets list` (run from `blog.Api`) against [`user-secrets.md`](blog.Api/user-secrets.md).

**`dotnet ef database update` fails with a connection error**
- Confirm PostgreSQL is running and reachable (`psql -U postgres -h localhost`).
- Confirm the database from step 4 exists.
- Confirm `ConnectionStrings:DefaultConnection` is set correctly in user secrets (host, port, database name, username, password all match your local PostgreSQL setup).

**Migration fails while creating the `pg_trgm` extension / GIN trigram indexes**
Creating a PostgreSQL extension typically requires elevated privileges. Either run the migration with a superuser role, or have a database administrator run `CREATE EXTENSION IF NOT EXISTS pg_trgm;` once on the target database before applying migrations.

**`401 Unauthorized` on endpoints marked `[Authorize]`**
- Make sure you're sending `Authorization: Bearer <accessToken>` with a token obtained from `/api/auth/login` (or the confirm-email/confirm-login/refresh endpoints).
- Access tokens are short-lived (`JwtSettings:AccessTokenExpiryMinutes`); use `/api/auth/refresh` to get a new one once it expires.

**`429 Too Many Requests` on auth endpoints during local testing**
Auth endpoints (`register`, `login`, `refresh`, `confirm-email`, `confirm-login`, `forgot-password`, `reset-password`) are rate-limited more strictly than the rest of the API. Wait for the sliding window to reset, or lower request frequency while testing.

**Emails are never received (registration/login/reset OTP)**
- Verify `EmailSettings` credentials are correct and that `SmtpPort`/`SmtpHost` match your provider.
- For Gmail, you must use an **App Password**, not your regular account password, and two-factor authentication must be enabled on the Google account first.
- Check spam/junk folders — the templated emails are sent from `EmailSettings:FromAddress`.

**Post image upload fails with an `UNAVAILABLE` or `UNSUPPORTED_MEDIA_TYPE` error**
- `UNAVAILABLE` typically means Cloudinary credentials (`CloudinarySettings`) are missing or incorrect.
- `UNSUPPORTED_MEDIA_TYPE` / `PAYLOAD_TOO_LARGE` mean the uploaded file isn't one of the allowed image types (JPEG, PNG, WEBP, GIF) or exceeds the 5 MB limit.

**Browser requests are blocked by CORS**
`Cors:AllowedOrigins` is empty by default, so no cross-origin browser requests are allowed. Add your frontend's origin (e.g. `http://localhost:3000`) to `Cors:AllowedOrigins` in `appsettings.json` (or an environment-specific override).

**Port `5005` or `7007` is already in use**
Another process is bound to the port. Either stop it, or change `applicationUrl` in `blog.Api/Properties/launchSettings.json`.

**HTTPS certificate warnings when calling `https://localhost:7007`**
Trust the local ASP.NET Core development certificate:

```bash
dotnet dev-certs https --trust
```

**Changes to entities aren't reflected in the database**
A new migration is required after changing an entity or its EF Core configuration:

```bash
dotnet ef migrations add <MigrationName> --project blog.Infrastructure --startup-project blog.Api
dotnet ef database update --project blog.Infrastructure --startup-project blog.Api
```