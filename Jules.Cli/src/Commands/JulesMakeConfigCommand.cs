using System.Text.Json;
using System.Text.Json.Serialization;
using Jules.Cli.Utils;

namespace Jules.Cli.Commands;


/// create the configuration file in the current working directory
public sealed class JulesMakeConfigCommand : IJulesCommand
{
    public void Execute()
    {
        var jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };
        var content = JsonSerializer.SerializeToUtf8Bytes<JsonConfig>(new JsonConfig(), jsonOpts);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "jules.json");
        using var fs = File.Create(filePath);
        fs.Write(content);
        JulesLogger.Info("config file `jules.json` has been created successfully");
    }
}

