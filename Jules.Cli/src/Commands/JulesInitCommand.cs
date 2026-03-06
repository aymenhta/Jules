using Dapper;
using Jules.Cli.Utils;
using Jules.Cli.Database;

namespace Jules.Cli.Commands;


public sealed class JulesInitCommand : IJulesCommand
{
    public SupportedDrivers Dialect { get; }
    public string DataSource { get; }
    public string Table { get; }

    public JulesInitCommand()
    {
        var config = JsonConfig.LoadAndValidateConfigFile();

        Dialect = config.Dialect;
        DataSource = config.DataSource;
        Table = Constants.MigrationsTableName;
    }

    private string GetDDL()
    {
        switch (Dialect)
        {
            case SupportedDrivers.Sqlite:
                return $"""
                CREATE TABLE IF NOT EXISTS {Table}(
                    Id INTEGER PRIMARY KEY,
                    LastAppliedMigrationId TEXT NOT NULL,
                    IsSuccessfull INTEGER NOT NULL
                );
                """;
            case SupportedDrivers.Psql:
                return $"""
                CREATE TABLE IF NOT EXISTS {Table}(
                    Id SERIAL PRIMARY KEY,
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
                            Id INT IDENTITY(1,1) PRIMARY KEY,
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

