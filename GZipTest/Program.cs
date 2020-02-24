using Microsoft.Extensions.DependencyInjection;

namespace GZipTest
{
    class Program
    {
        public static int Main(string[] args)
        {
            using var application = BuildServiceProvider().GetService<ConsoleApplication>();
            return application.Run(args);
        }

        private static ServiceProvider BuildServiceProvider() =>
            new ServiceCollection()
                .AddSingleton<IConsoleCommandArgumentsParser, ConsoleCommandArgumentsParser>()
                .AddSingleton<IConsoleCommandArgumentsValidator, ConsoleCommandArgumentsValidator>()
                .AddSingleton<ConsoleApplication>()
                .BuildServiceProvider();
    }
}
