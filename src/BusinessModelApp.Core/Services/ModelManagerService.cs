using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Services
{
    public class ModelManagerService
    {
        private readonly string _modelsDirectory;
        private readonly ILogger<ModelManagerService> _logger;
        private readonly HttpClient _httpClient;

        public ModelManagerService(ILogger<ModelManagerService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _modelsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");
            
            if (!Directory.Exists(_modelsDirectory))
            {
                Directory.CreateDirectory(_modelsDirectory);
            }
        }

        public IEnumerable<string> ListModels()
        {
            return Directory.GetFiles(_modelsDirectory, "*.gguf");
        }

        public async Task DownloadModelAsync(string url, string fileName)
        {
            var filePath = Path.Combine(_modelsDirectory, fileName);
            _logger.LogInformation($"Downloading model from {url} to {filePath}...");

            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }

            _logger.LogInformation("Model download complete.");
        }

        public string GetModelPath(string fileName)
        {
            if (Path.IsPathRooted(fileName))
            {
                return fileName;
            }
            return Path.Combine(_modelsDirectory, fileName);
        }

        public async Task SaveModelAsync(Stream stream, string fileName)
        {
            var filePath = Path.Combine(_modelsDirectory, fileName);
            _logger.LogInformation($"Saving uploaded model to {filePath}...");

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fileStream);
            }

            _logger.LogInformation("Model saved successfully.");
        }
    }
}
