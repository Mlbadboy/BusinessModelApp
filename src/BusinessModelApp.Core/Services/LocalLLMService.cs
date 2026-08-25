using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Services
{
    public class LocalLLMService : IAIService, IDisposable
    {
        private readonly ILogger<LocalLLMService> _logger;
        private LLamaWeights _model;
        private LLamaContext _context;
        private InteractiveExecutor _executor;
        private string _currentModelPath;

        public string ProviderName => "Local LLM (LLamaSharp)";
        public bool IsModelLoaded => _model != null;
        public string CurrentModelName => Path.GetFileName(_currentModelPath);

        public LocalLLMService(ILogger<LocalLLMService> logger)
        {
            _logger = logger;
        }

        public async Task LoadModelAsync(string modelPath)
        {
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"Model file not found at {modelPath}");
            }

            _logger.LogInformation($"Loading local model from: {modelPath}");

            // Dispose existing model if any
            Dispose();

            await Task.Run(() =>
            {
                var parameters = new ModelParams(modelPath)
                {
                    ContextSize = 4096, // Adjust based on memory
                    GpuLayerCount = 0   // CPU only for compatibility
                };

                _model = LLamaWeights.LoadFromFile(parameters);
                _context = _model.CreateContext(parameters);
                _executor = new InteractiveExecutor(_context);
                _currentModelPath = modelPath;
            });

            _logger.LogInformation("Model loaded successfully.");
        }

        public async Task<string> GetCompletionAsync(string prompt)
        {
            if (!IsModelLoaded)
            {
                // Return null so FallbackAIService can try the next provider
                return null; 
            }

            _logger.LogInformation("Generating response with Local LLM...");

            var inferenceParams = new InferenceParams()
            {
                AntiPrompts = new List<string> { "User:", "ACTION:" } // Stop generation at these tokens
            };

            var response = "";
            await foreach (var text in _executor.InferAsync(prompt, inferenceParams))
            {
                response += text;
            }

            return response;
        }

        public Task<float[]> GetEmbeddingAsync(string text)
        {
            // Embeddings not implemented yet for this service
            return Task.FromResult(new float[0]);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _model?.Dispose();
            _context = null;
            _model = null;
        }
    }
}
