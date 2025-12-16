using System.Text.Json;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    /// <summary>
    /// Service for audio transcription using OpenAI Whisper API
    /// </summary>
    public class WhisperTranscriptionService : IWhisperTranscriptionService
    {
        private readonly IConfiguration _configuration;
        private readonly IS3Service _s3Service;
        private readonly ILogger<WhisperTranscriptionService> _logger;
        private readonly HttpClient _httpClient;
        private const string WhisperApiUrl = "https://api.openai.com/v1/audio/transcriptions";

        public WhisperTranscriptionService(
            IConfiguration configuration,
            IS3Service s3Service,
            ILogger<WhisperTranscriptionService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _s3Service = s3Service;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<TranscriptionResponseDto> TranscribeAudioAsync(string audioS3Key, string? language = null)
        {
            var transcriptionId = Guid.NewGuid().ToString();

            try
            {
                _logger.LogInformation("Starting audio transcription. TranscriptionId={TranscriptionId}, S3Key={S3Key}, Language={Language}",
                    transcriptionId, audioS3Key, language ?? "auto-detect");

                // Step 1: Get OpenAI API key
                var apiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("OpenAI API key is not configured");
                }

                // Step 2: Generate download URL for the audio file from S3
                _logger.LogInformation("Generating S3 download URL for audio file");
                var audioUrl = await _s3Service.GenerateDownloadUrlAsync(audioS3Key);

                // Step 3: Download audio file from S3
                _logger.LogInformation("Downloading audio file from S3");
                var audioBytes = await _httpClient.GetByteArrayAsync(audioUrl);
                _logger.LogInformation("Downloaded {Size} bytes", audioBytes.Length);

                // Validate file size (Whisper API has 25MB limit)
                if (audioBytes.Length > 25 * 1024 * 1024)
                {
                    throw new InvalidOperationException("Audio file exceeds 25MB limit for Whisper API");
                }

                // Step 4: Prepare multipart form data for Whisper API
                using var content = new MultipartFormDataContent();

                // Add audio file with appropriate content type
                var fileContent = new ByteArrayContent(audioBytes);
                var extension = Path.GetExtension(audioS3Key).ToLower();
                var contentType = extension switch
                {
                    ".m4a" => "audio/mp4",
                    ".mp3" => "audio/mpeg",
                    ".wav" => "audio/wav",
                    ".aac" => "audio/aac",
                    ".ogg" => "audio/ogg",
                    ".webm" => "audio/webm",
                    _ => "audio/mp4" // Default to m4a
                };
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                content.Add(fileContent, "file", $"audio{extension}");

                // Add model parameter
                content.Add(new StringContent("whisper-1"), "model");

                // Add response format (json returns text and metadata)
                content.Add(new StringContent("json"), "response_format");

                // Add language hint if provided (improves accuracy)
                if (!string.IsNullOrEmpty(language))
                {
                    content.Add(new StringContent(language), "language");
                    _logger.LogInformation("Using language hint: {Language}", language);
                }

                // Step 5: Call Whisper API
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, WhisperApiUrl)
                {
                    Content = content
                };
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

                _logger.LogInformation("Sending transcription request to OpenAI Whisper API");
                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Whisper API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Whisper API returned {response.StatusCode}: {errorContent}");
                }

                // Step 6: Parse response
                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);

                var transcribedText = doc.RootElement
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                // Extract detected language from response (if available)
                string? detectedLanguage = null;
                if (doc.RootElement.TryGetProperty("language", out var langElement))
                {
                    detectedLanguage = langElement.GetString();
                }

                _logger.LogInformation(
                    "Transcription completed successfully. TranscriptionId={TranscriptionId}, TextLength={Length}, DetectedLanguage={Language}",
                    transcriptionId, transcribedText.Length, detectedLanguage ?? "unknown");

                return new TranscriptionResponseDto
                {
                    TranscribedText = transcribedText.Trim(),
                    TranscriptionId = transcriptionId,
                    Language = detectedLanguage
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during transcription. TranscriptionId={TranscriptionId}", transcriptionId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transcribe audio. TranscriptionId={TranscriptionId}, S3Key={S3Key}",
                    transcriptionId, audioS3Key);
                throw;
            }
        }
    }
}
