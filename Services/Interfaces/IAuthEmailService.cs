namespace Wihngo.Services.Interfaces
{
    using System.Threading.Tasks;

    /// <summary>
    /// Service for sending authentication-related emails
    /// </summary>
    public interface IAuthEmailService
    {
        /// <summary>
        /// Send email confirmation email with token
        /// </summary>
        Task SendEmailConfirmationAsync(string email, string name, string confirmationToken);

        /// <summary>
        /// Send password reset email with token
        /// </summary>
        Task SendPasswordResetAsync(string email, string name, string resetToken);

        /// <summary>
        /// Send welcome email after successful registration
        /// </summary>
        Task SendWelcomeEmailAsync(string email, string name);

        /// <summary>
        /// Send security alert email (e.g., password changed, account locked)
        /// </summary>
        Task SendSecurityAlertAsync(string email, string name, string alertType, string details);

        /// <summary>
        /// Send account unlocked notification
        /// </summary>
        Task SendAccountUnlockedAsync(string email, string name);
    }
}
