using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Rcm.Web
{
    public class Program
    {
        public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

        public static Task Main(string[] args) =>
            CreateWebHostBuilder(args)
                .Build()
                .RunAsync();

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(ConfigureWebHost);

        private static void ConfigureWebHost(IWebHostBuilder webBuilder) =>
            webBuilder
                .UseShutdownTimeout(ShutdownTimeout)
                .UseStartup<Startup>();
    }
}
