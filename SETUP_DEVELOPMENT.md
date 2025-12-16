# Development Environment Setup

## Initial Setup

### 1. Configuration File Setup

Copy the example development configuration file:

```bash
cp appsettings.Development.Example.json appsettings.Development.json
```

### 2. Configure Secrets

**IMPORTANT**: This project uses secure secret storage, NOT configuration files. Choose one method:

#### Method 1: .NET User Secrets (Recommended for .NET Development)

User Secrets stores credentials outside your project directory, making it impossible to accidentally commit them.

**Initialize User Secrets:**
```bash
dotnet user-secrets init
```

**Set Required Secrets:**
```bash
# AWS S3 (Required for media uploads)
dotnet user-secrets set "AWS:AccessKeyId" "AKIAXXXXXXXXXXXXXXXX"
dotnet user-secrets set "AWS:SecretAccessKey" "your-secret-key"
dotnet user-secrets set "AWS:BucketName" "wihngo-media-dev"
dotnet user-secrets set "AWS:Region" "us-east-1"

# OpenAI (Optional - for AI story generation)
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-api-key-here"
```

**View Your Secrets:**
```bash
dotnet user-secrets list
```

**Where Secrets Are Stored:**
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- Linux/Mac: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

This file is completely outside your project and can never be committed to git!

#### Method 2: Environment Variables (Recommended for Production)

**AWS S3 (Required for media uploads):**
```bash
# Windows (PowerShell)
$env:AWS_ACCESS_KEY_ID = "AKIAXXXXXXXXXXXXXXXX"
$env:AWS_SECRET_ACCESS_KEY = "your-secret-key-here"
$env:AWS_BUCKET_NAME = "wihngo-media-dev"
$env:AWS_REGION = "us-east-1"

# Linux/Mac
export AWS_ACCESS_KEY_ID="AKIAXXXXXXXXXXXXXXXX"
export AWS_SECRET_ACCESS_KEY="your-secret-key-here"
export AWS_BUCKET_NAME="wihngo-media-dev"
export AWS_REGION="us-east-1"
```

**OpenAI (Optional - for AI story generation):**
```bash
# Windows (PowerShell)
$env:OpenAI__ApiKey = "sk-your-api-key-here"

# Linux/Mac
export OpenAI__ApiKey="sk-your-api-key-here"
```
Note: ASP.NET Core uses double underscore (`__`) to represent nested configuration (`:`).

#### How to Get Credentials:

**AWS S3:**
- Go to AWS IAM Console: https://console.aws.amazon.com/iam/
- Create a new IAM user with these permissions:
  - `s3:PutObject`
  - `s3:GetObject`
  - `s3:DeleteObject`
- Generate Access Key ID and Secret Access Key
- Create an S3 bucket or use existing one

**OpenAI:**
- Get API key at: https://platform.openai.com/api-keys
- Recommended: Use service account for team projects

#### Alternative: Configuration Files (Fallback)

If you prefer to use configuration files instead of environment variables, you can add secrets to `appsettings.Development.json`:

```json
"OpenAI": {
  "ApiKey": "sk-your-openai-api-key-here"
}
```

**Note**: The app checks secrets in this priority order:
1. **User Secrets** (local development only) - e.g., `OpenAI:ApiKey`
2. **Environment Variables** - e.g., `AWS_ACCESS_KEY_ID` or `OpenAI__ApiKey`
3. **Configuration Files** - e.g., `appsettings.Development.json`

Higher priority sources override lower ones.

### 3. Optional Configurations

**Email (SMTP)** - Edit `appsettings.Development.json`:
```json
"Smtp": {
  "Username": "your-mailtrap-username",
  "Password": "your-mailtrap-password"
}
```
- For development, use Mailtrap: https://mailtrap.io

**Payment Providers** - Edit `appsettings.Development.json`:
- **PayPal**: Add `ClientId`, `ClientSecret`, `WebhookId`
- **Blockchain**: Add wallet addresses for Solana/Base
- **TronGrid**: Add API key for Tron blockchain features

### 4. Security Notes

⚠️ **IMPORTANT**: This project follows security best practices:

**User Secrets (Recommended for .NET Development)**:
- ✅ Secrets stored outside project directory in user profile
- ✅ Impossible to accidentally commit to git
- ✅ .NET-specific, works seamlessly with ASP.NET Core
- ✅ Perfect for local development
- ✅ Managed via `dotnet user-secrets` commands

**Environment Variables (Recommended for Production)**:
- ✅ Used in production environments (Docker, Kubernetes, Azure, AWS)
- ✅ Production uses secrets management (Azure Key Vault, AWS Secrets Manager, etc.)
- ✅ Never commit credentials to git
- ✅ Rotate keys immediately if accidentally exposed

**Configuration Files (Fallback Only)**:
- `appsettings.Development.json` is in `.gitignore` and will NOT be committed to git
- Only use as last resort if User Secrets or environment variables aren't available
- Never share your local configuration files

#### Secrets Management Methods:
- ✅ **User Secrets**: AWS, OpenAI keys (local development)
- ✅ **Environment Variables**: AWS, OpenAI keys (production/CI/CD)
- ✅ **Configuration Files**: SMTP, JWT, payment providers (protected by .gitignore)

#### Credentials in Configuration Files (Protected by .gitignore):
- ✅ SMTP credentials
- ✅ JWT secret key
- ✅ Payment provider credentials (PayPal, blockchain wallets)
- ✅ Database passwords

### 5. File Structure

```
appsettings.json                      ← Base config (committed)
appsettings.Development.json          ← Your secrets (NOT committed) ✓
appsettings.Development.Example.json  ← Template (committed)
appsettings.CryptoLogging.json        ← Crypto logging config (committed)
```

## Quick Reference

### OpenAI API Costs (for AI Story Generation)

With `gpt-4o-mini` (recommended):
- 10 stories ≈ $0.003 (less than half a cent)
- 100 stories ≈ $0.03 (3 cents)
- 1,000 stories ≈ $0.30 (30 cents)

With `gpt-4`:
- 10 stories ≈ $0.32
- 100x more expensive than gpt-4o-mini

**Set spending limits** on OpenAI dashboard to avoid surprises.

## Troubleshooting

**"AWS credentials are not configured"**
- **Option 1 (Recommended)**: Use User Secrets:
  ```bash
  dotnet user-secrets set "AWS:AccessKeyId" "AKIAXXXXXXXXXXXXXXXX"
  dotnet user-secrets set "AWS:SecretAccessKey" "your-secret-key"
  ```
- **Option 2**: Set environment variables: `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`
- **Option 3**: Add to `appsettings.Development.json` under `AWS` section
- Ensure the S3 bucket exists and is accessible
- Check IAM permissions for the user (s3:PutObject, s3:GetObject, s3:DeleteObject)
- Restart the application

**"OpenAI API key is not configured"**
- **Option 1 (Recommended)**: Use User Secrets:
  ```bash
  dotnet user-secrets set "OpenAI:ApiKey" "sk-your-key-here"
  ```
- **Option 2**: Set environment variable: `OpenAI__ApiKey` (note the double underscore)
- **Option 3**: Add to `appsettings.Development.json` under `OpenAI:ApiKey`
- Restart the application

**Check your User Secrets:**
```bash
dotnet user-secrets list
```

**"SMTP connection failed"**
- Update SMTP credentials in `appsettings.Development.json`
- Or disable email features for development

**Media upload fails (403/404 errors)**
- Verify AWS credentials are correct
- Check S3 bucket name and region match your AWS setup
- Ensure IAM user has s3:PutObject, s3:GetObject, s3:DeleteObject permissions
- Verify bucket CORS policy allows your frontend domain

**Build errors about missing configuration**
- Ensure `appsettings.Development.json` exists
- Copy from `appsettings.Development.Example.json` if missing
