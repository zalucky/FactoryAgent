namespace FactoryAgent.Services
{
    public static class Logger
    {
        private static readonly object _lock = new();

        public static void Info(string message, string device = null) =>
            Write(message, "INFO", ConsoleColor.Gray, device);

        public static void Success(string message, string device = null) =>
            Write(message, "OK", ConsoleColor.Green, device);

        public static void Warn(string message, string device = null) =>
            Write(message, "WARN", ConsoleColor.Yellow, device);

        public static void Error(string message, string device = null) =>
            Write(message, "ERR", ConsoleColor.Red, device);

        private static void Write(string message, string level, ConsoleColor color, string device)
        {
            lock (_lock)
            {
                Console.ForegroundColor = color;
                var devicePart = device != null ? $"[{device}]" : "";
                //Console.WriteLine($"[{level}] {DateTime.Now:HH:mm:ss} {devicePart} {message}");
                Console.WriteLine($"[{level}] {DateTime.Now:HH:mm:ss}" + (device != null ? $" [{device}]" : "") + $" {message}");
                Console.ResetColor();
            }
        }
    }
}
