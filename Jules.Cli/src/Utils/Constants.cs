using System.Globalization;

namespace Jules.Cli.Utils;


public static class Constants
{
    public static readonly string MigrationsTableName = "JulesMigrationsTracker";
    public static readonly string MigrationsIdFormat = "yyyyMMddHHmmss";
    public static IFormatProvider FormatProvider = CultureInfo.InvariantCulture;
}
