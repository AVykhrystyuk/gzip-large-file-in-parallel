using Microsoft.Extensions.DependencyInjection;

namespace GZipTest
{
    class Program
    {
        public static int Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddSingleton<IConsoleCommandArgumentsParser, ConsoleCommandArgumentsParser>()
                .AddSingleton<IConsoleCommandArgumentsValidator, ConsoleCommandArgumentsValidator>()
                .AddSingleton<ConsoleApplication>();

            var serviceProvider = services.BuildServiceProvider();

            using var application = serviceProvider.GetService<ConsoleApplication>();
            return application.Run(args);
        }
    }
}
