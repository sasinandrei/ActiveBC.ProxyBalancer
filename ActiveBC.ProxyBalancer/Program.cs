using ActiveBC.ProxyBalancer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ActiveBC.ProxyBalancer
{
    static class Program
    {
        static void Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            using var serviceProvider = new ServiceCollection()
               .AddLogging(logging =>
               {
                   logging.AddConsole();
               })
               .AddSingleton<IBalancer, Balancer>()
               .AddSingleton<IConfiguration>(configurationBuilder.Build())
               .AddSingleton<ProxyService>()
               .BuildServiceProvider();

            var proxyService = serviceProvider.GetService<ProxyService>();
            proxyService.Run();
        }
    }
}
