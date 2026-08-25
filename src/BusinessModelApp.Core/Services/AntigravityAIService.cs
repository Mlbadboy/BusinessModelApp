using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Services
{
    public class AntigravityAIService : IAIService
    {
        private readonly ILogger<AntigravityAIService> _logger;

        public string ProviderName => "Antigravity AI (Local)";

        public AntigravityAIService(ILogger<AntigravityAIService> logger)
        {
            _logger = logger;
            Console.WriteLine("[AntigravityAIService] Initialized. Ready to serve as ultimate fallback.");
        }

        public Task<string> GetCompletionAsync(string prompt)
        {
            _logger.LogInformation($"Antigravity AI generating response for prompt: {prompt}");

            // 1. Handle "Finish" state (if the prompt contains the observation of success)
            if (prompt.Contains("written successfully"))
            {
                return Task.FromResult("THOUGHT: The file has been created successfully. I can now finish the task.\nACTION: FINISH: Task completed. File created.");
            }

            // 2. Handle Generic File Creation
            if (prompt.Contains("Create a file named"))
            {
                // Simple parsing to extract filename and content
                // Expected format: "Create a file named [filename] with the content [content]"
                // We'll use a basic split to get the filename.
                
                string filename = "output.txt";
                string content = "Default content";

                try 
                {
                    var parts = prompt.Split(new[] { "named ", " with the content" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        filename = parts[1].Trim().Trim('\"', '\'');
                        // Content might be in the next part or just the rest of the string
                        var contentPart = prompt.Substring(prompt.IndexOf("content") + 7).Trim().Trim('\"', '\'');
                        content = contentPart;
                    }
                }
                catch {}

                return Task.FromResult($"THOUGHT: The user wants me to create a file named '{filename}'.\nACTION: WRITE_FILE: {filename}|{content}");
            }

            // 3. Default Business Logic (Existing)
            var response = "**Antigravity Insight:**\nTask analyzed and processed. Our autonomous systems are optimizing workflows to ensure maximum efficiency. Proceeding with standard operational protocols.";
            
            if (prompt.Contains("REVENUE"))
            {
                response += "\nREVENUE: 5000";
            }
            else if (prompt.Contains("EXPENSE"))
            {
                response += "\nEXPENSE: 1000";
            }

            return Task.FromResult(response);
        }

        public Task<float[]> GetEmbeddingAsync(string text)
        {
            // Return a dummy embedding
            return Task.FromResult(new float[768]);
        }
    }
}
