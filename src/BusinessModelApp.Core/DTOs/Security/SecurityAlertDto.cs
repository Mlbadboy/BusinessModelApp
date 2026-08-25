using System;

namespace BusinessModelApp.Core.DTOs.Security
{
    public class SecurityAlertDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string AlertType { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
    }
}
