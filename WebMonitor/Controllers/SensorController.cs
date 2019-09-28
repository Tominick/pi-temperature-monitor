using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using MeasApi.Models;
using System.Linq;

namespace MeasApi.Controllers
{
    [Route("api/Sensor")]
    [ApiController]
    public class SensorController : ControllerBase
    {
         private readonly MeasureContext _context;

        public SensorController(MeasureContext context)
        {
            _context = context;
        }

        // GET: api/Sensor
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sensor>>> GetSensorItems()
        {
            return await _context.Sensors.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<Sensor>>> GetSensorItems(int id)
        {
            return await _context.Sensors.Where(x=>x.SensorId == id).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Measure>> PostSensor(Sensor sensor)
        {
            if (!this.HttpContext.Request.Host.Value.StartsWith("localhost")) return Unauthorized();
            
            _context.Sensors.Add(sensor);
            await _context.SaveChangesAsync();
            
            return StatusCode(201);
        }
    }
}