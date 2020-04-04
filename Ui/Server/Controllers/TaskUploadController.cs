using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ui.Server.Controllers
{
    [Route("[controller]/[action]")]
    public class TaskUploadController : ControllerBase
    {
        private readonly UiAgent _uiAgent;

        public TaskUploadController(UiAgent uiAgent )
        {
            _uiAgent = uiAgent;
        }
        
        [DisableRequestSizeLimit]
        [HttpPost]
        public async Task<ActionResult> File(IFormFile file, string taskId)
        {
            _uiAgent.
            return Ok();
        }
    }
}