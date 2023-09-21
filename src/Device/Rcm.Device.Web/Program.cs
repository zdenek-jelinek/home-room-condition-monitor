using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Rcm.Device.Web
{
    public static class Program
    {
        public static TimeSpan ShutdownTimeout => TimeSpan.FromSeconds(10);

        public static async Task Main(string[] args)
        {
            var isService = !args.Contains("--console");
            if (isService)
            {
                InitializeCurrentDirectory();
            }

            var webHostBuilder = CreateWebHostBuilder(args);

            if (isService)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    webHostBuilder.UseSystemd();
                }
                else
                {
                    throw new NotImplementedException("Windows service support is not implemented yet.");
                }
            }

            var webHost = webHostBuilder.Build();

            try
            {
                await webHost.RunAsync();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static IHostBuilder CreateWebHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(ConfigureWebHost);
        }

        private static void ConfigureWebHost(IWebHostBuilder webBuilder)
        {
            webBuilder
                .UseShutdownTimeout(ShutdownTimeout)
                .UseStartup<Startup>();
        }

        private static void InitializeCurrentDirectory()
        {
            var currentProcessModule = Process.GetCurrentProcess().MainModule
                ?? throw new Exception("Could not access metadata of main module of current process.");

            var currentProcessFileName = currentProcessModule.FileName
                ?? throw new Exception("The current process main module has no file name.");

            var currentProcessModuleDirectory = Path.GetDirectoryName(currentProcessFileName)!;

            Directory.SetCurrentDirectory(currentProcessModuleDirectory);
        }
    }
}
