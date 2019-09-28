using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeasApi.Models
{
    public class Measure
    {
        public int SensorId { get; set;}
        public DateTime DateTime { get; set; }
        public float Temperature { get; set; }
        public float? Humidity { get; set; }
    }
}