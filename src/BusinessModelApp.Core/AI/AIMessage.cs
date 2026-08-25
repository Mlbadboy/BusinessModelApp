using System;

namespace BusinessModelApp.Core.AI
{
    public class AIMessage
    {
        public string Role { get; set; } = "user"; // system, user, assistant
        public string Content { get; set; } = string.Empty;

        public static AIMessage System(string content) => new AIMessage { Role = "system", Content = content };
        public static AIMessage User(string content) => new AIMessage { Role = "user", Content = content };
        public static AIMessage Assistant(string content) => new AIMessage { Role = "assistant", Content = content };
    }
}
