using System;

namespace BusinessModelApp.Core.Domain.Task
{
    public enum NoteType
    {
        General,
        Progress,
        Blocker,
        Quality,
        Feedback,
        System
    }

    public class TaskNote
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public NoteType Type { get; set; } = NoteType.General;
        public bool IsPrivate { get; set; } = false;
        public string[] Tags { get; set; } = Array.Empty<string>();

        public TaskNote()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public TaskNote(string content, string createdBy, NoteType type = NoteType.General)
        {
            Content = content;
            CreatedBy = createdBy;
            Type = type;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
