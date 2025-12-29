namespace Wihngo.Services.Interfaces
{
    public interface IS3Service
    {
        /// <summary>
        /// Generates a pre-signed URL for uploading a file to S3
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="mediaType">Type of media: profile-image, story-image, story-video, bird-profile-image, bird-video</param>
        /// <param name="fileExtension">File extension (e.g., .jpg, .mp4)</param>
        /// <param name="relatedId">Optional related ID (storyId for stories, birdId for birds)</param>
        /// <returns>Pre-signed upload URL and the S3 key</returns>
        Task<(string uploadUrl, string s3Key)> GenerateUploadUrlAsync(
            Guid userId, 
            string mediaType, 
            string fileExtension, 
            Guid? relatedId = null);

        /// <summary>
        /// Generates a pre-signed URL for downloading/viewing a file from S3
        /// </summary>
        /// <param name="s3Key">S3 key/path of the file</param>
        /// <returns>Pre-signed download URL</returns>
        Task<string> GenerateDownloadUrlAsync(string s3Key);

        /// <summary>
        /// Deletes a file from S3
        /// </summary>
        /// <param name="s3Key">S3 key/path of the file to delete</param>
        Task DeleteFileAsync(string s3Key);

        /// <summary>
        /// Checks if a file exists in S3
        /// </summary>
        /// <param name="s3Key">S3 key/path of the file</param>
        Task<bool> FileExistsAsync(string s3Key);

        /// <summary>
        /// Uploads a file directly to S3
        /// </summary>
        /// <param name="s3Key">S3 key/path for the file</param>
        /// <param name="stream">File content stream</param>
        /// <param name="contentType">MIME type of the file</param>
        Task UploadFileAsync(string s3Key, Stream stream, string contentType);
    }
}
