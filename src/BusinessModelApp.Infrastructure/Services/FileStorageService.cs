using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService, IDisposable
    {
        private readonly string _basePath;
        private readonly ILogger<FileStorageService> _logger;
        private bool _disposed;

        public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Get base path from configuration or use default
            _basePath = configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            
            // Ensure the base directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                _logger.LogInformation("Created file storage directory at {BasePath}", _basePath);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be empty", nameof(fileName));

            try
            {
                // Create a unique file name to prevent overwriting
                var fileExtension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var relativePath = GetRelativePath(uniqueFileName);
                var fullPath = GetFullPath(relativePath);

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Save the file
                using (var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(file, 81920, cancellationToken); // 80KB buffer
                }

                _logger.LogInformation("Uploaded file {FileName} to {FilePath}", fileName, relativePath);
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", fileName);
                throw new InvalidOperationException("An error occurred while uploading the file", ex);
            }
        }

        public async Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            try
            {
                var fullPath = GetFullPath(filePath);
                
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found at path: {FilePath}", filePath);
                    return null;
                }

                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await fileStream.CopyToAsync(memoryStream, 81920, cancellationToken);
                }
                memoryStream.Position = 0;
                
                _logger.LogDebug("Downloaded file from {FilePath}", filePath);
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from {FilePath}", filePath);
                throw new InvalidOperationException("An error occurred while downloading the file", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                var fullPath = GetFullPath(filePath);
                
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                    return false;
                }

                // Use a retry policy for file operations which might be temporarily locked
                const int maxRetries = 3;
                const int delayMs = 100;
                
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        File.Delete(fullPath);
                        _logger.LogInformation("Deleted file at {FilePath}", filePath);
                        return true;
                    }
                    catch (IOException) when (i < maxRetries - 1)
                    {
                        await Task.Delay(delayMs * (i + 1), cancellationToken);
                    }
                }
                
                // If we get here, all retries failed
                _logger.LogError("Failed to delete file after {MaxRetries} attempts: {FilePath}", maxRetries, filePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file at {FilePath}", filePath);
                return false;
            }
        }

        public async Task<string> GetFileUrlAsync(string filePath, TimeSpan? expirationTime = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            try
            {
                var fullPath = GetFullPath(filePath);
                
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found when generating URL: {FilePath}", filePath);
                    return null;
                }

                // In a real implementation, this would generate a pre-signed URL for cloud storage
                // For local file system, we'll just return a file:// URL or a relative path
                // In a production environment, you'd typically use a CDN or cloud storage with proper authentication
                
                // For development purposes, return a relative path that can be served by the web server
                return $"/api/files/download?path={Uri.EscapeDataString(filePath)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating URL for file at {FilePath}", filePath);
                return null;
            }
        }

        public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return Task.FromResult(false);

            try
            {
                var fullPath = GetFullPath(filePath);
                return Task.FromResult(File.Exists(fullPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists at {FilePath}", filePath);
                return Task.FromResult(false);
            }
        }

        #region Helper Methods

        private string GetFullPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Relative path cannot be empty", nameof(relativePath));

            // Prevent directory traversal attacks
            var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
            if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Access to the specified path is not allowed");
            }

            return fullPath;
        }

        private string GetRelativePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            // Create a date-based directory structure (e.g., 2023/05/31/filename.ext)
            var now = DateTime.UtcNow;
            return Path.Combine(now.Year.ToString(), now.Month.ToString("D2"), now.Day.ToString("D2"), fileName);
        }

        #endregion

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here if needed
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~FileStorageService()
        {
            Dispose(disposing: false);
        }

        #endregion
    }
}
