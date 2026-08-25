namespace BusinessModelApp.Core.Domain.Common
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; }
        void MarkAsDeleted();
    }
}
