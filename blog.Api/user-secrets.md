# User Secrets Configuration Guide

This project uses **.NET User Secrets** to store sensitive configuration values during local development.

> **Important**
>
> - Never commit real secrets to Git.
> - Never store production credentials in `appsettings.json`.
> - Each developer must configure their own User Secrets before running the project.

---

# Initialize User Secrets

If the project does not already have User Secrets initialized, run:

```bash
dotnet user-secrets init
```

---

# Set Required Secrets

Configure the following secrets.

## JWT Settings

```text
JwtSettings:SecretKey = YOUR_SECURE_RANDOM_SECRET_KEY
JwtSettings:Issuer = YOUR_API_ISSUER
JwtSettings:Audience = YOUR_CLIENT_AUDIENCE
JwtSettings:AccessTokenExpiryMinutes = 15
JwtSettings:RefreshTokenExpiryDays = 30
```

### Example

```text
JwtSettings:SecretKey = ReplaceWithAtLeast32CharactersLongRandomSecret
JwtSettings:Issuer = myapi
JwtSettings:Audience = myclient
JwtSettings:AccessTokenExpiryMinutes = 15
JwtSettings:RefreshTokenExpiryDays = 30
```

---

## Email Settings

Configure an SMTP provider (Gmail, Outlook, Mailtrap, etc.).

```text
EmailSettings:SmtpHost = YOUR_SMTP_HOST
EmailSettings:SmtpPort = YOUR_SMTP_PORT
EmailSettings:SmtpUsername = YOUR_EMAIL_ADDRESS
EmailSettings:SmtpPassword = YOUR_SMTP_PASSWORD_OR_APP_PASSWORD
EmailSettings:FromAddress = YOUR_EMAIL_ADDRESS
EmailSettings:FromName = YOUR_APPLICATION_NAME
```

### Gmail Example

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
> Gmail requires an **App Password** when two-factor authentication is enabled.
> Your regular Google account password will not work.

---

## Database Connection

Configure your PostgreSQL connection string.

```text
ConnectionStrings:DefaultConnection = Host=YOUR_HOST;Database=YOUR_DATABASE;Username=YOUR_USERNAME;Password=YOUR_PASSWORD
```

### Example

```text
ConnectionStrings:DefaultConnection = Host=localhost;Database=blog;Username=postgres;Password=your_password
```

---

## Cloudinary Settings

Create a Cloudinary account and copy your credentials from the Cloudinary Dashboard.

```text
CloudinarySettings:CloudName = YOUR_CLOUD_NAME
CloudinarySettings:ApiKey = YOUR_API_KEY
CloudinarySettings:ApiSecret = YOUR_API_SECRET
```

> **Note**
>
> All three values are required for image upload functionality.

---

# Using the .NET CLI

You can configure secrets individually using the following commands.

## JWT

```bash
dotnet user-secrets set "JwtSettings:SecretKey" "YOUR_SECURE_RANDOM_SECRET_KEY"

dotnet user-secrets set "JwtSettings:Issuer" "YOUR_API_ISSUER"

dotnet user-secrets set "JwtSettings:Audience" "YOUR_CLIENT_AUDIENCE"

dotnet user-secrets set "JwtSettings:AccessTokenExpiryMinutes" "15"

dotnet user-secrets set "JwtSettings:RefreshTokenExpiryDays" "30"
```

---

## Email

```bash
dotnet user-secrets set "EmailSettings:SmtpHost" "smtp.gmail.com"

dotnet user-secrets set "EmailSettings:SmtpPort" "587"

dotnet user-secrets set "EmailSettings:SmtpUsername" "your-email@gmail.com"

dotnet user-secrets set "EmailSettings:SmtpPassword" "YOUR_GOOGLE_APP_PASSWORD"

dotnet user-secrets set "EmailSettings:FromAddress" "your-email@gmail.com"

dotnet user-secrets set "EmailSettings:FromName" "Blog"
```

---

## Database

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=blog;Username=postgres;Password=your_password"
```

---

## Cloudinary

```bash
dotnet user-secrets set "CloudinarySettings:CloudName" "YOUR_CLOUD_NAME"

dotnet user-secrets set "CloudinarySettings:ApiKey" "YOUR_API_KEY"

dotnet user-secrets set "CloudinarySettings:ApiSecret" "YOUR_API_SECRET"
```

---

# Verify Configuration

To verify that your secrets have been configured correctly:

```bash
dotnet user-secrets list
```

---

# Required Secret Keys

| Key | Required | Description |
|------|----------|-------------|
| `JwtSettings:SecretKey` | ✅ | Secret key used to sign JWT access tokens. |
| `JwtSettings:Issuer` | ✅ | JWT issuer. |
| `JwtSettings:Audience` | ✅ | JWT audience. |
| `JwtSettings:AccessTokenExpiryMinutes` | ✅ | Access token lifetime in minutes. |
| `JwtSettings:RefreshTokenExpiryDays` | ✅ | Refresh token lifetime in days. |
| `EmailSettings:SmtpHost` | ✅ | SMTP server hostname. |
| `EmailSettings:SmtpPort` | ✅ | SMTP server port. |
| `EmailSettings:SmtpUsername` | ✅ | SMTP account username/email. |
| `EmailSettings:SmtpPassword` | ✅ | SMTP password or application password. |
| `EmailSettings:FromAddress` | ✅ | Sender email address. |
| `EmailSettings:FromName` | ✅ | Sender display name. |
| `ConnectionStrings:DefaultConnection` | ✅ | PostgreSQL connection string. |
| `CloudinarySettings:CloudName` | ✅ | Cloudinary cloud name. |
| `CloudinarySettings:ApiKey` | ✅ | Cloudinary API key. |
| `CloudinarySettings:ApiSecret` | ✅ | Cloudinary API secret. |

---

# Security Notes

- Never commit `secrets.json` to source control.
- Never share API keys, passwords, or JWT secret keys.
- Use different credentials for Development, Staging, and Production environments.
- Rotate secrets immediately if they are ever exposed.
- Generate a cryptographically secure JWT secret with a minimum length of **32 characters** (64+ characters recommended).