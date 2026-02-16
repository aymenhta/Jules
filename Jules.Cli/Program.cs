using System.Text;

namespace Jules.Cli;

public interface IJulesCommand
{
    void Execute();
}

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
        var formattedDateTime  = now.ToString("yyyyMMddHHmms");
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

        var upinfo = (new UTF8Encoding(true)).GetBytes($"-- mode: \"UP\" ({now.ToString("yyyy/MM/dd HH:mm:s")})");
        var downinfo = (new UTF8Encoding(true)).GetBytes($"-- mode: \"DOWN\" ({now.ToString("yyyy/MM/dd HH:mm:s")})");

        upfs.Write(upinfo, 0, upinfo.Length);
        downfs.Write(downinfo, 0, downinfo.Length);

        
        Console.WriteLine("migration " + upFilename + " has been create successfully");
        Console.WriteLine("migration " + downFilename + " has been create successfully");
    }
}


public enum SupportedDrivers
{
    Sqlite, Mssql, Psql
}

public enum Actions
{
    Init, Create, Up, Down
}

internal sealed class Program
{
    public static void Main(string[] args)
    {
        if (!args.Any())
        {
            PrintUsage();
            return;
        }

        if (args.Length < 2)
        {
            if (args[0] == "--help" || args[0] == "-h")
            {
                PrintUsage();
                return;
            }
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("ERROR: didn't provide enough number arguments");
            Console.ResetColor();
            return;
        }

        if (args.Length == 2)
        {
            IJulesCommand command = args[0] switch
            {
                "create" => new JulesCreateCommand(Actions.Create, args[1]),
                _ => throw new InvalidOperationException("ERROR: Invalid command")
            };

            command.Execute();
            return;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("jules [ACTION] [DRIVER] [CONNECTION STRING] [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("Actions:");
        Console.WriteLine("\t- init: initialize/prepare the database for migrations");
        Console.WriteLine("\t- create: create a new migration file, in the specified migration directory");
        Console.WriteLine("\t- up: apply migrations");
        Console.WriteLine("\t- down: undo migrations");
        Console.WriteLine();
        Console.WriteLine("Drivers:");
        Console.WriteLine("\t- sqlite, mssql, psql");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("\t- dir: migrations dir (default is './Migrations/')");
    }
}


