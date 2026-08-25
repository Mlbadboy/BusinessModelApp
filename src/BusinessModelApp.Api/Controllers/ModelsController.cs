using System.Threading.Tasks;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModelsController : ControllerBase
    {
        private readonly ModelManagerService _modelManager;
        private readonly LocalLLMService _localLLM;

        public ModelsController(ModelManagerService modelManager, LocalLLMService localLLM)
        {
            _modelManager = modelManager;
            _localLLM = localLLM;
        }

        [HttpGet]
        public IActionResult ListModels()
        {
            var models = _modelManager.ListModels();
            return Ok(new 
            { 
                Models = models,
                CurrentModel = _localLLM.CurrentModelName,
                IsLoaded = _localLLM.IsModelLoaded
            });
        }

        [HttpPost("download")]
        public async Task<IActionResult> DownloadModel([FromBody] DownloadModelRequest request)
        {
            if (string.IsNullOrEmpty(request.Url) || string.IsNullOrEmpty(request.FileName))
            {
                return BadRequest("Url and FileName are required.");
            }

            try
            {
                await _modelManager.DownloadModelAsync(request.Url, request.FileName);
                return Ok(new { Message = "Download started/completed." });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("load")]
        public async Task<IActionResult> LoadModel([FromBody] LoadModelRequest request)
        {
            if (string.IsNullOrEmpty(request.FileName))
            {
                return BadRequest("FileName is required.");
            }

            try
            {
                var path = _modelManager.GetModelPath(request.FileName);
                await _localLLM.LoadModelAsync(path);
                return Ok(new { Message = $"Model {request.FileName} loaded successfully." });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadModel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (!file.FileName.EndsWith(".gguf", System.StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only .gguf files are allowed.");
            }

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    await _modelManager.SaveModelAsync(stream, file.FileName);
                }
                return Ok(new { Message = $"Model {file.FileName} uploaded successfully." });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }

    public class DownloadModelRequest
    {
        public string Url { get; set; }
        public string FileName { get; set; }
    }

    public class LoadModelRequest
    {
        public string FileName { get; set; }
    }
}
