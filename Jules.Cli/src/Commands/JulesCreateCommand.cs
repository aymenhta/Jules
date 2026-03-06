using System.Text;
using Jules.Cli.Utils;
using Jules.Cli.Database;

namespace Jules.Cli.Commands;


public sealed class JulesCreateCommand : IJulesCommand
{
    public Actions Action { get; private set; }
    public string MigrationName { get; private set; }
    public string MigrationsDirecory { get; private set; }

    public JulesCreateCommand(Actions action, string name, string dir = "./Migrations/")
    {
        Action = action;
        MigrationName = name;
        MigrationsDirecory = dir;
    }

    public void Execute()
    {
        var now = DateTime.UtcNow;
        var formattedDateTime = now.ToString(Constants.MigrationsIdFormat);
        var upFilename = $"{formattedDateTime}__{MigrationName}__up.sql";
        var downFilename = $"{formattedDateTime}__{MigrationName}__down.sql";

        if (!Directory.Exists(MigrationsDirecory))
        {
            Directory.CreateDirectory(MigrationsDirecory);
        }

        var upFullPath = MigrationsDirecory
            + (MigrationsDirecory.EndsWith("/") || MigrationsDirecory.EndsWith("\\")
                    ? string.Empty
                    : MigrationsDirecory.Last())
            + upFilename;
        var downFullPath = MigrationsDirecory
            + (MigrationsDirecory.EndsWith("/") || MigrationsDirecory.EndsWith("\\")
                    ? string.Empty
                    : MigrationsDirecory.Last())
            + downFilename;

        using var upfs = File.Create(upFullPath);
        using var downfs = File.Create(downFullPath);

        var commentDate = now.ToString("yyyy/MM/dd HH:mm:ss");

        var upinfo = (new UTF8Encoding(true)).GetBytes($"-- mode: \"UP\" ({commentDate})");
        var downinfo = (new UTF8Encoding(true)).GetBytes($"-- mode: \"DOWN\" ({commentDate})");

        upfs.Write(upinfo, 0, upinfo.Length);
        downfs.Write(downinfo, 0, downinfo.Length);

        JulesLogger.Info("migration " + upFilename + " has been create successfully");
        JulesLogger.Info("migration " + downFilename + " has been create successfully");
    }
}
