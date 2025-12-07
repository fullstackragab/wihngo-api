using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Wihngo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // GET: api/<AuthController>
        [HttpGet]
        public string Get()
        {
            return "Successful";
        }

        // POST api/<AuthController>
        [HttpPost]
        public Object Post([FromBody] Object loginDto)
        {
            return loginDto;
        }
    }
}
