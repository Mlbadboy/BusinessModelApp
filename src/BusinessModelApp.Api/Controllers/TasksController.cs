using BusinessModelApp.Core.DTOs.Task;
using BusinessModelApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;

        public TasksController(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks()
        {
            var tasks = await _taskRepository.GetAllTasksAsync();
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<ActionResult<TaskDto>> CreateTask(CreateTaskDto createTaskDto)
        {
            Console.WriteLine($"[TasksController] Received CreateTask request for: {createTaskDto.Title}");
            // Use the assigned user ID from the DTO, or default to creator (1 - which is invalid for Guid, but we'll handle it)
            // Actually, MockTaskRepository expects int creatorUserId, but we want to assign the task to someone.
            // We'll pass 0 as creator for now since we don't have auth.
            var creatorUserId = 0; 
            var createdTask = await _taskRepository.CreateTaskAsync(createTaskDto, creatorUserId);
            return CreatedAtAction(nameof(GetTask), new { id = createdTask.Id }, createdTask);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTask(Guid id)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            return Ok(task);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(Guid id, [FromBody] string status)
        {
            await _taskRepository.UpdateTaskStatusAsync(id, status);
            return NoContent();
        }
    }
}
