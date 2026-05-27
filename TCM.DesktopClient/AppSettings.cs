using System.IO;
using System.Text.Json;

namespace TCM.DesktopClient
{
    public class AppConfig
    {
        public string ApiBaseUrl { get; set; }
    }

    public static class AppSettings
    {
        public static string ApiBaseUrl { get; private set; } = "https://localhost:7243/";

        public static void Load()
        {
            try
            {
                if (File.Exists("appsettings.json"))
                {
                    string json = File.ReadAllText("appsettings.json");
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null && !string.IsNullOrWhiteSpace(config.ApiBaseUrl))
                    {
                        ApiBaseUrl = config.ApiBaseUrl;
                    }
                }
            }
            catch
            {

            }
        }
    }
}