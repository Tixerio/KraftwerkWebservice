using Microsoft.AspNetCore.Mvc;
using Powergrid.PowerGrid;


namespace Powergrid2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PowergridController : ControllerBase
    {
        private Grid grid;

        public PowergridController(Grid grid)
        {
            this.grid = grid;
        }

        [HttpPost("ChangeEnergy")]
        public IActionResult ChangeEnergy([FromBody] String request)
        {
         
            if(!grid.Members.ContainsKey(request))
            {
                return Ok("Unregistered.");
            }

            if(grid.Stopped)
            {
                return Ok("Not started yet.");
            }

            grid.ChangeEnergy(request);
            return Ok("Registered");
        }

        [HttpGet("GetExpectedConsume")]
        public IActionResult GetExpectedConsume()
        {
            return Ok(grid.GetExpectedConsume());
        }

        [HttpGet("GetEnergy")]
        public double GetEnergy()
        {
            return grid.AvailableEnergy;
        }


        [HttpGet("GetTime")]
        public IActionResult GetTime()
        {
            return Ok(grid.TimeInInt/60);
        }


        [HttpPost("Register")]

        public IActionResult Register([FromBody] MemberObject request)
        {
            var ID = Guid.NewGuid().ToString();
            switch (request.Type)
            {
                case "Powerplant":
                    grid.Members.Add(ID, new Powerplant(request.Name));
                    grid.MultiplicatorAmount.Add(ID, 5);
                    break;
                case "Consumer":
                    grid.Members.Add(ID, new Consumer(request.Name));
                    grid.MultiplicatorAmount.Add(ID, 500);
                    break;
            }


            Console.WriteLine("Registered");
          
            return Ok(ID);
        }

        [HttpPost("ForceBlackout")]
        public IActionResult ForceBlackout()
        {
            grid.AvailableEnergy = -40000;
            return Ok();
        }

        public class MemberObject
        {
            public String Name { get; set; }
            public String Type { get; set; }

        }
    }
}