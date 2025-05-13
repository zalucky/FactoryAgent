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
                    await senders[device].SendDataAsync(data);
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
