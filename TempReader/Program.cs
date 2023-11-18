using System;
using System.Collections.Generic;
using System.Linq;
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

        public override string ToString()
        {
            return string.Format("{0}  Temperature: {1:0.0} °C", DateTime, Temperature) + 
                   (Humidity == null ? string.Empty : string.Format(" Humidity: {0:0.0} %", Humidity.Value)); 
        }
    }

    class Program
    {
        const int DELAY = 2000;

        //https://github.com/adafruit/Adafruit_Python_DHT/blob/a609d7dcfb2b8208b88498c54a5c099e55159636/Adafruit_DHT/common.py
        //def read_retry(sensor, pin, retries=15, delay_seconds=2, platform=None)
        static Measure ReadRetry(DhtBase sensor, int retries=15)
        {
            var measure = new Measure();
            for (var i=0;i<retries;i++)
            {
                var temperature = (float)sensor.Temperature.Celsius;
                var humidity = sensor.Humidity;
                if (sensor.IsLastReadSuccessful && humidity>=0.0f && humidity<=100.0f && temperature>-100.0f && temperature<150.0f)
                {
                    measure.DateTime = DateTime.Now;
                    measure.Temperature = temperature;
                    measure.Humidity = double.IsNaN(humidity) ? null : (float?)humidity;
                    break;
                }                
                Thread.Sleep(DELAY);
            }
            return measure;
        }

        //https://forum.dexterindustries.com/t/solved-dht-sensor-occasionally-returning-spurious-values/2939/4
        static List<Measure> NoiseCut(List<Measure> measures)
        {
            if (measures == null || measures.Count() == 0) throw new ArgumentException();
            var avgT = measures.Sum(x => x.Temperature) / measures.Count;
            var stdDevT = Math.Sqrt(measures.Sum(x => (x.Temperature - avgT) * (x.Temperature - avgT)) / measures.Count);
            if (stdDevT > 0.0) measures = measures.Where(x => x.Temperature > avgT - stdDevT && x.Temperature < avgT + stdDevT).ToList();

            if (measures.All(x => x.Humidity.HasValue))
            {
                var avgH = measures.Sum(x => x.Humidity.Value) / measures.Count;
                var stdDevH = Math.Sqrt(measures.Sum(x => (x.Humidity.Value - avgH) * (x.Humidity.Value - avgH)) / measures.Count);
                if (stdDevH > 0.0) measures = measures.Where(x => x.Humidity.Value > avgH - stdDevH && x.Humidity.Value < avgH + stdDevH).ToList();
            }
            return measures;
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
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " Dht11|Dht21|Dht22 pinNumer [sensorId]");
                Console.WriteLine("      pinNumer: The pin number (GPIO number)");
                Console.WriteLine("      sensorId: 1 is default or any other integer");
                return;
            }
            if (args.Contains("-w"))
            {
                await MainForW1(args);
            }
            var name = args[0];
            var pin = Int32.Parse(args[1]);            
            var sensorId = args.Length>2 ? Int32.Parse(args[2]) : 1;
            using (var dht = CreateSensor(name, pin))
            {
                var measures = new List<Measure>();
                while (measures.Count < 5)
                {
                    var measuredValue = ReadRetry(dht);
                    if (measuredValue.DateTime == DateTime.MinValue)
                    {
                        Console.WriteLine("Failed to get reading.");
                        return;
                    }
                    #if (DEBUG) 
                    {
                        Console.WriteLine(measuredValue);
                    } 
                    #else
                    {
                        Console.Write(".");
                    }
                    #endif
                    measures.Add(measuredValue);
                    Thread.Sleep(DELAY);
                }
                Console.WriteLine();
                measures = NoiseCut(measures);

                var measure = new Measure
                {
                    SensorId = sensorId,
                    DateTime = measures.First().DateTime,
                    Temperature = (float)Math.Round(measures.Sum(x => x.Temperature) / measures.Count, 1),
                    Humidity = measures.All(x => x.Humidity.HasValue) ? (float)Math.Round(measures.Sum(x => x.Humidity.Value) / measures.Count, 1) : (float?)null
                };

                Console.WriteLine(measure);

                //Floor to minute
                measure.DateTime = new DateTime(measure.DateTime.Year, measure.DateTime.Month, measure.DateTime.Day, measure.DateTime.Hour, measure.DateTime.Minute, 0);

                //POST to api
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

        //Experimental code for 1-gpio temperature sensor
        //In /boot/config.txt add 
        //dtoverlay=w1-gpio,gpiopin=21
        //
        //To check if ok:
        //lsmod | grep w1
        //To see last read temp:
        //cat /sys/bus/w1/devices/28-030897945f4b/w1_slave
        //
        //Usage: ./TempReader /sys/bus/w1/devices/28-030897945f4b -w 
        static async System.Threading.Tasks.Task MainForW1(string[] args)
        {
            var dir = args[0]; // "/sys/bus/w1/devices/28-030897945f4b";
            if (System.IO.Directory.Exists(dir))
            {
                var lines = System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "w1_slave")).Split('\n');
                if (lines.Length==3)
                {
                    var s = lines[1].Substring(lines[1].IndexOf("t=")+"t=".Length);
                    var i = int.Parse(s);

                    var measure = new Measure
                    {
                        SensorId = 4,
                        DateTime = DateTime.Now,
                        Temperature = (float)i / 1000.0f,
                    };

                    //Floor to minute
                    measure.DateTime = new DateTime(measure.DateTime.Year, measure.DateTime.Month, measure.DateTime.Day, measure.DateTime.Hour, measure.DateTime.Minute, 0);

                    //POST to api
                    using (var client = new HttpClient())
                    {
                        var response = await client.PostAsync("http://localhost:5000/api/measure", new StringContent(JsonConvert.SerializeObject(measure), Encoding.UTF8, "application/json"));
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine(response.StatusCode + " " + response.Content);
                        }
                    }
                } else
                {
                    Console.WriteLine("Expected 3 lines: " + lines.Length);
                }
            } else
            {
                Console.WriteLine("Unable to find " + dir);
            }
                
        }
        
    }
}