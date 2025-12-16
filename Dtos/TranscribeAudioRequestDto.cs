using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos
{
    /// <summary>
    /// Request DTO for transcribing audio to text
    /// </summary>
    public class TranscribeAudioRequestDto
    {
        /// <summary>
        /// S3 key of the audio file to transcribe
        /// Example: users/stories/{userId}/{storyId}/{uuid}.m4a
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string AudioS3Key { get; set; } = string.Empty;

        /// <summary>
        /// Language code hint for transcription (e.g., "en", "ar", "es")
        /// This improves transcription accuracy. If not provided, auto-detection is used.
        /// </summary>
        [MaxLength(10)]
        public string? Language { get; set; }
    }
}
