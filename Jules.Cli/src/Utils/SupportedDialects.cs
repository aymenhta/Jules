namespace Jules.Cli.Utils;

public sealed record MigrationsTable(DateTime LastAppliedMigrationId, bool IsSuccessfull);

public enum SupportedDrivers
{
    Sqlite, Mssql, Psql
}

public enum Actions
{
    Init, Create, Up, Down
}
