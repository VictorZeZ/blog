# User Secrets Configuration Guide

This project uses **.NET User Secrets** to store sensitive configuration values during local development.

> **Important**
>
> - Never commit real secrets to Git.
> - Never store production credentials in `appsettings.json` or `appsettings.Development.json`.
> - Each developer must configure their own User Secrets before running the project.

## Table of Contents

- [What are User Secrets, and why use them?](#what-are-user-secrets-and-why-use-them)
- [How User Secrets work](#how-user-secrets-work)
- [Prerequisites](#prerequisites)
- [Initialize User Secrets](#initialize-user-secrets)
- [Set Required Secrets](#set-required-secrets)
  - [JWT Settings](#jwt-settings)
  - [Database Connection](#database-connection)
  - [Email Settings](#email-settings)
  - [Cloudinary Settings](#cloudinary-settings)
- [Using the .NET CLI](#using-the-net-cli)
- [Verify Configuration](#verify-configuration)
- [Removing Secrets](#removing-secrets)
- [Required Secret Keys](#required-secret-keys)
- [Settings That Do Not Need to Be Secrets](#settings-that-do-not-need-to-be-secrets)
- [Production Considerations](#production-considerations)
- [Troubleshooting](#troubleshooting)

---

## What are User Secrets, and why use them?

[.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) is a development-time feature that stores configuration values **outside of your project folder**, in a JSON file on your machine, keyed by a `UserSecretsId` defined in `blog.Api.csproj`. This means:

- Secrets are never checked into source control, even by accident.
- Each developer can use their own database, SMTP account, and Cloudinary account without touching shared configuration files.
- The values are still read through the standard ASP.NET Core configuration system, so no code changes are needed — `IOptions<T>` bindings in `blog.Infrastructure/DependencyInjection.cs` (`JwtSettings`, `CloudinarySettings`, `EmailSettings`, `AccountLockoutSettings`, `EmailVerificationSettings`) work exactly the same whether the values come from `appsettings.json`, User Secrets, or environment variables.

User Secrets are **not encrypted** — they are only kept out of the repository. They are a development convenience, not a production secret store (see [Production Considerations](#production-considerations)).

## How User Secrets work

Secrets are stored in a `secrets.json` file outside the project directory:

- Windows: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- macOS/Linux: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

The `<UserSecretsId>` for this project is already defined in `blog.Api/blog.Api.csproj` (`<UserSecretsId>34838c63-a85e-4587-a491-3a2a54d93e66</UserSecretsId>`), so you do not need to generate one — you only need to populate the values.

Configuration keys use `:` as a section separator (e.g. `JwtSettings:SecretKey`), matching the strongly-typed settings classes bound via `IOptions<T>` in the Domain/Infrastructure layers.

## Prerequisites

- .NET 10 SDK installed (`dotnet --version` should report a 10.x.x version).
- The repository cloned locally, with a terminal open in the `blog.Api` project folder (all commands below assume you're inside `blog.Api`).

```bash
cd blog.Api
```

## Initialize User Secrets

User Secrets are usually already wired up via `UserSecretsId` in the `.csproj`, but running `init` is a safe, idempotent way to confirm this and create the local secrets store if it doesn't exist yet:

```bash
dotnet user-secrets init
```

If it prints that a `UserSecretsId` already exists, that's expected — it's already configured in `blog.Api.csproj`.

## Set Required Secrets

Every secret below is required. The application validates all of these settings at startup (`ValidateOnStart`) and will refuse to run if any are missing, empty, or fail validation (e.g. an out-of-range port, an invalid email format).

### JWT Settings

Used by `JwtService` to sign access tokens and by `AuthExtensions` to configure JWT bearer validation.

```text
JwtSettings:SecretKey = YOUR_SECURE_RANDOM_SECRET_KEY
JwtSettings:Issuer = YOUR_API_ISSUER
JwtSettings:Audience = YOUR_CLIENT_AUDIENCE
JwtSettings:AccessTokenExpiryMinutes = 15
JwtSettings:RefreshTokenExpiryDays = 30
```

| Key | Description |
|---|---|
| `SecretKey` | Symmetric key (HMAC-SHA256) used to sign and validate access tokens. **Must be at least 32 characters**; 64+ random characters is recommended. Generate one with `openssl rand -base64 48` or any password generator. |
| `Issuer` | The `iss` claim value your API issues tokens as. Any stable string identifying your API (e.g. `myapi`). |
| `Audience` | The `aud` claim value your API expects. Any stable string identifying your client (e.g. `myclient`). |
| `AccessTokenExpiryMinutes` | Lifetime of short-lived access tokens, in minutes. |
| `RefreshTokenExpiryDays` | Lifetime of refresh tokens, in days, before they must be re-obtained via login. |

#### Example

```text
JwtSettings:SecretKey = ReplaceWithAtLeast32CharactersLongRandomSecret
JwtSettings:Issuer = myapi
JwtSettings:Audience = myclient
JwtSettings:AccessTokenExpiryMinutes = 15
JwtSettings:RefreshTokenExpiryDays = 30
```

### Database Connection

Used by `AppDbContext` to connect to your local PostgreSQL instance.

```text
ConnectionStrings:DefaultConnection = Host=YOUR_HOST;Database=YOUR_DATABASE;Username=YOUR_USERNAME;Password=YOUR_PASSWORD
```

#### Example

```text
ConnectionStrings:DefaultConnection = Host=localhost;Database=blog;Username=postgres;Password=your_password
```

> The database itself (e.g. `blog`) must already exist before running migrations — User Secrets only configure how to *connect* to it. See the main [README's Setup section](../README.md#setup) for creating the database and applying migrations.

### Email Settings

Used by `SmtpEmailSender` to deliver OTP emails for registration, login verification, password reset, and email changes.

```text
EmailSettings:SmtpHost = YOUR_SMTP_HOST
EmailSettings:SmtpPort = YOUR_SMTP_PORT
EmailSettings:SmtpUsername = YOUR_EMAIL_ADDRESS
EmailSettings:SmtpPassword = YOUR_SMTP_PASSWORD_OR_APP_PASSWORD
EmailSettings:FromAddress = YOUR_EMAIL_ADDRESS
EmailSettings:FromName = YOUR_APPLICATION_NAME
```

| Key | Description |
|---|---|
| `SmtpHost` | Hostname of your SMTP provider (e.g. `smtp.gmail.com`). |
| `SmtpPort` | SMTP port, typically `587` for STARTTLS. |
| `SmtpUsername` | The account used to authenticate with the SMTP server. |
| `SmtpPassword` | The account password or app-specific password. |
| `FromAddress` | The sender address shown on outgoing emails. Usually the same as `SmtpUsername`. |
| `FromName` | The sender display name shown on outgoing emails (e.g. `Blog`). |

#### Gmail Example

```text
EmailSettings:SmtpHost = smtp.gmail.com
EmailSettings:SmtpPort = 587
EmailSettings:SmtpUsername = your-email@gmail.com
EmailSettings:SmtpPassword = YOUR_GOOGLE_APP_PASSWORD
EmailSettings:FromAddress = your-email@gmail.com
EmailSettings:FromName = Blog
```

> **Note**
>
> Gmail requires an **App Password** when two-factor authentication is enabled on the account.
> Your regular Google account password will not work and the connection will be rejected.
> Generate one at https://myaccount.google.com/apppasswords (requires 2-Step Verification to be enabled first).
>
> Any other SMTP provider (Outlook, Mailtrap, SendGrid SMTP relay, Amazon SES, etc.) works the same way — just substitute the host, port, and credentials it gives you.

### Cloudinary Settings

Used by `CloudinaryService` to upload and delete post title images.

```text
CloudinarySettings:CloudName = YOUR_CLOUD_NAME
CloudinarySettings:ApiKey = YOUR_API_KEY
CloudinarySettings:ApiSecret = YOUR_API_SECRET
```

| Key | Description |
|---|---|
| `CloudName` | Your Cloudinary cloud name, shown on the Cloudinary Dashboard. |
| `ApiKey` | Your Cloudinary API key. |
| `ApiSecret` | Your Cloudinary API secret. Treat this like a password. |

> **Note**
>
> Create a free account at https://cloudinary.com/users/register/free, then copy all three values from the **Dashboard** page. All three are required — image uploads will fail with an `UNAVAILABLE` domain error if any is missing or incorrect.

## Using the .NET CLI

You can also configure each secret individually using `dotnet user-secrets set`, run from inside `blog.Api`:

### JWT

```bash
dotnet user-secrets set "JwtSettings:SecretKey" "YOUR_SECURE_RANDOM_SECRET_KEY"
dotnet user-secrets set "JwtSettings:Issuer" "YOUR_API_ISSUER"
dotnet user-secrets set "JwtSettings:Audience" "YOUR_CLIENT_AUDIENCE"
dotnet user-secrets set "JwtSettings:AccessTokenExpiryMinutes" "15"
dotnet user-secrets set "JwtSettings:RefreshTokenExpiryDays" "30"
```

### Database

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=blog;Username=postgres;Password=your_password"
```

### Email

```bash
dotnet user-secrets set "EmailSettings:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:SmtpPort" "587"
dotnet user-secrets set "EmailSettings:SmtpUsername" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:SmtpPassword" "YOUR_GOOGLE_APP_PASSWORD"
dotnet user-secrets set "EmailSettings:FromAddress" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:FromName" "Blog"
```

### Cloudinary

```bash
dotnet user-secrets set "CloudinarySettings:CloudName" "YOUR_CLOUD_NAME"
dotnet user-secrets set "CloudinarySettings:ApiKey" "YOUR_API_KEY"
dotnet user-secrets set "CloudinarySettings:ApiSecret" "YOUR_API_SECRET"
```

## Verify Configuration

To confirm all secrets are set correctly:

```bash
dotnet user-secrets list
```

This prints every key currently stored for this project (values included — the file lives outside the repo, but remember not to paste this output anywhere public).

## Removing Secrets

To remove a single secret:

```bash
dotnet user-secrets remove "JwtSettings:SecretKey"
```

To wipe all secrets for this project and start over:

```bash
dotnet user-secrets clear
```

## Required Secret Keys

| Key | Required | Description |
|------|----------|-------------|
| `JwtSettings:SecretKey` | ✅ | Secret key used to sign JWT access tokens. |
| `JwtSettings:Issuer` | ✅ | JWT issuer. |
| `JwtSettings:Audience` | ✅ | JWT audience. |
| `JwtSettings:AccessTokenExpiryMinutes` | ✅ | Access token lifetime in minutes. |
| `JwtSettings:RefreshTokenExpiryDays` | ✅ | Refresh token lifetime in days. |
| `ConnectionStrings:DefaultConnection` | ✅ | PostgreSQL connection string. |
| `EmailSettings:SmtpHost` | ✅ | SMTP server hostname. |
| `EmailSettings:SmtpPort` | ✅ | SMTP server port. |
| `EmailSettings:SmtpUsername` | ✅ | SMTP account username/email. |
| `EmailSettings:SmtpPassword` | ✅ | SMTP password or application password. |
| `EmailSettings:FromAddress` | ✅ | Sender email address. |
| `EmailSettings:FromName` | ✅ | Sender display name. |
| `CloudinarySettings:CloudName` | ✅ | Cloudinary cloud name. |
| `CloudinarySettings:ApiKey` | ✅ | Cloudinary API key. |
| `CloudinarySettings:ApiSecret` | ✅ | Cloudinary API secret. |

## Settings That Do Not Need to Be Secrets

The following settings ship with working defaults in `appsettings.json` / `appsettings.Development.json` and are **not** sensitive, so they are not stored as User Secrets. You can still override them per environment if needed:

| Section | Purpose |
|---|---|
| `AccountLockoutSettings` | `MaxFailedAttempts`, `LockoutDurationMinutes` — controls login lockout behavior. |
| `EmailVerificationSettings` | Expiry minutes and max attempts per OTP purpose (registration, login verification, change email, reset password, confirm new email). |
| `Cors:AllowedOrigins` | Origins allowed to call the API from a browser. Empty by default. |

## Production Considerations

User Secrets are a **local development tool only** — the `secrets.json` file is unencrypted and lives on your machine, and is never deployed with the application. For any non-local environment (staging, production), use a proper secret store instead, for example:

- Environment variables (mapped automatically by ASP.NET Core's configuration system using the same `Section__Key` naming, e.g. `JwtSettings__SecretKey`).
- A managed secret manager such as Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault.
- Your hosting platform's built-in secrets/config management (e.g. container orchestrator secrets).

Regardless of the mechanism, the configuration **keys** stay identical to the ones documented above — only where they're read from changes.

## Troubleshooting

**The app fails to start with a `ValidationException`/options validation error mentioning `JwtSettings`, `EmailSettings`, `CloudinarySettings`, `AccountLockoutSettings`, or `EmailVerificationSettings`**
Run `dotnet user-secrets list` from `blog.Api` and compare the output against the [Required Secret Keys](#required-secret-keys) table above. A missing key, an empty value, or an out-of-range number (e.g. a non-numeric `SmtpPort`) will all trigger this at startup, before any request is handled.

**`dotnet user-secrets` commands say no `UserSecretsId` was found**
Make sure you're running the command from inside the `blog.Api` folder (not the repository root or another project folder) — User Secrets are scoped per project via the `UserSecretsId` in that project's `.csproj`.

**Secrets seem to be "missing" even though I set them**
Double-check you're running `dotnet run` (or `dotnet ef ...`) with `blog.Api` as the target/startup project. User Secrets are tied to a specific project's `UserSecretsId`; setting a secret while inside a different project folder writes it to a different, unrelated secrets store.

**Emails aren't sending even though `EmailSettings` looks correct**
See the [main README's Troubleshooting section](../README.md#troubleshooting) for SMTP-specific guidance (e.g. Gmail App Passwords).