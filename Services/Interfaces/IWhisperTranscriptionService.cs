using Wihngo.Dtos;

namespace Wihngo.Services.Interfaces
{
    /// <summary>
    /// Service for audio transcription using OpenAI Whisper API
    /// </summary>
    public interface IWhisperTranscriptionService
    {
        /// <summary>
        /// Transcribe audio file from S3 to text
        /// </summary>
        /// <param name="audioS3Key">S3 key of the audio file</param>
        /// <param name="language">Optional language code hint (e.g., "en", "ar", "es") for better accuracy</param>
        /// <returns>Transcription result with text and metadata</returns>
        Task<TranscriptionResponseDto> TranscribeAudioAsync(string audioS3Key, string? language = null);
    }
}
