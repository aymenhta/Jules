using System.Text;

namespace Jules.Cli.Utils;


public static class JulesLogger
{
    public static void Info(string msg)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(msg);

        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.DarkGreen;
        Console.Write("INFO");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($" {msg} ({DateTime.UtcNow.ToString("HH:mm:ss")})");
        Console.ResetColor();
    }

    public static void Error(Exception e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var sb = new StringBuilder();
        sb.Append(e.Message);
        if (e.InnerException is not null)
        {
            sb.Append("\n\t|>");
            sb.Append(e.InnerException.Message);
        }

        var msg = sb.ToString();

        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.DarkRed;
        Console.Write("ERROR");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($" {msg} ({DateTime.UtcNow.ToString("HH:mm:ss")})");
        Console.ResetColor();
    }


    public static void Error(string msg)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(msg);

        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.DarkRed;
        Console.Write("ERROR");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($" {msg} ({DateTime.UtcNow.ToString("HH:mm:ss")})");
        Console.ResetColor();
    }
}
