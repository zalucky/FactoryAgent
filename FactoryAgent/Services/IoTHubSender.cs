using FactoryAgent.Models;
using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices.Shared;

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

        public async Task<int?> GetDesiredProductionRateAsync()
        {
            var twin = await deviceClient.GetTwinAsync();
            if (twin.Properties.Desired.Contains("ProductionRate"))
            {
                return twin.Properties.Desired["ProductionRate"];
            }
            return null;
        }
        public async Task UpdateReportedProductionRateAsync(int rate)
        {
            var reportedProperties = new TwinCollection();
            reportedProperties["ProductionRate"] = rate;
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            Console.WriteLine($"Updated Reported Property: ProductionRate = {rate}");
        }

        public async Task UpdateReportedDeviceErrorsAsync(int errorCode)
        {
            var reported = new TwinCollection();
            reported["DeviceErrors"] = errorCode;
            await deviceClient.UpdateReportedPropertiesAsync(reported);
            Console.WriteLine($"Updated Reported Property: DeviceErrors = {errorCode}");
        }

        public void SetMethodHandler(Func<string, Task<MethodResponse>> handler, string methodName)
        {
            deviceClient.SetMethodHandlerAsync(methodName, async (request, context) =>
            {
                return await handler(request.DataAsJson);
            }, null);
        }
    }
}
