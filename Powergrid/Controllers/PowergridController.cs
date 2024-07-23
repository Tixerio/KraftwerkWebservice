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
                return StatusCode(418);
            }

            if(grid.Stopped)
            {
                return Ok("Not started yet.");
            }

            grid.ChangeEnergy(request);
            return Ok("Registered");
        }

        [HttpPost("StartNewDay")]
        public IActionResult StartNewDay()
        {
            grid.TimeInInt = 1450 - grid.TimeInInt;
            return Ok();
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
            return Ok(grid.TimeInInt / 60);
        }
        
        [HttpPost("Register")]

        public IActionResult Register([FromBody] MemberObject request)
        {
            var id = Guid.NewGuid().ToString();
            switch (request.Type)
            {
                case "Powerplant":
                    grid.Members.Add(id, new Powerplant(request.Name));
                    grid.MultiplicatorAmount.Add(id, 5);
                    break;
                case "Consumer":
                    grid.Members.Add(id, new Consumer(request.Name));
                    grid.MultiplicatorAmount.Add(id, 500);
                    grid.InitPlanMember();
                    break;
            }

            Console.WriteLine("Registered");
            Dictionary<string, string> transformedMembers = new();
            foreach (var (key, value) in grid.Members)
            {
                transformedMembers.Add(key, $"{value.Name}({value.GetType()})");
            }

            grid.Clients.All.ReceiveMembers(transformedMembers);
            return Ok(id);
        }

        [HttpPost("ForceBlackout")]
        [NonAction]
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