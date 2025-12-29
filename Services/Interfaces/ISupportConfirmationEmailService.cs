namespace Wihngo.Services.Interfaces
{
    /// <summary>
    /// Service for sending support confirmation emails after successful transactions.
    /// IMPORTANT: Only call this service after on-chain transaction success with confirmation hash.
    /// Never call on intent creation, retry, or failure.
    /// </summary>
    public interface ISupportConfirmationEmailService
    {
        /// <summary>
        /// Sends a support confirmation email to the supporter.
        /// </summary>
        /// <param name="dto">Support confirmation details</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendSupportConfirmationAsync(SupportConfirmationDto dto);
    }

    /// <summary>
    /// Data required for sending a support confirmation email.
    /// All amounts are in USDC.
    /// </summary>
    public class SupportConfirmationDto
    {
        /// <summary>
        /// Supporter's email address
        /// </summary>
        public string SupporterEmail { get; set; } = string.Empty;

        /// <summary>
        /// Supporter's display name
        /// </summary>
        public string SupporterName { get; set; } = string.Empty;

        /// <summary>
        /// Name of the bird being supported
        /// </summary>
        public string BirdName { get; set; } = string.Empty;

        /// <summary>
        /// S3 key or URL for bird's profile image
        /// </summary>
        public string? BirdImageUrl { get; set; }

        /// <summary>
        /// Amount going directly to the bird (USDC)
        /// </summary>
        public decimal BirdAmount { get; set; }

        /// <summary>
        /// Optional amount for Wihngo platform support (USDC)
        /// </summary>
        public decimal? WihngoAmount { get; set; }

        /// <summary>
        /// Total amount of support (USDC)
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// UTC timestamp of the transaction
        /// </summary>
        public DateTime TransactionDateTime { get; set; }

        /// <summary>
        /// Solana transaction signature/hash
        /// </summary>
        public string TransactionHash { get; set; } = string.Empty;

        /// <summary>
        /// Preferred language for the email (en, ar). Defaults to "en".
        /// </summary>
        public string Language { get; set; } = "en";
    }
}
