namespace TempReaderService
{
    public class SettingsOptions {
        public int Interval {get;set;}
        public string SensorType {get;set;}
        public int Pin {get;set;}
        public int SensorId {get;set;}
        public string PostApiUrl {get;set;}
        public int PostApiInterval {get;set;}
        public bool PostApi {get;set;}
    }
}
