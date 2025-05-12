using FactoryAgent.Models;
using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Text.Json;

namespace FactoryAgent.Services
{
    public class IoTHubSender
    {
        private readonly DeviceClient deviceClient;

        public IoTHubSender(string connectionString)
        {
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
        }

        public async Task SendDataAsync(DeviceData data)
        {
            var messageString = JsonSerializer.Serialize(data);
            var message = new Message(Encoding.UTF8.GetBytes(messageString));
            message.ContentType = "application/json";
            message.ContentEncoding = "utf-8";

            await deviceClient.SendEventAsync(message);
            Console.WriteLine($"Data sent to IoT Hub: {messageString}");
        }
    }
}
