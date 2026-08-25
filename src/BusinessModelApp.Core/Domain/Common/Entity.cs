using System;

namespace BusinessModelApp.Core.Domain.Common
{
    public interface IEntity<TKey> where TKey : IEquatable<TKey>
    {
        TKey Id { get; set; }
        DateTime CreatedAt { get; }
        DateTime UpdatedAt { get; }
    }

    public abstract class Entity<TKey> : IEntity<TKey> where TKey : IEquatable<TKey>
    {
        public TKey Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        protected Entity()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        protected void UpdateTimestamps()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    // For backward compatibility and default Guid usage
    public abstract class Entity : Entity<Guid>
    {
        protected Entity()
        {
            Id = Guid.NewGuid();
        }
    }
}
