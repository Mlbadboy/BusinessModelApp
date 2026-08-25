using BusinessModelApp.Core.Domain.Common;
using BusinessModelApp.Core.Domain.Users;
using System;

namespace BusinessModelApp.Core.Domain.Tasks
{
    public class ProjectTask : Entity, ISoftDeletable
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public TaskStatus Status { get; private set; }
        public TaskPriority Priority { get; private set; }

        public Guid AssignedToId { get; private set; }
        public Guid AssignedById { get; private set; }

        public DateTime? DueDate { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        // Navigation properties
        public virtual User AssignedTo { get; private set; }
        public virtual User AssignedBy { get; private set; }

        public bool IsDeleted { get; private set; }

        private ProjectTask() { } // For EF Core

        public ProjectTask(string title, string description, TaskPriority priority, Guid assignedToId, Guid assignedById, DateTime? dueDate)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty.", nameof(title));
            if (assignedToId == Guid.Empty)
                throw new ArgumentException("AssignedToId cannot be empty.", nameof(assignedToId));
            if (assignedById == Guid.Empty)
                throw new ArgumentException("AssignedById cannot be empty.", nameof(assignedById));

            Title = title;
            Description = description ?? string.Empty;
            Status = TaskStatus.Pending;
            Priority = priority;
            AssignedToId = assignedToId;
            AssignedById = assignedById;
            DueDate = dueDate;
        }

        public void UpdateDetails(string title, string description, TaskPriority priority, DateTime? dueDate)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? string.Empty;
            Priority = priority;
            DueDate = dueDate;
        }

        public void StartTask()
        {
            if (Status == TaskStatus.Pending)
            {
                Status = TaskStatus.InProgress;
            }
        }

        public void CompleteTask()
        {
            if (Status != TaskStatus.Completed)
            {
                Status = TaskStatus.Completed;
                CompletedAt = DateTime.UtcNow;
            }
        }

        public void Reassign(Guid newAssigneeId)
        {
            if (newAssigneeId == Guid.Empty)
                throw new ArgumentException("New assignee ID cannot be empty.", nameof(newAssigneeId));

            AssignedToId = newAssigneeId;
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
        }
    }

    public enum TaskStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
