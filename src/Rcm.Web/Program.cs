using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Rcm.Web
{
    public class Program
    {
        public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

        public static void Main(string[] args) =>
            CreateWebHostBuilder(args)
                .Build()
                .Run();

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost
                .CreateDefaultBuilder(args)
                .UseShutdownTimeout(ShutdownTimeout)
                .UseStartup<Startup>();
    }
}
