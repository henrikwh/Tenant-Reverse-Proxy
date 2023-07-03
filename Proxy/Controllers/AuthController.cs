using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Proxy.Controllers
{
    //[Route("api/[controller]")]
    
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpPost]
        [Route("/auth")]
        [Authorize]
        public IActionResult ReadParam() {
            var u = this.User;
            return Ok();
        }
    }

}
