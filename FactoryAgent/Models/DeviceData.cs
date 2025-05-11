namespace FactoryAgent.Models
{
    public class DeviceData
    {
        public string DeviceName { get; set; }
        public int ProductionStatus { get; set; }
        public string WorkorderId { get; set; }
        public int GoodCount { get; set; }
        public int BadCount { get; set; }
        public double Temperature { get; set; }
    }
}
