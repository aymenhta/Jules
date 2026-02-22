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

    public static JsonConfig LoadAndValidateConfigFile()
    {
        JsonConfig? config = JsonConfig.LoadFromDisk();
        if (config is null)
        {
            throw new InvalidOperationException("Couldn't find configuration file.");
        }


        if (config.Dialect != SupportedDrivers.Sqlite
            && config.Dialect != SupportedDrivers.Psql
            && config.Dialect != SupportedDrivers.Mssql)
        {
            throw new InvalidOperationException($"{config.Dialect.ToString()} is an unknown SQL dialect.");
        }

        if (string.IsNullOrWhiteSpace(config.DataSource))
        {
            throw new ArgumentNullException("Couldn't find the datasource.");
        }

        return config;
    }
};
