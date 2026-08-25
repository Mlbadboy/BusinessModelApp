using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using BusinessModelApp.Core.Services;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Core.Agents
{
    public class AutonomousAgent
    {
        private readonly IAIService _aiService;
        private readonly ICommandExecutionService _commandService;
        private readonly IFileSystemService _fileService;
        private readonly ILogger<AutonomousAgent> _logger;
        private readonly IAgentBroadcaster _broadcaster;

        private const int MaxIterations = 10;

        public AutonomousAgent(
            IAIService aiService,
            ICommandExecutionService commandService,
            IFileSystemService fileService,
            ILogger<AutonomousAgent> logger,
            IAgentBroadcaster broadcaster)
        {
            _aiService = aiService;
            _commandService = commandService;
            _fileService = fileService;
            _logger = logger;
            _broadcaster = broadcaster;
        }

        public async Task<string> RunAsync(string goal)
        {
            var history = new StringBuilder();
            history.AppendLine("You are an autonomous AI developer. You have access to a terminal and file system.");
            history.AppendLine("Your goal is: " + goal);
            history.AppendLine("You must strictly follow this format:");
            history.AppendLine("THOUGHT: <your reasoning>");
            history.AppendLine("ACTION: <one of the available commands>");
            history.AppendLine("");
            history.AppendLine("Available Commands:");
            history.AppendLine("1. EXECUTE: <command> (e.g., EXECUTE: echo hello)");
            history.AppendLine("2. WRITE_FILE: <path>|<content> (e.g., WRITE_FILE: test.txt|Hello World)");
            history.AppendLine("3. READ_FILE: <path> (e.g., READ_FILE: test.txt)");
            history.AppendLine("4. FINISH: <summary> (Use this when the goal is achieved)");
            history.AppendLine("");
            history.AppendLine("Begin.");

            for (int i = 0; i < MaxIterations; i++)
            {
                _logger.LogInformation($"[Agent] Iteration {i + 1}/{MaxIterations}");
                await _broadcaster.BroadcastLog($"[Agent] Iteration {i + 1}/{MaxIterations}");
                
                // 1. Get AI Response
                var prompt = history.ToString();
                var response = await _aiService.GetCompletionAsync(prompt);
                
                _logger.LogInformation($"[Agent] AI Response: {response}");
                await _broadcaster.BroadcastLog($"[Agent] AI Response: {response}");
                history.AppendLine(response);

                // 2. Parse Action
                var actionLine = ParseAction(response);
                if (string.IsNullOrEmpty(actionLine))
                {
                    var error = "OBSERVATION: ERROR: No valid ACTION found. Please specify an ACTION.";
                    history.AppendLine(error);
                    continue;
                }

                // 3. Execute Action
                if (actionLine.StartsWith("FINISH:"))
                {
                    return actionLine.Substring(7).Trim();
                }

                string observation;
                try
                {
                    observation = await ExecuteActionAsync(actionLine);
                }
                catch (Exception ex)
                {
                    observation = $"ERROR: {ex.Message}";
                }

                // 4. Update History
                var observationLog = $"OBSERVATION: {observation}";
                _logger.LogInformation($"[Agent] {observationLog}");
                await _broadcaster.BroadcastLog($"[Agent] {observationLog}");
                history.AppendLine(observationLog);
            }

            return "Failed to complete task within maximum iterations.";
        }

        private string ParseAction(string response)
        {
            var lines = response.Split('\n');
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("ACTION:"))
                {
                    return line.Trim().Substring(7).Trim();
                }
            }
            return null;
        }

        private async Task<string> ExecuteActionAsync(string action)
        {
            if (action.StartsWith("EXECUTE:"))
            {
                var cmd = action.Substring(8).Trim();
                return await _commandService.ExecuteCommandAsync(cmd);
            }
            else if (action.StartsWith("WRITE_FILE:"))
            {
                var parts = action.Substring(11).Split('|', 2);
                if (parts.Length < 2) return "ERROR: Invalid WRITE_FILE format. Use: path|content";
                var path = parts[0].Trim();
                var content = parts[1];
                await _fileService.WriteFileAsync(path, content);
                return $"File {path} written successfully.";
            }
            else if (action.StartsWith("READ_FILE:"))
            {
                var path = action.Substring(10).Trim();
                return await _fileService.ReadFileAsync(path);
            }

            return "ERROR: Unknown command.";
        }
    }
}
