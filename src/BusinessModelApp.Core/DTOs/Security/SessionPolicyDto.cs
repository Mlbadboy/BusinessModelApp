using System;

namespace BusinessModelApp.Core.DTOs.Security
{
    public class SessionPolicyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SessionTimeoutMinutes { get; set; }
        public bool AllowMultipleSessions { get; set; }
    }
}
