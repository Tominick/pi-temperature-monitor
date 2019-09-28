using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using Iot.Device.DHTxx;
using Newtonsoft.Json;

namespace TempReader
{
    public class Measure
    {
        public int SensorId { get; set;}
        public DateTime DateTime { get; set; }
        public float Temperature { get; set; }
        public float? Humidity { get; set; }
    }

    class Program
    {

        //https://github.com/adafruit/Adafruit_Python_DHT/blob/a609d7dcfb2b8208b88498c54a5c099e55159636/Adafruit_DHT/common.py
        //def read_retry(sensor, pin, retries=15, delay_seconds=2, platform=None)
        static Measure ReadRetry(DhtBase sensor, int retries=15)
        {
            var measure = new Measure();
            for (var i=0;i<retries;i++)
            {
                var temperature = (float)sensor.Temperature.Celsius;
                var humidity = sensor.Humidity;
                if (sensor.IsLastReadSuccessful && humidity>=0.0f && humidity<=100.0f && temperature> -100.0f && temperature<150.0f)
                {
                    measure.DateTime = DateTime.Now;
                    measure.Temperature = temperature;
                    measure.Humidity = double.IsNaN(humidity) ? null : (float?)humidity;
                    break;
                }                
                Thread.Sleep(2000);
            }
            return measure;
        }

        static DhtBase CreateSensor(string name, int pin)
        {
            switch (name)
            {
                case "Dht11":
                 return new Dht11(pin);
                case "Dht21":
                 return new Dht21(pin);                
                case "Dht22":
                 return new Dht22(pin);
                default:
                 throw new NotImplementedException();
            }            
        }
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " Dht11|Dht21|Dht22 pinNumer [sensorId]");
                Console.WriteLine("      pinNumer: The pin number (GPIO number)");
                Console.WriteLine("      sensorId: 1 is default or any other integer");
                return;
            }
            var name = args[0];
            var pin = Int32.Parse(args[1]);            
            var sensorId = args.Length>2 ? Int32.Parse(args[2]) : 1;
            using (var dht = CreateSensor(name, pin))
            {
                var measure = ReadRetry(dht);
                if (measure.DateTime == DateTime.MinValue)
                {
                    Console.WriteLine("Failed to get reading.");
                    return;
                }
                measure.SensorId = sensorId;
                Console.WriteLine(measure.DateTime.ToString() + " Temperature: " + measure.Temperature.ToString("0.0") + " °C" + 
                                  (measure.Humidity==null ? string.Empty : (" Humidity: " + measure.Humidity.Value.ToString("0.0") + " % ")));
                
                //Floor to minute
                measure.DateTime = new DateTime(measure.DateTime.Year, measure.DateTime.Month, measure.DateTime.Day, measure.DateTime.Hour, measure.DateTime.Minute, 0);

                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync("http://localhost:5000/api/measure", new StringContent(JsonConvert.SerializeObject(measure), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(response.StatusCode + " " + response.Content);
                    }
                }
                
            }
        }
    }
}
