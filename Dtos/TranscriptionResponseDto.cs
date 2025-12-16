namespace Wihngo.Dtos
{
    /// <summary>
    /// Response DTO for audio transcription via OpenAI Whisper API
    /// </summary>
    public class TranscriptionResponseDto
    {
        /// <summary>
        /// Transcribed text from the audio file
        /// </summary>
        public string TranscribedText { get; set; } = string.Empty;

        /// <summary>
        /// Unique ID for this transcription request
        /// </summary>
        public string TranscriptionId { get; set; } = string.Empty;

        /// <summary>
        /// Detected language of the audio (ISO 639-1 code, e.g., "en", "es", "fr")
        /// Returned by Whisper API when available
        /// </summary>
        public string? Language { get; set; }
    }
}
