using Microsoft.Extensions.DependencyInjection;

namespace GZipTest
{
    static class Program
    {
        public static int Main(string[] args)
        {
            GZipTest.Core.ProgramCore.Main(args);
            return 0;

            // using var application = BuildServiceProvider().GetService<ConsoleApplication>();
            // return application.Run(args);
        }

        private static ServiceProvider BuildServiceProvider() =>
            new ServiceCollection()
                .AddSingleton<IConsoleCommandArgumentsParser, ConsoleCommandArgumentsParser>()
                .AddSingleton<IConsoleCommandArgumentsValidator, ConsoleCommandArgumentsValidator>()
                .AddSingleton<ConsoleApplication>()
                .BuildServiceProvider();
    }
}
