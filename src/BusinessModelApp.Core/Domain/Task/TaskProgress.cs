using System;

namespace BusinessModelApp.Core.Domain.Task
{
    public class TaskProgress
    {
        public TimeSpan TimeSpent { get; set; }
        public int ProgressPercentage { get; set; }
        public string Notes { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public double? QualityScore { get; set; }
        public string[] CompletedMilestones { get; set; } = Array.Empty<string>();
        public string[] BlockersEncountered { get; set; } = Array.Empty<string>();

        public TaskProgress()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public TaskProgress(int progressPercentage, TimeSpan timeSpent, string updatedBy, string notes = null)
        {
            ProgressPercentage = progressPercentage;
            TimeSpent = timeSpent;
            UpdatedBy = updatedBy;
            Notes = notes;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsCompleted => ProgressPercentage >= 100;

        public void AddMilestone(string milestone)
        {
            var milestones = new string[CompletedMilestones.Length + 1];
            CompletedMilestones.CopyTo(milestones, 0);
            milestones[CompletedMilestones.Length] = milestone;
            CompletedMilestones = milestones;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddBlocker(string blocker)
        {
            var blockers = new string[BlockersEncountered.Length + 1];
            BlockersEncountered.CopyTo(blockers, 0);
            blockers[BlockersEncountered.Length] = blocker;
            BlockersEncountered = blockers;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
