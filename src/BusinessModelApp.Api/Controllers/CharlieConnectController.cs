using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Connectors;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CharlieConnectController : ControllerBase
    {
        private readonly ICharlieConnectService _connectService;

        public CharlieConnectController(ICharlieConnectService connectService)
        {
            _connectService = connectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetConnections([FromQuery] Guid? workspaceId)
        {
            var wsId = workspaceId ?? Guid.Empty;
            var connections = await _connectService.GetConnectionsAsync(wsId);
            return Ok(new { success = true, connections });
        }

        [HttpGet("capabilities")]
        public IActionResult GetCapabilities()
        {
            return Ok(new { success = true, capabilities = ConnectorCapabilityRegistry.Capabilities });
        }

        public class ConnectRequest
        {
            public Guid? WorkspaceId { get; set; }
            public ConnectionProvider Provider { get; set; }
            public string AccountIdentifier { get; set; } = string.Empty;
            public List<string> Scopes { get; set; } = new();
        }

        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] ConnectRequest request)
        {
            var wsId = request.WorkspaceId ?? Guid.Empty;
            var conn = await _connectService.ConnectProviderAsync(wsId, request.Provider, request.AccountIdentifier, request.Scopes);
            return Ok(new { success = true, connection = conn });
        }

        public class DisconnectRequest
        {
            public Guid? WorkspaceId { get; set; }
            public ConnectionProvider Provider { get; set; }
        }

        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect([FromBody] DisconnectRequest request)
        {
            var wsId = request.WorkspaceId ?? Guid.Empty;
            var success = await _connectService.DisconnectProviderAsync(wsId, request.Provider);
            return Ok(new { success });
        }

        [HttpPost("test/{provider}")]
        public async Task<IActionResult> TestConnection([FromRoute] ConnectionProvider provider, [FromQuery] Guid? workspaceId)
        {
            var wsId = workspaceId ?? Guid.Empty;
            var conn = await _connectService.TestConnectionAsync(wsId, provider);
            return Ok(new { success = true, connection = conn });
        }
    }
}
