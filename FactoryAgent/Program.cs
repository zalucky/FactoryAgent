using FactoryAgent.Services;
using Microsoft.Extensions.Configuration;
using FactoryAgent.Models;
using Microsoft.Azure.Devices.Client;

class Program
{
    static async Task Main(string[] args)
    {        
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var endpoint = config["OpcUa:Endpoint"];
        
        var devices = config.GetSection("IoTHub")
            .GetChildren()
            .Select(section => new DeviceConfig
            {
                Name = section.Key,
                ConnectionString = section.Value
            })
            .ToList();

        var reader = new OpcUaReader(endpoint);

        try
        {
            reader.Connect();
            Logger.Success("Connected with OPC UA Server");

            var senders = devices.ToDictionary(
                d => d.Name,
                d => new IoTHubSender(d.ConnectionString)
            );

            var previousErrors = devices.ToDictionary(d => d.Name, d => -1);
            var previousGoodCounts = devices.ToDictionary(d => d.Name, d => 0);
            var previousBadCounts = devices.ToDictionary(d => d.Name, d => 0);

            // EmergencyStop + ResetErrorStatus
            foreach (var device in devices.Select(d => d.Name))
            {
                var sender = senders[device];

                sender.SetMethodHandler(async (_) =>
                {
                    reader.CallMethod(device, "EmergencyStop");
                    Logger.Warn("EmergencyStop called via IoT Hub", device);
                    return new MethodResponse(200);
                }, "EmergencyStop");

                sender.SetMethodHandler(async (_) =>
                {
                    reader.CallMethod(device, "ResetErrorStatus");
                    Logger.Warn("ResetErrorStatus called via IoT Hub", device);
                    return new MethodResponse(200);
                }, "ResetErrorStatus");
            }

            while (true)
            {
                foreach (var device in devices.Select(d => d.Name))
                {
                    var data = reader.ReadDevice(device);
                    Logger.Info($"{device}: Status={data.ProductionStatus}, Temp={data.Temperature}°C, Errors={data.DeviceErrors}");
                    int goodDelta = data.GoodCount - previousGoodCounts[device];
                    int badDelta = data.BadCount - previousBadCounts[device];

                    previousGoodCounts[device] = data.GoodCount;
                    previousBadCounts[device] = data.BadCount;

                    var payload = new
                    {
                        data.DeviceName,
                        data.ProductionStatus,
                        data.WorkorderId,
                        data.Temperature,
                        data.ProductionRate,
                        data.DeviceErrors,
                        data.GoodCount,
                        data.BadCount,
                        GoodDelta = goodDelta,
                        BadDelta = badDelta,
                        Timestamp = DateTime.UtcNow
                    };

                    // dane telemetryczne
                    await senders[device].SendDataAsync(payload); 

                    // device twin
                    var desiredRate = await senders[device].GetDesiredProductionRateAsync();
                    if (desiredRate.HasValue)
                    {
                        Logger.Info($"[TWIN] Desired Production Rate for {device}: {desiredRate.Value}%");
                        if (reader.ReadDevice(device).ProductionRate != desiredRate.Value)
                        {
                            reader.WriteProductionRate(device, desiredRate.Value);
                            Logger.Warn($"Updated {device} Production Rate from {data.ProductionRate} to {desiredRate.Value}");
                        }
                    }
                    await senders[device].UpdateReportedProductionRateAsync(data.ProductionRate);

                    // DeviceErrors
                    if (previousErrors[device] != data.DeviceErrors)
                    {
                        await senders[device].UpdateReportedDeviceErrorsAsync(data.DeviceErrors);
                        previousErrors[device] = data.DeviceErrors;
                    }
                    Logger.Info($"[{DateTime.Now}] {device}: Errors={data.DeviceErrors}");
                }
                Thread.Sleep(5000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            reader.Disconnect();
        }
    }
}
