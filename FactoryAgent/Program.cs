using FactoryAgent.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var endpoint = "opc.tcp://localhost:4840";
        var deviceNames = new[] { "Device 1", "Device 2" };

        var reader = new OpcUaReader(endpoint);

        try
        {
            reader.Connect();
            Console.WriteLine("Connected with OPC UA Server");

            var connectionStrings = new[]
            {
                    "CONNECTION STRING",
                    "CONNECTION STRING"
            };
            var senders = deviceNames
                .Select((name, index) => new { Name = name, Sender = new IoTHubSender(connectionStrings[index]) })
                .ToDictionary(x => x.Name, x => x.Sender);

            while (true)
            {
                foreach (var device in deviceNames)
                {
                    var data = reader.ReadDevice(device);
                    Console.WriteLine($"[{DateTime.Now}] {device}: Status={data.ProductionStatus}, ...");
                    await senders[device].SendDataAsync(data);
                }

                Thread.Sleep(5000); // odczyt co 5 sekund
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
