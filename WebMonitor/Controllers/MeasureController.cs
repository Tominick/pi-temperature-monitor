using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using MeasApi.Models;
using System.Linq;
using System;

namespace MeasApi.Controllers
{
    [Route("api/Measure")]
    [ApiController]
    public class MeasureController : ControllerBase
    {
        private readonly MeasureContext _context;

        public MeasureController(MeasureContext context)
        {
            _context = context;            
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Measure>>> GetMeasureItems()
        {
            return await _context.Measures.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<Measure>>> GetMeasureItems(int id)
        {
            return await _context.Measures.Where(x=>x.SensorId == id).OrderBy(x => x.DateTime).ToListAsync();
        }

        [HttpGet("{id}/{count}")]
        public async Task<ActionResult<IEnumerable<Measure>>> GetMeasureItems(int id, int count)
        {
            //NotSupportedException: Could not parse expression 
            //return await _context.Measures.Where(x=>x.SensorId == id).OrderBy(x => x.DateTime).TakeLast(count).ToListAsync();
            return await _context.Measures.Where(x=>x.SensorId == id).OrderByDescending(x => x.DateTime).Take(count).ToListAsync();
        }

        [HttpGet("{id}/last")]
        public async Task<ActionResult<Measure>> GetLast(int id)
        {
            return await _context.Measures.Where(x=>x.SensorId == id).OrderBy(x => x.DateTime).LastAsync();
        }
        
        [HttpGet("{id}/date/{date}")]
        public async Task<ActionResult<IEnumerable<Measure>>> GetDate(int id, DateTime date)
        {
            return await _context.Measures
                .Where(x => x.SensorId == id && x.DateTime >= date && x.DateTime < date.AddDays(1))
                .OrderBy(x => x.DateTime)
                .ToListAsync();
        }

        [HttpGet("{id}/date/{date}/ByHour")]
        public async Task<ActionResult<IEnumerable<Measure>>> GetDateByHour(int id, DateTime date)
        {
            return await _context.Measures.Where(x => x.SensorId == id && x.DateTime >= date && x.DateTime < date.AddDays(1))
                         .GroupBy(x => x.DateTime.Hour).Select(g => new Measure() { 
                             SensorId = g.First().SensorId,
                             DateTime = g.First().DateTime.Date, 
                             Temperature = g.Average(x => x.Temperature), 
                             Humidity = g.Average(x => x.Humidity) }).OrderBy(x => x.DateTime).ToListAsync();
        }

        [HttpGet("{id}/from/{fromDate}/to/{toDate}/ByDay")]
        public async Task<ActionResult<IEnumerable<Measure>>> GetDateRangeByDay(int id, DateTime fromDate, DateTime toDate)
        {
            return await _context.Measures
                         .Where(x => x.SensorId == id && x.DateTime.Date >= fromDate && x.DateTime.Date <= toDate)
                         .GroupBy(x => x.DateTime.Date)
                         .Select(g => new Measure()
                         {
                             SensorId = g.First().SensorId,
                             DateTime = g.Key,
                             Temperature = g.Average(x => x.Temperature),
                             Humidity = g.Average(x => x.Humidity)
                         })
                         .OrderBy(x => x.DateTime)
                         .ToListAsync();
        }

        [HttpGet("{id}/date/{date}/ByHourMinMax")]
        public async Task<ActionResult<IEnumerable<object>>> GetDateByHourMinMax(int id, DateTime date)
        {
            return await _context.Measures
                        .Where(x => x.SensorId == id && x.DateTime >= date && x.DateTime < date.AddDays(1))
                         .GroupBy(x => x.DateTime.Hour).Select(g => new
                         {
                             SensorId = g.First().SensorId,
                             Hour = g.Key,
                             MinTemperature = g.Min(x => x.Temperature),
                             MaxTemperature = g.Max(x => x.Temperature),
                             MinHumidity = g.Min(x => x.Humidity),
                             MaxHumidity = g.Max(x => x.Humidity)
                         })
                         .OrderBy(x => x.Hour).ToListAsync();
        }


        // POST: api/Measure
        // Example:
        // {
        // "sensorId":1,
        // "dateTime": "2019-08-20T13:55:21.7244997",
        // "temperature": 25.0,
        // "humidity": null
        // }
        //
        [HttpPost]
        public async Task<ActionResult<Measure>> Post(/* [FromBody]*/ Measure measure)
        {
            if (!this.HttpContext.Request.Host.Value.StartsWith("localhost")) return Unauthorized();
            
            if (_context.Measures.Any(x=>x.SensorId == measure.SensorId && x.DateTime == measure.DateTime)) return BadRequest();
            
            _context.Measures.Add(measure);
            await _context.SaveChangesAsync();
            
            //return CreatedAtAction(nameof(measure), measure);
            return StatusCode(201);
        }

    }
}