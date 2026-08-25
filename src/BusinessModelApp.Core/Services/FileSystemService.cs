using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Services
{
    public interface IFileSystemService
    {
        Task WriteFileAsync(string path, string content);
        Task<string> ReadFileAsync(string path);
        Task CreateDirectoryAsync(string path);
        Task<bool> ExistsAsync(string path);
        Task<IEnumerable<string>> ListFilesAsync(string path);
    }

    public class FileSystemService : IFileSystemService
    {
        private readonly ILogger<FileSystemService> _logger;
        // Sandbox directory to prevent system-wide damage
        private readonly string _sandboxRoot;

        public FileSystemService(ILogger<FileSystemService> logger)
        {
            _logger = logger;
            // Default to a 'Workspace' folder in the app directory
            _sandboxRoot = Path.Combine(Environment.CurrentDirectory, "Workspace");
            if (!Directory.Exists(_sandboxRoot))
            {
                Directory.CreateDirectory(_sandboxRoot);
            }
        }

        private string GetFullPath(string relativePath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_sandboxRoot, relativePath));
            if (!fullPath.StartsWith(_sandboxRoot))
            {
                throw new UnauthorizedAccessException("Access to files outside the sandbox is denied.");
            }
            return fullPath;
        }

        public async Task WriteFileAsync(string path, string content)
        {
            var fullPath = GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _logger.LogInformation($"Writing file: {path}");
            await File.WriteAllTextAsync(fullPath, content);
        }

        public async Task<string> ReadFileAsync(string path)
        {
            var fullPath = GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
            return await File.ReadAllTextAsync(fullPath);
        }

        public Task CreateDirectoryAsync(string path)
        {
            var fullPath = GetFullPath(path);
            Directory.CreateDirectory(fullPath);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string path)
        {
            var fullPath = GetFullPath(path);
            return Task.FromResult(File.Exists(fullPath) || Directory.Exists(fullPath));
        }

        public Task<IEnumerable<string>> ListFilesAsync(string path)
        {
            var fullPath = GetFullPath(path);
            if (!Directory.Exists(fullPath))
            {
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
            }
            return Task.FromResult<IEnumerable<string>>(Directory.GetFiles(fullPath));
        }
    }
}
