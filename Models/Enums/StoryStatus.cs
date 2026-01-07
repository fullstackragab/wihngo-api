namespace Wihngo.Models.Enums
{
    /// <summary>
    /// Moderation status for stories
    /// </summary>
    public enum StoryStatus
    {
        /// <summary>
        /// Story is awaiting admin review (default on creation)
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Story has been approved and is visible publicly
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Story has been rejected by admin
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Admin requested changes from the author
        /// </summary>
        ChangesRequested = 3
    }
}
