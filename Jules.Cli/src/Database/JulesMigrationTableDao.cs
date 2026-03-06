using Dapper;
using System.Data;
using Jules.Cli.Utils;

namespace Jules.Cli.Database;


public sealed class JulesMigrationTableDao
{
    public sealed record DaoResult(string LastAppliedMigrationId, bool IsSuccessfull);

    private readonly IDbConnection _conn;

    public JulesMigrationTableDao(IDbConnection conn)
    {
        _conn = conn;
    }


    public DaoResult? GetLastAppliedMigration()
    {
        var query = $"SELECT LastAppliedMigrationId, IsSuccessfull FROM {Constants.MigrationsTableName}";
        return _conn.QueryFirstOrDefault<DaoResult>(query);
    }

    public void CreateOrUpdate(MigrationFile migration, bool isSuccessfull)
    {
        var query = $"""
			INSERT INTO {Constants.MigrationsTableName}(LastAppliedMigrationId, IsSuccessfull)
			VALUES (@Id, @Flag)
		""";

        if (GetLastAppliedMigration() is not null)
        {
            query = $"""
				UPDATE {Constants.MigrationsTableName} SET LastAppliedMigrationId = @Id, IsSuccessfull = @Flag
			""";
        }

        int nbRows = _conn.Execute(query, new { Id = migration.MigrationID, Flag = isSuccessfull });
        if (nbRows != 1)
        {
            throw new InvalidOperationException($"Couldn't persist the state of the migration {migration.Fname} into database.");
        }
    }

    public void Reset()
    {
        var query = $"DELETE FROM {Constants.MigrationsTableName}";
        int nbRows = _conn.Execute(query);
        if (nbRows != 1)
        {
            throw new InvalidOperationException("Failed to reset the migration tracking table to its initial state.");
        }
    }
}
