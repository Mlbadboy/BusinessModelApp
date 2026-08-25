using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
        Task<Stream> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task<string> GetFileUrlAsync(string filePath, TimeSpan? expirationTime = null);
        Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
