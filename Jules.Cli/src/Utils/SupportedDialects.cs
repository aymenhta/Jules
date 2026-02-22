namespace Jules.Cli.Utils;

// public sealed record MigrationsTable(string LastAppliedMigrationId, bool IsSuccessfull);

public enum SupportedDrivers
{
    Sqlite, Mssql, Psql
}

public enum Actions
{
    Init, Create, Up, Down
}
