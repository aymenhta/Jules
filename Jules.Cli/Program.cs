namespace Jules.Cli;

public static class FileContentHelpers
{
    public static string FileContents()
    {
        var sb = new System.Text.StringBuilder();

        sb.Append("-- +jules Up\n");
        sb.Append("-- +jules StatementBegin\n");
        sb.Append("SELECT 'up SQL query';\n");
        sb.Append("-- +jules StatementEnd\n");
        
        sb.Append("\n\n");

        sb.Append("-- +jules Down\n");
        sb.Append("-- +jules StatementBegin\n");
        sb.Append("SELECT 'down SQL query';\n");
        sb.Append("-- +jules StatementEnd\n");

        return sb.ToString();
    }
}

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
        var now = DateTime.UtcNow.ToString("yyyyMMddHHmms");
        var filename = $"{now}_{MigrationName}.sql";
        
        if (!Directory.Exists(MigrationsDirecory))
        {
            Directory.CreateDirectory(MigrationsDirecory);
        }

        var fullPath = MigrationsDirecory
            + (MigrationsDirecory.EndsWith("/") || MigrationsDirecory.EndsWith("\\")
                    ? string.Empty
                    : MigrationsDirecory.Last())
            + filename;

        
        File.WriteAllText(fullPath, FileContentHelpers.FileContents());
        
        Console.WriteLine("migration " + filename + " has been create successfully");
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


