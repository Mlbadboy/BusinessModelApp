namespace BusinessModelApp.Core.Domain.Users
{
    public static class Permission
    {
        // User permissions
        public const string UserRead = "user.read";
        public const string UserCreate = "user.create";
        public const string UserUpdate = "user.update";
        public const string UserDelete = "user.delete";
        public const string UserAssignRole = "user.assign.role";

        // Role permissions
        public const string RoleRead = "role.read";
        public const string RoleCreate = "role.create";
        public const string RoleUpdate = "role.update";
        public const string RoleDelete = "role.delete";

        // Business Model permissions
        public const string BusinessModelRead = "businessmodel.read";
        public const string BusinessModelCreate = "businessmodel.create";
        public const string BusinessModelUpdate = "businessmodel.update";
        public const string BusinessModelDelete = "businessmodel.delete";
        public const string BusinessModelPublish = "businessmodel.publish";

        // Dashboard permissions
        public const string DashboardView = "dashboard.view";
        public const string DashboardAnalytics = "dashboard.analytics";
        public const string DashboardAdmin = "dashboard.admin";

        // AI Integration permissions
        public const string AIRead = "ai.read";
        public const string AIConfigure = "ai.configure";
        public const string AIExecute = "ai.execute";

        // System permissions
        public const string SystemSettings = "system.settings";
        public const string SystemAudit = "system.audit";
        public const string SystemAdmin = "system.admin";

        public static string[] GetAll()
        {
            return new[]
            {
                UserRead, UserCreate, UserUpdate, UserDelete, UserAssignRole,
                RoleRead, RoleCreate, RoleUpdate, RoleDelete,
                BusinessModelRead, BusinessModelCreate, BusinessModelUpdate, BusinessModelDelete, BusinessModelPublish,
                DashboardView, DashboardAnalytics, DashboardAdmin,
                AIRead, AIConfigure, AIExecute,
                SystemSettings, SystemAudit, SystemAdmin
            };
        }
    }
}
