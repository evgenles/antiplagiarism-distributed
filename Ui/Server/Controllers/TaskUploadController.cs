using System;
using System.IO;
using System.Threading.Tasks;
using Grpc.Core.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Ui.Server.Controllers
{
    [Route("[controller]/[action]")]
    public class TaskUploadController : ControllerBase
    {
        private readonly UiAgent _uiAgent;
        private readonly ILogger<TaskUploadController> _logger;

        public TaskUploadController(UiAgent uiAgent, ILogger<TaskUploadController> logger)
        {
            _uiAgent = uiAgent;
            _logger = logger;
        }

        [DisableRequestSizeLimit]
        [HttpPost]
        public async Task<ActionResult> File(IFormFile file, string taskId)
        {
            try
            {
                await using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                await _uiAgent.UploadDocumentAsync(memoryStream.ToArray(), taskId);
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError("Can`t upload file for {@Task} {@Exception}", taskId, e.ToString());
                return StatusCode(500);
            }
        }
    }
}