using System;

namespace TempReaderService
{
    public class Measure
    {
        public int SensorId { get; set;}
        public DateTime DateTime { get; set; }
        public float Temperature { get; set; }
        public float? Humidity { get; set; }

        public override string ToString()
        {
            return string.Format("{0:yyyy/MM/dd HH:mm:ss} Temperature: {1:0.0} °C", DateTime, Temperature) + 
                   (Humidity == null ? string.Empty : string.Format(" Humidity: {0:0.0} %", Humidity.Value)); 
        }
    }
}