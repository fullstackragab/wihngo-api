namespace Wihngo.Services
{
    using System.Text.RegularExpressions;

    public interface IPasswordValidationService
    {
        (bool isValid, List<string> errors) ValidatePassword(string password);
        bool IsPasswordCompromised(string password);
    }

    public class PasswordValidationService : IPasswordValidationService
    {
        private readonly ILogger<PasswordValidationService> _logger;
        private static readonly HashSet<string> CommonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "password123", "12345678", "qwerty", "abc123",
            "monkey", "1234567", "letmein", "trustno1", "dragon", "baseball",
            "iloveyou", "master", "sunshine", "ashley", "bailey", "passw0rd",
            "shadow", "123123", "654321", "superman", "qazwsx", "michael",
            "football", "welcome", "jesus", "ninja", "mustang", "password1"
        };

        public PasswordValidationService(ILogger<PasswordValidationService> logger)
        {
            _logger = logger;
        }

        public (bool isValid, List<string> errors) ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required");
                return (false, errors);
            }

            // Minimum length
            if (password.Length < 8)
            {
                errors.Add("Password must be at least 8 characters long");
            }

            // Maximum length
            if (password.Length > 128)
            {
                errors.Add("Password must not exceed 128 characters");
            }

            // Require at least one uppercase letter
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errors.Add("Password must contain at least one uppercase letter");
            }

            // Require at least one lowercase letter
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errors.Add("Password must contain at least one lowercase letter");
            }

            // Require at least one digit
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                errors.Add("Password must contain at least one digit");
            }

            // Require at least one special character
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]"))
            {
                errors.Add("Password must contain at least one special character (!@#$%^&* etc.)");
            }

            // Check for common passwords
            if (IsPasswordCompromised(password))
            {
                errors.Add("This password is too common. Please choose a more secure password");
            }

            // Check for sequential characters
            if (HasSequentialCharacters(password))
            {
                errors.Add("Password should not contain sequential characters (e.g., 123, abc)");
            }

            return (errors.Count == 0, errors);
        }

        public bool IsPasswordCompromised(string password)
        {
            return CommonPasswords.Contains(password);
        }

        private bool HasSequentialCharacters(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                char c1 = password[i];
                char c2 = password[i + 1];
                char c3 = password[i + 2];

                // Check for sequential digits (123, 234, etc.)
                if (char.IsDigit(c1) && char.IsDigit(c2) && char.IsDigit(c3))
                {
                    if (c2 == c1 + 1 && c3 == c2 + 1)
                        return true;
                    if (c2 == c1 - 1 && c3 == c2 - 1)
                        return true;
                }

                // Check for sequential letters (abc, xyz, etc.)
                if (char.IsLetter(c1) && char.IsLetter(c2) && char.IsLetter(c3))
                {
                    char lower1 = char.ToLower(c1);
                    char lower2 = char.ToLower(c2);
                    char lower3 = char.ToLower(c3);

                    if (lower2 == lower1 + 1 && lower3 == lower2 + 1)
                        return true;
                    if (lower2 == lower1 - 1 && lower3 == lower2 - 1)
                        return true;
                }
            }

            return false;
        }
    }
}
