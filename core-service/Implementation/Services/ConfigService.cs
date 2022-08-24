using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;

namespace AIQXCoreService.Implementation.Services
{
    public class ConfigService
    {
        private IConfiguration _config;

        public ConfigService(IConfiguration config)
        {
            _config = config;
        }

        private string Get(string key) => Environment.GetEnvironmentVariable(key) ?? _config[key];

        // Env
        public bool isDev() => Get("ASPNETCORE_ENVIRONMENT") == "Development";
        public int HttpPort() => int.Parse(Environment.GetEnvironmentVariable("APP_PORT_CORE") ?? Environment.GetEnvironmentVariable("APP_PORT") ?? _config["APP_PORT"]);
        public string FileServiceUrl() => Environment.GetEnvironmentVariable("APP_FILE_SERVICE_URL") ?? _config["APP_FILE_SERVICE_URL"];

        // SQL
        public string SqlConnStr() => Environment.GetEnvironmentVariable("APP_MSSQL_CONNSTR") ?? _config["APP_MSSQL_CONNSTR"];
        public string TablePrefix() => Environment.GetEnvironmentVariable("APP_TABLE_PREFIX") ?? _config["APP_TABLE_PREFIX"] ?? "";


        // Email configurations
        public string FrontendUseCaseDetailURL() => Environment.GetEnvironmentVariable("APP_FRONTEND_USE_CASE_DETAIL_URL") ?? _config["APP_FRONTEND_USE_CASE_DETAIL_URL"];
        public string SmtpConnHost() => Environment.GetEnvironmentVariable("APP_SMTP_CONN_HOST") ?? _config["APP_SMTP_CONN_HOST"] ?? "localhost";
        public int SmtpConnPort() => int.Parse(Environment.GetEnvironmentVariable("APP_SMTP_CONN_PORT") ?? _config["APP_SMTP_CONN_PORT"] ?? "1025");
        public string SmtpConnUserName() => Environment.GetEnvironmentVariable("APP_SMTP_CONN_USERNAME") ?? _config["APP_SMTP_CONN_USERNAME"];
        public string SmtpConnPassword() => Environment.GetEnvironmentVariable("APP_SMTP_CONN_PASSWORD") ?? _config["APP_SMTP_CONN_PASSWORD"];
        public string SmtpSenderName() => Environment.GetEnvironmentVariable("APP_SMTP_SENDER_NAME") ?? _config["APP_SMTP_SENDER_NAME"];
        public string SmtpSenderAddress() => Environment.GetEnvironmentVariable("APP_SMTP_SENDER_ADDRESS") ?? _config["APP_SMTP_SENDER_ADDRESS"];
        public string[] SmtpRecipientAddresses() => (Environment.GetEnvironmentVariable("APP_SMTP_RECIPIENT_ADDRESSES") ?? _config["APP_SMTP_RECIPIENT_ADDRESSES"] ?? "").Split(",", System.StringSplitOptions.RemoveEmptyEntries);
    }
}
