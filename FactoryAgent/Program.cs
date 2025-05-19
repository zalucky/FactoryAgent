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
            Console.WriteLine("Connected with OPC UA Server");

            var senders = devices.ToDictionary(
                d => d.Name,
                d => new IoTHubSender(d.ConnectionString)
            );

            var previousErrors = devices.ToDictionary(d => d.Name, d => -1);
            var previousGoodCounts = devices.ToDictionary(d => d.Name, d => 0);
            var previousBadCounts = devices.ToDictionary(d => d.Name, d => 0);

            while (true)
            {
                foreach (var device in devices.Select(d => d.Name))
                {
                    var data = reader.ReadDevice(device);
                    Console.WriteLine($"[{DateTime.Now}] {device}: Status={data.ProductionStatus}, ...");
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
                        Console.WriteLine($"[TWIN] Desired Production Rate for {device}: {desiredRate.Value}%");
                        if (reader.ReadDevice(device).ProductionRate != desiredRate.Value)
                        {
                            reader.WriteProductionRate(device, desiredRate.Value);
                            await senders[device].UpdateReportedProductionRateAsync(desiredRate.Value);
                        }
                    }
                    await senders[device].UpdateReportedProductionRateAsync(data.ProductionRate);

                    // DeviceErrors
                    if (previousErrors[device] != data.DeviceErrors)
                    {
                        await senders[device].UpdateReportedDeviceErrorsAsync(data.DeviceErrors);
                        previousErrors[device] = data.DeviceErrors;
                    }
                    Console.WriteLine($"[{DateTime.Now}] {device}: Errors={data.DeviceErrors}");

                    // EmergencyStop + ResetErrorStatus
                    var sender = senders[device];

                    // EmergencyStop
                    sender.SetMethodHandler(async (payload) =>
                    {
                        reader.CallMethod(device, "EmergencyStop");
                        return new MethodResponse(200);
                    }, "EmergencyStop");

                    // ResetErrorStatus
                    sender.SetMethodHandler(async (payload) =>
                    {
                        reader.CallMethod(device, "ResetErrorStatus");
                        return new MethodResponse(200);
                    }, "ResetErrorStatus");
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
