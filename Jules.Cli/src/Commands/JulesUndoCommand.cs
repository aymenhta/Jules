using Jules.Cli.Utils;
using Jules.Cli.Database;

namespace Jules.Cli.Commands;


public sealed class JulesUndoCommand : IJulesCommand
{
    public SupportedDrivers Dialect { get; }
    public string DataSource { get; }
    public string MigrationsDir { get; }
    public string MigrationsTableName { get; }

    public JulesUndoCommand()
    {
        var config = JsonConfig.LoadAndValidateConfigFile();

        Dialect = config.Dialect;
        DataSource = config.DataSource;
        MigrationsDir = config.Dir;
        MigrationsTableName = Constants.MigrationsTableName;
    }

    public void Execute()
    {
        using var conn = DbConnectionFactory.Create(Dialect, DataSource);
        conn.Open();

        var julesMigrationTableDao = new JulesMigrationTableDao(conn);

        var lastAppliedMigration = julesMigrationTableDao.GetLastAppliedMigration();
        if (lastAppliedMigration is null)
        {
            throw new InvalidOperationException("you don't have any applied migrations in the database.");
        }

        var lastAppliedMigrationId = DateTime.Parse(lastAppliedMigration.LastAppliedMigrationId);

        var beforeLastAppliedMigration = MigrationFile
            .LoadAllFromDiskByMode(MigrationsDir, "up")
            .Where(m => m.MigrationID < lastAppliedMigrationId)
            .OrderByDescending(m => m.MigrationID)
            .FirstOrDefault();

        var migration = MigrationFile.LoadOneFromDisk(
            MigrationsDir,
            lastAppliedMigrationId.ToString(Constants.MigrationsIdFormat),
            "down"
        );
        try
        {
            migration.Apply(conn);
            JulesLogger.Info($"migration {migration.Fname} has been cancelled successfully.");
        }
        catch (Exception)
        {
            JulesLogger.Error($"migration {migration.Fname} failed to be cancelled.");
            throw;
        }

        try
        {
            if (beforeLastAppliedMigration is null)
            {
                julesMigrationTableDao.Reset();
            }
            else
            {
                julesMigrationTableDao.CreateOrUpdate(beforeLastAppliedMigration, true);
                JulesLogger.Info($"migration {beforeLastAppliedMigration.Fname} is now the last successfully applied migration.");
            }
        }
        catch (Exception)
        {
            JulesLogger.Error($"failed to update the tracking table with the up-to-date migration");
            throw;
        }
    }
}
