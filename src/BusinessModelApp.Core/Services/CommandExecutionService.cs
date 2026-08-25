using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Services
{
    public interface ICommandExecutionService
    {
        Task<string> ExecuteCommandAsync(string command, string workingDirectory = null);
    }

    public class CommandExecutionService : ICommandExecutionService
    {
        private readonly ILogger<CommandExecutionService> _logger;

        public CommandExecutionService(ILogger<CommandExecutionService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ExecuteCommandAsync(string command, string workingDirectory = null)
        {
            _logger.LogInformation($"Executing command: {command} in {workingDirectory ?? "current directory"}");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        outputBuilder.AppendLine(args.Data);
                        _logger.LogDebug($"[STDOUT] {args.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        errorBuilder.AppendLine(args.Data);
                        _logger.LogWarning($"[STDERR] {args.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = errorBuilder.ToString();
                    _logger.LogError($"Command failed with exit code {process.ExitCode}: {error}");
                    return $"ERROR: {error}";
                }

                return outputBuilder.ToString();
            }
        }
    }
}
