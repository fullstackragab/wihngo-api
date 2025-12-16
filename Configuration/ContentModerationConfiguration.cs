namespace Wihngo.Configuration
{
    public class ContentModerationConfiguration
    {
        public bool EnableTextModeration { get; set; } = true;
        public bool EnableMediaModeration { get; set; } = true;

        // AWS Rekognition thresholds (0-100)
        public int NsfwThreshold { get; set; } = 80;
        public int ViolenceThreshold { get; set; } = 80;
        public int HateThreshold { get; set; } = 80;
        public int MinConfidencePercent { get; set; } = 75;

        // OpenAI Moderation API blocked categories
        public List<string> BlockedCategories { get; set; } = new()
        {
            "sexual/minors",
            "hate",
            "violence/graphic",
            "self-harm/intent",
            "self-harm/instructions"
        };
    }
}
