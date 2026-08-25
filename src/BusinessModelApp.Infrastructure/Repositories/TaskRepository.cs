using BusinessModelApp.Core.DTOs.Task;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<TaskDto> CreateTaskAsync(CreateTaskDto taskDto, int creatorUserId)
        {
            var task = new TaskDto
            {
                Id = Guid.NewGuid(),
                Title = taskDto.Title,
                Description = taskDto.Description,
                DueDate = taskDto.DueDate,
                AssignedToUserId = taskDto.AssignedToUserId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            return Task.FromResult(task);
        }

        public Task<IEnumerable<TaskDto>> GetAllTasksAsync()
        {
            IEnumerable<TaskDto> tasks = new List<TaskDto>();
            return Task.FromResult(tasks);
        }

        public Task<TaskDto> GetTaskByIdAsync(Guid id)
        {
            var task = new TaskDto { Id = id };
            return Task.FromResult(task);
        }

        public Task UpdateTaskStatusAsync(Guid id, string status)
        {
            return Task.CompletedTask;
        }
    }
}
