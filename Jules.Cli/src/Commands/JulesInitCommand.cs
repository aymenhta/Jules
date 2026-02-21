using Dapper;
using Jules.Cli.Utils;

namespace Jules.Cli.Commands;


public sealed class JulesInitCommand : IJulesCommand
{
    public SupportedDrivers Dialect { get; }
    public string DataSource { get; }
    public string Table { get; }

    public JulesInitCommand()
    {
        var config = GetAndValidateConfigFile();

        Dialect = config.Dialect;
        DataSource = config.DataSource;
        Table = Constants.MigrationsTableName;
    }

    private JsonConfig GetAndValidateConfigFile()
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

    // TODO: Migrations Table name here needs proper validation
    private string GetDDL()
    {
        switch (Dialect)
        {
            case SupportedDrivers.Sqlite:
                return $"""
                CREATE TABLE IF NOT EXISTS {Table}(
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    LastAppliedMigrationId TEXT NOT NULL,
                    IsSuccessfull INTEGER CHECK (IsSuccessfull IN (0, 1))
                );
                """;
            case SupportedDrivers.Psql:
                return $"""
                CREATE TABLE IF NOT EXISTS {Table}(
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
                        WHERE name = '{Table}'
                    )
                    BEGIN
                        CREATE TABLE dbo.{Table} (
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
        try
        {
            using var conn = DbConnectionFactory.Create(Dialect, DataSource);
            conn.Open();
            conn.Execute(GetDDL());
            JulesLogger.Info("Migrations table has been created successfully");
        }
        catch (System.Data.Common.DbException ex)
        {
            throw new InvalidOperationException("Failed to create the migrations table", ex);
        }
    }
}

