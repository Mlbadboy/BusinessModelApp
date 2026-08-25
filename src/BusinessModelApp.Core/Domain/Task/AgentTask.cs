using System;
using System.Collections.Generic;
using AgentDomain = BusinessModelApp.Core.Domain.Agent;

namespace BusinessModelApp.Core.Domain.Task
{
    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum TaskStatus
    {
        New,
        Assigned,
        InProgress,
        Blocked,
        UnderReview,
        Completed,
        Cancelled
    }

    public class AgentTask
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DueDate { get; set; }
        public double EstimatedHours { get; set; }
        public string AssignedToId { get; set; }
        public string AssignedById { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, int> SkillRequirements { get; set; } = new Dictionary<string, int>();
        public TaskMetrics Metrics { get; set; }
        public List<TaskNote> Notes { get; set; } = new List<TaskNote>();
        public List<string> Blockers { get; set; } = new List<string>();
        public List<TaskActivity> ActivityLog { get; set; } = new List<TaskActivity>();

        // Navigation properties
        public virtual AgentDomain.Agent AssignedTo { get; set; }
        public virtual AgentDomain.Agent AssignedBy { get; set; }

        public AgentTask()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Metrics = new TaskMetrics();
        }

        public void AssignTo(string agentId, string assignedBy)
        {
            AssignedToId = agentId;
            AssignedById = assignedBy;
            Status = TaskStatus.Assigned;
            UpdatedAt = DateTime.UtcNow;
            
            AddActivity(TaskActivityType.Assignment, $"Task assigned to agent {agentId} by {assignedBy}");
        }

        public void UpdateStatus(TaskStatus newStatus, string updatedBy)
        {
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;

            if (newStatus == TaskStatus.Completed)
            {
                CompletedAt = DateTime.UtcNow;
                Metrics.CalculateFinalMetrics();
            }

            AddActivity(TaskActivityType.StatusChange, $"Status updated to {newStatus} by {updatedBy}");
        }

        public void AddNote(TaskNote note)
        {
            Notes.Add(note);
            UpdatedAt = DateTime.UtcNow;
            AddActivity(TaskActivityType.NoteAdded, $"Note added by {note.CreatedBy}");
        }

        public void UpdateProgress(TaskProgress progress)
        {
            Metrics.TimeSpent = progress.TimeSpent;
            Metrics.ProgressPercentage = progress.ProgressPercentage;
            UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(progress.Notes))
            {
                AddNote(new TaskNote
                {
                    Content = progress.Notes,
                    CreatedBy = progress.UpdatedBy,
                    CreatedAt = progress.UpdatedAt,
                    Type = NoteType.Progress
                });
            }

            AddActivity(TaskActivityType.ProgressUpdate, 
                $"Progress updated to {progress.ProgressPercentage}% by {progress.UpdatedBy}");
        }

        private void AddActivity(TaskActivityType type, string description)
        {
            ActivityLog.Add(new TaskActivity
            {
                Type = type,
                Description = description,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public class TaskMetrics
    {
        public TimeSpan TimeSpent { get; set; }
        public int ProgressPercentage { get; set; }
        public double? QualityScore { get; set; }
        public double? EfficiencyScore { get; set; }
        public double? ProductivityScore { get; set; }

        public void CalculateFinalMetrics()
        {
            // Implement metric calculations based on your business logic
            // This is a placeholder for the actual implementation
            ProductivityScore = (QualityScore + EfficiencyScore) / 2;
        }
    }

    public class TaskActivity
    {
        public TaskActivityType Type { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum TaskActivityType
    {
        Created,
        Assignment,
        StatusChange,
        ProgressUpdate,
        NoteAdded,
        BlockerAdded,
        BlockerResolved,
        MetricsUpdated,
        Completed,
        Cancelled
    }
}
