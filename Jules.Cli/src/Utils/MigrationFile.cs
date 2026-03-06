using System.Data;
using Dapper;

namespace Jules.Cli.Utils;


public sealed class MigrationFile
{
    public string Fpath { get; }
    public string Fname { get; }
    public DateTime MigrationID { get; }

    public MigrationFile(string filePath)
    {
        Fpath = filePath;
        Fname = Path.GetFileName(Fpath);
        MigrationID = DateTime.ParseExact(Fname.Split("__")[0], Constants.MigrationsIdFormat, Constants.FormatProvider);
    }

    public IEnumerable<string> Queries => File
        .ReadAllText(Fpath)
        .Split(';')
        .Select(query => query.Trim())
        .Where(query => !string.IsNullOrWhiteSpace(query));


    public void Apply(IDbConnection conn)
    {
        foreach (var query in Queries)
        {
            conn.Execute(query);
        }
    }

    public static IEnumerable<MigrationFile> LoadAllFromDiskByMode(string migrationsDir, string migrationsMode)
    {
        if (!Directory.Exists(migrationsDir))
        {
            throw new InvalidOperationException($"The specified migrations directory `{migrationsDir}` doesn't exist.");
        }

        if (migrationsMode != "up" && migrationsMode != "down")
        {
            throw new InvalidOperationException($"Uknown migration mode `{migrationsMode}`.");
        }

        return Directory
            .EnumerateFiles(migrationsDir)
            .Where(f => f.Contains(migrationsMode))
            .Select(f => new MigrationFile(f));
    }

    public static MigrationFile LoadOneFromDisk(string migrationDir, string migrationID, string migrationMode)
    {
        if (!Directory.Exists(migrationDir))
        {
            throw new InvalidOperationException($"The specified migrations directory `{migrationDir}` doesn't exist.");
        }

        if (migrationMode != "up" && migrationMode != "down")
        {
            throw new InvalidOperationException($"Uknown migration mode `{migrationMode}`.");
        }

        var filepath = Directory
            .EnumerateFiles(migrationDir)
            .Where(f => f.Contains(migrationID) && f.Contains(migrationMode))
            .FirstOrDefault();
        if (filepath is null)
        {
            throw new InvalidOperationException($"Migration `{migrationID}` with mode `{migrationMode}` could not be found.");
        }

        return new MigrationFile(filepath);
    }
}
