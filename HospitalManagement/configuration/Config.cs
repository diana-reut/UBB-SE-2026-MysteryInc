using System;
using System.IO;
using System.Text.Json;

namespace HospitalManagement.Configuration
{
    public static class Config
    {
        public static string ConnectionString { get; private set; } = string.Empty;

        public static void Load()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "configuration", "appsettings.template.json");

            if (!File.Exists(filePath))
            {
                throw new Exception("Configuration file appsettings.local.json was not found.");
            }

            string jsonContent = File.ReadAllText(filePath);

            using JsonDocument document = JsonDocument.Parse(jsonContent);

            if (!document.RootElement.TryGetProperty("ConnectionStrings", out JsonElement connectionStringsSection))
            {
                throw new Exception("Missing 'ConnectionStrings' section in appsettings.local.json.");
            }

            if (!connectionStringsSection.TryGetProperty("DefaultConnection", out JsonElement defaultConnectionElement))
            {
                throw new Exception("Missing 'DefaultConnection' in appsettings.local.json.");
            }

            string? connectionString = defaultConnectionElement.GetString();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("The connection string is empty.");
            }

            ConnectionString = connectionString;
        }
    }
}