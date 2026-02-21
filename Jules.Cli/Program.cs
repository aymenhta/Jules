using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Jules.Cli;

public static class Constants
{
    public static readonly string MigrationsTableName = "db_migrations";
}

public static class DbConnectionFactory
{
    public static IDbConnection Create(SupportedDrivers dialect, string dataSource)
    {

        return dialect switch
        {
            SupportedDrivers.Sqlite => new SqliteConnection(dataSource),
            SupportedDrivers.Mssql => new SqlConnection(dataSource),
            SupportedDrivers.Psql => new NpgsqlConnection(dataSource),
            _ => throw new InvalidOperationException("unkown SQL Dialect")
        };
    }
}


public sealed record MigrationsTable(DateTime LastAppliedMigrationId, bool IsSuccessfull);

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
            return new();
        }

    }
};


public interface IJulesCommand
{
    void Execute();
}


public sealed class JulesInitCommand : IJulesCommand
{
    public required SupportedDrivers Dialect { get; init; }
    public required string DataSource { get; init; }
    public required string Table { get; init; }

    // Factory Method
    public static JulesInitCommand Of(JsonConfig config)
    {
        var command = new JulesInitCommand
        {
            Dialect = config.Dialect,
            DataSource = config.DataSource,
            Table = Constants.MigrationsTableName,
        };

        if (command.Dialect != SupportedDrivers.Sqlite
            && command.Dialect != SupportedDrivers.Psql
            && command.Dialect != SupportedDrivers.Mssql)
        {
            throw new InvalidOperationException($"{command.Dialect.ToString()} is an unknown SQL dialect.");
        }

        if (string.IsNullOrWhiteSpace(config.DataSource))
        {
            throw new ArgumentNullException("Couldn't find the datasource.");
        }

        if (string.IsNullOrWhiteSpace(command.Table))
        {
            throw new ArgumentNullException("Couldn't find the migrations table.");
        }

        return command;
    }

    // TODO: Migrations Table name here needs proper validation
    private string GetDDL()
    {
        switch (Dialect)
        {
            case SupportedDrivers.Sqlite:
                return $"""
                CREATE TABLE IF NOT EXISTS {Constants.MigrationsTableName}(
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    LastAppliedMigrationId TEXT NOT NULL,
                    IsSuccessfull INTEGER CHECK (IsSuccessfull IN (0, 1))
                );
                """;
            case SupportedDrivers.Psql:
                return $"""
                CREATE TABLE IF NOT EXISTS {Constants.MigrationsTableName}(
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    LastAppliedMigrationId TEXT NOT NULL,
                    IsSuccessfull BOOLEAN NOT NULL
                );
                """;
            case SupportedDrivers.Mssql:
                return $"""
                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.tables
                        WHERE name = '{Constants.MigrationsTableName}'
                    )
                    BEGIN
                        CREATE TABLE dbo.{Constants.MigrationsTableName} (
                            Id INT NOT NULL PRIMARY KEY
                                CONSTRAINT CK_MigrationsTable_Id CHECK (Id = 1),

                            LastAppliedMigrationId NVARCHAR(MAX) NOT NULL,

                            IsSuccessfull BIT NOT NULL
                        );
                    END
                    """;
            default:
                throw new InvalidOperationException($"{Dialect.ToString()} is an unkown SQL dialect.");
        }
    }

    public void Execute()
    {
        using var conn = DbConnectionFactory.Create(Dialect, DataSource);
        conn.Open();
        int numberOfRows = conn.Execute(GetDDL());
        if (
            (Dialect == SupportedDrivers.Sqlite && numberOfRows == -1)
            || (Dialect == SupportedDrivers.Psql && numberOfRows != -1) // NPGSQL return -1 for DDL stmts
            || (Dialect == SupportedDrivers.Mssql && numberOfRows != 0) // Microsoft.Data.SqlClient return 0 for DDL stmts
        )
        {
            throw new InvalidOperationException("Failed to create the migrations table");
        }

        JulesLogger.Info("Migrations table has been created successfully");
    }
}


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
        var formattedDateTime = now.ToString("yyyyMMddHHmmss");
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

        if (args[0] == "--help" || args[0] == "-h")
        {
            PrintUsage();
            return;
        }


        try
        {
            var command = GetCommand(args);
            command!.Execute();
        }
        catch (Exception e)
        {
            JulesLogger.Error(e);
            throw; // TODO: For development only, remove when releasing; 
        }
    }

    private static IJulesCommand GetCommand(string[] args)
    {
        IJulesCommand? command = null;
        // single argument commands
        if (args.Length < 2)
        {
            if (args[0] == "makeconfig")
            {
                command = new JulesMakeConfigCommand();
            }
            else if (args[0] == "init")
            {
                JsonConfig? config = JsonConfig.LoadFromDisk();
                if (config is null) throw new InvalidOperationException("Couldn't find configuration file.");
                command = JulesInitCommand.Of(config);
            }
            else
            {
                throw new InvalidOperationException("unknown command");
            }
        }
        // double argument commands
        else
        {
            if (args[0] == "create")
            {
                command = new JulesCreateCommand(action: Actions.Create, name: args[1]);
            }
            else
            {
                throw new InvalidOperationException("unknown command");
            }
        }

        return command;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("jules [ACTION] [DRIVER] [CONNECTION STRING] [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("Actions:");
        Console.WriteLine("\t- init: initialize/prepare the database for migrations");
        Console.WriteLine("\t- makeconfig: scaffold the configuration file in the current working directory");
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


