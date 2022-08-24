using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using AIQXCommon.Middlewares;
using AIQXCoreService.Implementation.Persistence;
using AIQXCoreService.Implementation.Services;
using AutoMapper;
using FluentEmail.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Opw.HttpExceptions.AspNetCore;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AIQXCoreService
{
    public class Startup
    {

        public IConfiguration Configuration { get; }
        public ILogger<Startup> Logger;

        public readonly static long StartTime = DateTimeOffset.Now.ToUnixTimeSeconds();

        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Configuration = configuration;
            Logger = LoggerFactory.Create(builder => { builder.AddSerilog(); }).CreateLogger<Startup>();
        }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ConfigService>();

            services.AddMvc().AddHttpExceptions(options =>
            {
                options.IncludeExceptionDetails = _ => false;
                options.ShouldLogException = exception =>
                {
                    Logger.LogError(exception, "Exception while processing request");
                    return false;
                };
            });

            services.AddAutoMapper(System.Reflection.Assembly.GetExecutingAssembly());
            services.AddControllers(options => options.Filters.Add(typeof(ResponseWrapperResultFilter)))
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                });

            var from = Environment.GetEnvironmentVariable("APP_SMTP_SENDER_ADDRESS") ?? Configuration["APP_SMTP_SENDER_ADDRESS"];
            var password = Environment.GetEnvironmentVariable("APP_SMTP_CONN_PASSWORD") ?? Configuration["APP_SMTP_CONN_PASSWORD"];
            var port = int.Parse(Environment.GetEnvironmentVariable("APP_SMTP_CONN_PORT") ?? Configuration["APP_SMTP_CONN_PORT"] ?? "1025");
            var connHost = Environment.GetEnvironmentVariable("APP_SMTP_CONN_HOST") ?? Configuration["APP_SMTP_CONN_HOST"] ?? "localhost";
            var connUserName = Environment.GetEnvironmentVariable("APP_SMTP_CONN_USERNAME") ?? Configuration["APP_SMTP_CONN_USERNAME"];
            var connPassword = Environment.GetEnvironmentVariable("APP_SMTP_CONN_PASSWORD") ?? Configuration["APP_SMTP_CONN_PASSWORD"];
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Configuration["ASPNETCORE_ENVIRONMENT"];

            services.AddFluentEmail(from)
                    .AddLiquidRenderer(options =>
                    {
                        options.FileProvider = new PhysicalFileProvider(Path.Combine($"{Directory.GetCurrentDirectory()}/", "Assets")); ;
                    })
                    .AddSmtpSender(new SmtpClient(connHost)
                    {
                        UseDefaultCredentials = false,
                        Port = port,
                        Credentials = new NetworkCredential(connUserName, connPassword),
                        EnableSsl = (env == "Development") ? false : true,
                    });

            // SETUP
            services.AddDbContext<AppDbContext>();
            services.AddScoped<PlantService>();
            services.AddScoped<UseCaseService>();
            services.AddScoped<AttachmentService>();
            services.AddScoped<NotificationService>();

            services.AddCors(o => o.AddPolicy("DevelopmentPolicy", builder =>
                {
                    builder.AllowAnyMethod().AllowAnyHeader().AllowCredentials().SetIsOriginAllowed(_ => true);
                }));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("docs", new OpenApiInfo { Title = "Core Service API", Version = "v1" });
                c.SwaggerGeneratorOptions = new SwaggerGeneratorOptions
                {
                    DescribeAllParametersInCamelCase = true
                };
                c.EnableAnnotations();
            });
            services.AddSwaggerGenNewtonsoftSupport();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppDbContext context)
        {
            context.Database.Migrate();

            app.UseHttpExceptions();

            if (env.IsDevelopment())
            {
                app.UseCors("DevelopmentPolicy");
                app.UseSwagger();
            }
            else
            {
                app.UseSwagger(c =>
                {
                    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                    {
                        swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"https://{httpReq.Host.Value}/core" } };
                    });
                });
            }

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("../swagger/v1/swagger.json", "Core Service API");
                c.RoutePrefix = "docs";
            });

            app.UseRouting();

            app.UseAuthMiddleware();
            app.UseRoleMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
