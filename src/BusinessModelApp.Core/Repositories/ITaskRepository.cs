using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Task;

namespace BusinessModelApp.Core.Repositories
{
    public interface ITaskRepository
    {
        Task<AgentTask> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentTask>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentTask>> GetByAgentIdAsync(string agentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentTask>> GetByFilterAsync(TaskFilter filter, CancellationToken cancellationToken = default);
        Task<AgentTask> CreateAsync(AgentTask task, CancellationToken cancellationToken = default);
        Task<AgentTask> UpdateAsync(AgentTask task, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<TaskStatistics> GetStatisticsAsync(string agentId, TimeSpan timeRange, CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentTaskMatch>> GetBestAgentsForTaskAsync(string taskId, CancellationToken cancellationToken = default);
        Task AddTaskNoteAsync(string taskId, TaskNote note, CancellationToken cancellationToken = default);
        Task UpdateProgressAsync(string taskId, TaskProgress progress, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentTask>> GetOverdueTasks(CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentTask>> GetTasksDueThisWeek(CancellationToken cancellationToken = default);
        Task<int> GetActiveTaskCountForAgent(string agentId, CancellationToken cancellationToken = default);
        Task<double> GetAverageCompletionTime(string agentId, CancellationToken cancellationToken = default);
    }

    public class TaskFilter
    {
        public IEnumerable<Domain.Task.TaskStatus> Status { get; set; }
        public IEnumerable<TaskPriority> Priority { get; set; }
        public string AssignedTo { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public DateTime? DueBefore { get; set; }
        public DateTime? DueAfter { get; set; }
        public IDictionary<string, int> SkillRequirements { get; set; }
        public bool? IsOverdue { get; set; }
        public bool? HasBlockers { get; set; }
    }

    public class AgentTaskMatch
    {
        public string AgentId { get; set; }
        public double MatchScore { get; set; }
        public IDictionary<string, double> SkillMatches { get; set; }
        public int CurrentWorkload { get; set; }
        public double HistoricalPerformance { get; set; }
    }

    public class TaskStatistics
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public TimeSpan AverageCompletionTime { get; set; }
        public IDictionary<TaskPriority, int> TasksByPriority { get; set; }
        public IDictionary<Domain.Task.TaskStatus, int> TasksByStatus { get; set; }
        public int OverdueTasks { get; set; }
        public double AverageQualityScore { get; set; }
        public double AverageEfficiencyScore { get; set; }
        public IDictionary<string, int> TasksByTag { get; set; }
        public int BlockedTasks { get; set; }
    }
}
