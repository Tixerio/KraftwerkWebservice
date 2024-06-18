using Microsoft.AspNetCore.Mvc;
using Powergrid2.PowerGrid;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Markup;
using System.Reflection;


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
            
            if (grid.Members.ContainsKey(request) && grid.Started == true)
            {
                Console.WriteLine("Received Request from " + grid.Members[request]);
                grid.ChangeEnergy(request);
                return Ok("Registered");
            }
            if(!grid.Members.ContainsKey(request))
            {
                Console.WriteLine(grid.Members.Count());
                return Ok("Unregistered.");
            }

            if(!grid.Started)
            {
                return Ok("Not started yet.");
            }

            return Ok();
        }

        [HttpPost("GetIndividualPlan")]
        public IActionResult ConsumeEnergy([FromBody] String request)
        {
            return Ok(grid.GetIndividualPlan(request));
        }

        [HttpGet("GetEnergy")]
        public double GetEnergy()
        {
            return grid.AvailableEnergy;
        }

        [HttpPost("Register")]

        public IActionResult Register([FromBody] MemberObject request)
        {
            var ID = Guid.NewGuid().ToString();
            switch (request.Type)
            {
                case "Powerplant":
                    grid.Members.Add(ID, new Powerplant(request.Name));
                    break;
                case "Consumer":
                    grid.Members.Add(ID, new Consumer(request.Name));
                    break;
            }
            return Ok(ID);
        }

        [HttpPost("ForceBlackout")]
        public IActionResult ForceBlackout()
        {
            grid.AvailableEnergy = -40000;
            return Ok();
        }

        [HttpGet("BlackoutScenario")]
        public IActionResult BlackoutScenario()
        {
            grid.Blackout();
            return Ok();
        }

        [HttpGet("Start")]
        public IActionResult Start()
        {
            grid.Start();
            return Ok();
        }


        public class MemberObject
        {
            public String Name { get; set; }
            public String Type { get; set; }

        }
    }
}