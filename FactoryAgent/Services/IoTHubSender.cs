using FactoryAgent.Services;
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

        public async Task SendDataAsync(object data)
        {
            var messageString = JsonSerializer.Serialize(data);
            var message = new Message(Encoding.UTF8.GetBytes(messageString));
            message.ContentType = "application/json";
            message.ContentEncoding = "utf-8";

            await deviceClient.SendEventAsync(message);
            Logger.Info($"Data sent to IoT Hub: {messageString}", "TELEMETRY");
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
        public async Task UpdateReportedProductionRateAsync(int rate, string deviceName)
        {
            var reportedProperties = new TwinCollection();
            reportedProperties["ProductionRate"] = rate;
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            Logger.Info($"Updated Reported Property: ProductionRate = {rate}", deviceName);
        }

        public async Task UpdateReportedDeviceErrorsAsync(int errorCode, string deviceName)
        {
            var reported = new TwinCollection();
            reported["DeviceErrors"] = errorCode;
            await deviceClient.UpdateReportedPropertiesAsync(reported);
            Logger.Info($"Updated Reported Property: DeviceErrors = {errorCode}", deviceName);
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
