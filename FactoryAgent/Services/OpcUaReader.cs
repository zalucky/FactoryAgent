using FactoryAgent.Models;
using Opc.UaFx.Client;

namespace FactoryAgent.Services
{
    public class OpcUaReader
    {
        private readonly OpcClient client;

        public OpcUaReader(string endpoint)
        {
            client = new OpcClient(endpoint);
        }

        public void Connect()
        {
            client.Connect();
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public DeviceData ReadDevice(string deviceName)
        {
            var prefix = $"ns=2;s={deviceName}";

            var productionStatus = client.ReadNode($"{prefix}/ProductionStatus").Value;
            var workorderId = client.ReadNode($"{prefix}/WorkorderId").Value;
            var goodCount = client.ReadNode($"{prefix}/GoodCount").Value;
            var badCount = client.ReadNode($"{prefix}/BadCount").Value;
            var temperature = client.ReadNode($"{prefix}/Temperature").Value;
            var productionRate = client.ReadNode($"{prefix}/ProductionRate").Value;
            var deviceErrors = client.ReadNode($"{prefix}/DeviceError").Value;

            return new DeviceData
            {
                DeviceName = deviceName,
                ProductionStatus = Convert.ToInt32(productionStatus),
                WorkorderId = workorderId?.ToString(),
                GoodCount = Convert.ToInt32(goodCount),
                BadCount = Convert.ToInt32(badCount),
                Temperature = Convert.ToDouble(temperature),
                ProductionRate = Convert.ToInt32(productionRate),
                DeviceErrors = Convert.ToInt32(deviceErrors)
            };
        }

        public void WriteProductionRate(string deviceName, int value)
        {
            var nodeId = $"ns=2;s={deviceName}/ProductionRate";
            client.WriteNode(nodeId, value);
        }
    }
}

