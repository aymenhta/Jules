using System.Data;

using Jules.Cli.Utils;
using Jules.Cli.Database;

namespace Jules.Cli.Commands;

/// <summary>
/// This commands gets all the un-appllied migration files,
/// and apply them sequentially one-by-one.
// </summary>
public sealed class JulesUpCommand : IJulesCommand
{
    public SupportedDrivers Dialect { get; }
    public string DataSource { get; }
    public string MigrationsDir { get; }
    public string MigrationsTableName { get; }


    public JulesUpCommand()
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
        var availableMigrations = MigrationFile.LoadAllFromDiskByMode(MigrationsDir, "up");

        if (!availableMigrations.Any())
        {
            throw new InvalidOperationException("Couldn't find any `up` migrations.");
        }

        if (lastAppliedMigration is not null)
        {
            // since there is a migration already applied ignore all the migrations before it and
            // only apply the migrations that comes after it sequentially

            var lastAppliedMigrationID = DateTime.Parse(
                lastAppliedMigration.LastAppliedMigrationId
            );

            if (lastAppliedMigration.IsSuccessfull)
            {
                availableMigrations = availableMigrations.Where(mf => mf.MigrationID > lastAppliedMigrationID);
            }
            else // include the last applied migration
            {
                availableMigrations = availableMigrations.Where(mf => mf.MigrationID >= lastAppliedMigrationID);
            }
        }

        // Order available migrations in ascending order
        availableMigrations = availableMigrations.OrderBy(mf => mf.MigrationID);

        MigrationFile? lastMigration = null;
        try
        {
            foreach (var migration in availableMigrations)
            {
                lastMigration = migration;
                lastMigration.Apply(conn);
                JulesLogger.Info($"migration {lastMigration.Fname} has been applied successfully.");
                julesMigrationTableDao.CreateOrUpdate(lastMigration, true);
                JulesLogger.Info($"migration {lastMigration.Fname} has been tracked successfully.");
            }
        }
        catch (Exception)
        {
            JulesLogger.Error($"migration {lastMigration!.Fname} failed to be applied.");
            julesMigrationTableDao.CreateOrUpdate(lastMigration, false);
            throw;
        }
    }
}
