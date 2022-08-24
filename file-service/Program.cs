using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace AIQXFileService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .WriteTo.Console(
                outputTemplate: "[FILE SERVICE] [{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
             )
             .CreateLogger();

            try
            {
                Log.Information("Starting web host");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var port = Environment.GetEnvironmentVariable("APP_PORT_FILE") ?? Environment.GetEnvironmentVariable("APP_PORT");
                    if (!String.IsNullOrWhiteSpace(port))
                    {
                        webBuilder.UseUrls($"http://*:{port}").UseStartup<Startup>();
                    }
                    webBuilder.UseStartup<Startup>();
                })
            .UseSerilog();
    }
}
