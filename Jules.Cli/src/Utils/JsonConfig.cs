using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jules.Cli.Utils;


public sealed record JsonConfig(
    SupportedDrivers Dialect = SupportedDrivers.Sqlite,
    string DataSource = "Data Source=app.db",
    string Dir = "./Migrations"
)
{
    public static JsonConfig? LoadFromDisk()
    {
        try
        {
            var jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
            };
            var contents = File.ReadAllBytes("./jules.json");
            return JsonSerializer.Deserialize<JsonConfig>(contents, jsonOpts);
        }
        catch (FileNotFoundException)
        {
            return null;
        }

    }
};
