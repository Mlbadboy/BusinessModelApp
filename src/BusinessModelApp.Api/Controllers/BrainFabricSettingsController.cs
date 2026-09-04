using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/brain")]
    [Authorize]
    public class BrainFabricSettingsController : ControllerBase
    {
        private readonly IBrainFabricGovernanceService _brainService;
        private readonly IUserContextService _userContext;
        private readonly ILogger<BrainFabricSettingsController> _logger;

        public BrainFabricSettingsController(
            IBrainFabricGovernanceService brainService,
            IUserContextService userContext,
            ILogger<BrainFabricSettingsController> logger)
        {
            _brainService = brainService;
            _userContext = userContext;
            _logger = logger;
        }

        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var providers = await _brainService.GetProvidersAsync(workspaceId, ct);
            return Ok(providers);
        }

        [HttpPost("providers")]
        public async Task<IActionResult> ConfigureProvider([FromBody] ConfigureBrainProviderDto dto, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var result = await _brainService.ConfigureProviderAsync(workspaceId, dto, ct);
            return Ok(result);
        }

        [HttpPost("providers/{provider}/probe")]
        public async Task<IActionResult> TestConnection(BrainProviderType provider, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var passed = await _brainService.TestProviderConnectionAsync(workspaceId, provider, ct);
            return Ok(new { provider = provider.ToString(), success = passed });
        }

        [HttpGet("providers/{provider}/models")]
        public async Task<IActionResult> DiscoverModels(BrainProviderType provider, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var models = await _brainService.DiscoverModelsAsync(workspaceId, provider, ct);
            return Ok(models);
        }

        [HttpPost("test-inference")]
        public async Task<IActionResult> TestInference([FromBody] BrainInferenceTestRequestDto dto, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var result = await _brainService.RunInferenceTestAsync(workspaceId, dto, ct);
            return Ok(result);
        }
    }
}
