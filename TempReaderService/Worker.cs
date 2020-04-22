using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Iot.Device.DHTxx;
using Newtonsoft.Json;

namespace TempReaderService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly List<Measure> _measures;
        private readonly SettingsOptions _options;

        public Worker(ILogger<Worker> logger, SettingsOptions options)
        {
            _logger = logger;
            _options = options;
            _measures = new List<Measure>();
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

        private Measure Read(DhtBase sensor)
        {
            var measure = new Measure();
            var temperature = (float)sensor.Temperature.Celsius;
            var humidity = sensor.Humidity;
            if (sensor.IsLastReadSuccessful && humidity>=0.0f && humidity<=100.0f && temperature>-100.0f && temperature<150.0f)
            {
                measure.DateTime = DateTime.Now;
                measure.Temperature = temperature;
                measure.Humidity = double.IsNaN(humidity) ? null : (float?)humidity;                
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

        static DateTime MapDate(DateTime dt, TimeSpan interval)
        {
            var ticks = (dt.Ticks / interval.Ticks) * interval.Ticks;
            return new DateTime(ticks);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int minValidMeasures = 5;
            var validMeasures = new List<Measure>();
            Measure lastMeasure = null; 
            var lastPost = DateTime.Now;
                        
            using (var dht = CreateSensor(_options.SensorType, _options.Pin))
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var startRun = DateTime.Now;
                    var measure = Read(dht);
                    
                    //if valid measure
                    if (measure.DateTime!=DateTime.MinValue)
                    {
                        _measures.Add(measure);
                        while (_measures.Count > _options.PostApiInterval / _options.Interval)
                        {
                            _measures.RemoveAt(0);
                        }

                        validMeasures = NoiseCut(_measures);
                        if (validMeasures.Count < minValidMeasures)
                        {
                            _logger.LogInformation(".");
                        }
                        else
                        {
                            lastMeasure = new Measure
                                    {
                                        SensorId = _options.SensorId,
                                        DateTime = startRun,
                                        Temperature = (float)Math.Round(validMeasures.Sum(x => x.Temperature) / validMeasures.Count, 1),
                                        Humidity = validMeasures.All(x => x.Humidity.HasValue) ? 
                                                (float)Math.Round(validMeasures.Sum(x => x.Humidity.Value) / validMeasures.Count, 1) : 
                                                (float?)null
                                    };
                            
                            _logger.LogInformation("Measured: {0}", lastMeasure);
                        }
                    }
                                    
                    if (validMeasures.Count >= minValidMeasures && startRun>=lastPost.AddMilliseconds(_options.PostApiInterval) && _options.PostApi)
                    {
                        lastPost = MapDate(startRun, new TimeSpan(0, 0, 0, 0, _options.PostApiInterval));
                        lastMeasure.DateTime = lastPost;
                        //Floor to minute
                        if (_options.PostApiInterval % 60000 == 0) lastMeasure.DateTime = new DateTime(lastMeasure.DateTime.Year, lastMeasure.DateTime.Month, lastMeasure.DateTime.Day, lastMeasure.DateTime.Hour, lastMeasure.DateTime.Minute, 0);

                        //POST to api
                        using (var client = new HttpClient())
                        {
                            var response = await client.PostAsync(_options.PostApiUrl, new StringContent(JsonConvert.SerializeObject(lastMeasure), Encoding.UTF8, "application/json"), stoppingToken);
                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogError(response.StatusCode + " " + response.Content);
                            }
                            else
                            {
                                _logger.LogInformation("POSTED");
                            }
                        }
                    }

                    //Calculate Delay                    
                    var nextRunDelay = new TimeSpan(_options.Interval * TimeSpan.TicksPerMillisecond) - (DateTime.Now - startRun);
                    if (nextRunDelay < TimeSpan.Zero) nextRunDelay = TimeSpan.Zero;
                    await Task.Delay(nextRunDelay, stoppingToken);
                }
            }
        }
    }
}
