using FactoryAgent.Services;

class Program
{
    static void Main(string[] args)
    {
        var endpoint = "opc.tcp://localhost:4840";
        var deviceNames = new[] { "Device 1", "Device 2" };

        var reader = new OpcUaReader(endpoint);

        try
        {
            reader.Connect();
            Console.WriteLine("Connected with OPC UA Server");

            while (true)
            {
                foreach (var device in deviceNames)
                {
                    var data = reader.ReadDevice(device);
                    Console.WriteLine($"[{DateTime.Now}] {data.DeviceName}: " +
                                      $"Status={data.ProductionStatus}, " +
                                      $"WorkorderId={data.WorkorderId}, " +
                                      $"Good={data.GoodCount}, Bad={data.BadCount}, " +
                                      $"Temp={data.Temperature}°C");
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
