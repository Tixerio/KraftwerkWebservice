using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ConsumerEndpoint.Consumer;
namespace ConsumerEndpoint.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConsumerController : ControllerBase
    {
       private readonly Consumer.Consumer consumer;
       private readonly ConsumerListener consumerListener;
       private ILogger<ConsumerController> _logger;
       public ConsumerController(ILogger<ConsumerController> logger, ConsumerListener consumerListener)
        {
            
        }

        [HttpPost("ChangeIsConnected")]
        public IActionResult ChangeIsConnected([FromBody] bool request)
        {
            Consuming.isConsuming = request;
            Console.WriteLine("Got request, Consumer is now " + (!request ? "not " : " ") + "consuming.");
            return Ok(Consuming.isConsuming);
            }
    }
}