using BusinessModelApp.Core.DTOs;
using BusinessModelApp.Core.DTOs.Task;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Interfaces
{
    public interface ITaskRepository
    {
        Task<TaskDto> GetTaskByIdAsync(Guid id);
        Task<IEnumerable<TaskDto>> GetAllTasksAsync();
        Task<TaskDto> CreateTaskAsync(CreateTaskDto taskDto, int creatorUserId);
        Task UpdateTaskStatusAsync(Guid id, string status);
    }
}
