namespace Wihngo.Models.Enums
{
    /// <summary>
    /// Bird activity status based on last activity timestamp.
    /// Used to determine support button visibility.
    /// </summary>
    public enum BirdActivityStatus
    {
        /// <summary>
        /// Bird is active (activity within last 30 days).
        /// Support button is shown.
        /// </summary>
        Active,

        /// <summary>
        /// Bird is quiet (30-90 days since last activity).
        /// Support button is shown with "Last seen" indicator.
        /// </summary>
        Quiet,

        /// <summary>
        /// Bird is inactive (90+ days since last activity).
        /// Support button is hidden.
        /// </summary>
        Inactive,

        /// <summary>
        /// Bird has passed away (memorial status).
        /// Support button is hidden, tribute messages allowed.
        /// </summary>
        Memorial
    }
}
