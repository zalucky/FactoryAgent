using FactoryAgent.Services;
using Microsoft.Extensions.Configuration;
using FactoryAgent.Models;

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

            while (true)
            {
                foreach (var device in devices.Select(d => d.Name))
                {
                    var data = reader.ReadDevice(device);
                    Console.WriteLine($"[{DateTime.Now}] {device}: Status={data.ProductionStatus}, ...");
                    
                    // dane telemetryczne
                    await senders[device].SendDataAsync(data); 

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
