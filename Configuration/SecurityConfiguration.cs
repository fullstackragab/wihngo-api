namespace Wihngo.Configuration
{
    public class SecurityConfiguration
    {
        public PasswordPolicySettings PasswordPolicy { get; set; } = new();
        public AccountLockoutSettings AccountLockout { get; set; } = new();
        public TokenSettings TokenSettings { get; set; } = new();
        public RateLimitSettings RateLimit { get; set; } = new();
    }

    public class PasswordPolicySettings
    {
        public int MinimumLength { get; set; } = 8;
        public int MaximumLength { get; set; } = 128;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecialCharacter { get; set; } = true;
        public bool CheckCommonPasswords { get; set; } = true;
        public bool CheckSequentialCharacters { get; set; } = true;
        public int BCryptWorkFactor { get; set; } = 12;
    }

    public class AccountLockoutSettings
    {
        public int MaxFailedAttempts { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 30;
        public bool EnableLockout { get; set; } = true;
    }

    public class TokenSettings
    {
        public int AccessTokenExpiryHours { get; set; } = 24;
        public int RefreshTokenExpiryDays { get; set; } = 30;
        public int EmailConfirmationTokenExpiryHours { get; set; } = 24;
        public int PasswordResetTokenExpiryHours { get; set; } = 1;
    }

    public class RateLimitSettings
    {
        public int MaxLoginAttemptsPerWindow { get; set; } = 5;
        public int LoginWindowMinutes { get; set; } = 15;
        public int MaxApiRequestsPerWindow { get; set; } = 100;
        public int ApiWindowMinutes { get; set; } = 1;
        public bool EnableRateLimiting { get; set; } = true;
    }
}
