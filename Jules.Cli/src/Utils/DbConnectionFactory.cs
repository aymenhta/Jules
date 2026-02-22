using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Jules.Cli.Utils;


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


