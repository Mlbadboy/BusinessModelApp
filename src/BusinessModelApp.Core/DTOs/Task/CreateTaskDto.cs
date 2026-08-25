using System;

namespace BusinessModelApp.Core.DTOs.Task
{
    public class CreateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public Guid? AssignedToUserId { get; set; }
    }
}
