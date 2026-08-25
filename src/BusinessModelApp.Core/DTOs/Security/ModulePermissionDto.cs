using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Security
{
    public class ModulePermissionDto
    {
        public string ModuleName { get; set; }
        public List<string> Permissions { get; set; }
    }
}
