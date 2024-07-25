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

        /// <summary>
        /// Main endpoint to change the energy in the powergrid by a set value
        /// </summary>
        /// <param name="request"></param>
        /// <returns>changes energy</returns>
        /// <remarks>
        /// Beispiel:
        ///
        ///     Input:
        ///     {
        ///        "id"
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Changes the Energy in the powergrid by a certain value / Returns "Registered"</response>
        /// <response code="401">Either not registered or the server hasnt been started or isnt reachable</response>
        [HttpPost("ChangeEnergy")]
        public IActionResult ChangeEnergy([FromBody] String request)
        {
            if(!grid.Members.ContainsKey(request))
            {
                return StatusCode(401);
            }

            if(grid.Stopped)
            {
                return Ok("Not started yet.");
            }

            grid.ChangeEnergy(request);
            return Ok("Registered");
        }

        [HttpPost("StartNewDay")]
        [NonAction]
        public IActionResult StartNewDay()
        {
            grid.TimeInInt = 1435;
            return Ok();
        }

        /// <summary>
        /// Endpoint to return the expected consumption of the current day
        /// </summary>
        /// <param name="request"></param>
        /// <returns>changes energy</returns>
        /// <remarks>
        /// Beispiel:
        ///
        ///    Output:
        ///    {
        ///         "0": 0,
        ///         "1": 0,
        ///         "2": 0,
        ///         "3": 0,
        ///         "4": 0,
        ///         "5": 0,
        ///         "6": 0,
        ///         "7": 0,
        ///         "8": 0,
        ///         "9": 0,
        ///         "10": 0,
        ///         "11": 0,
        ///         "12": 0,
        ///         "13": 0,
        ///         "14": 0,
        ///         "15": 0,
        ///         "16": 0,
        ///         "17": 0,
        ///         "18": 0,
        ///         "19": 0,
        ///         "20": 0,
        ///         "21": 0,
        ///         "22": 0,
        ///         "23": 0
        ///    }
        ///
        /// </remarks>
        /// <response code="200">Returned the expected consumption for the current day"</response>
        [HttpGet("GetExpectedConsume")]
        public IActionResult GetExpectedConsume()
        {
            return Ok(grid.GetExpectedConsume());
        }

        /// <summary>
        /// Returns the current energy in the powergrid
        /// </summary>
        /// <param name="request"></param>
        /// <returns>changes energy</returns>
        /// <remarks>
        /// Beispiel:
        ///
        ///     Output:
        ///     {
        ///        "246"
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Successfully returned the current energy in the system</response>
        [HttpGet("GetEnergy")]
        public double GetEnergy()
        {
            return grid.AvailableEnergy;
        }

        /// <summary>
        /// Returns the current time in hours of the day
        /// </summary>
        /// <param name="request"></param>
        /// <returns>changes energy</returns>
        /// <remarks>
        /// Beispiel:
        ///
        ///     Output:
        ///     {
        ///        "21"
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Successfully returned the current time of the system</response>
        [HttpGet("GetTime")]
        public IActionResult GetTime()
        {
            return Ok(grid.TimeInInt / 60);
        }

        /// <summary>
        /// Lets you register to the powergrid, without it, you cant influence the energy in the powergrid
        /// </summary>
        /// <param name="request"></param>
        /// <returns>changes energy</returns>
        /// <remarks>
        /// Beispiel:
        ///
        ///     Input:
        ///     {
        ///        "name": "MyPowerplant",
        ///        "type": "Powerplant"
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Successfully registered to the powergrid</response>
        /// <response code="406">Couldnt register to the powergrid, might have not used "Powerplant" as type</response>
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
                default:
                {
                    return StatusCode(406);
                }
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