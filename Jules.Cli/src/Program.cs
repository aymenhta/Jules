using Jules.Cli.Utils;
using Jules.Cli.Commands;

namespace Jules.Cli;


internal sealed class Program
{
    public static void Main(string[] args)
    {
        if (!args.Any())
        {
            PrintUsage();
            return;
        }

        if (args[0] == "--help" || args[0] == "-h")
        {
            PrintUsage();
            return;
        }


        try
        {
            var command = GetCommand(args);
            command!.Execute();
        }
        catch (Exception e)
        {
            JulesLogger.Error(e);
            throw; // TODO: For development only, remove when releasing; 
        }
    }

    private static IJulesCommand GetCommand(string[] args)
    {
        IJulesCommand? command = null;
        // single argument commands
        if (args.Length < 2)
        {
            if (args[0] == "makeconfig")
            {
                command = new JulesMakeConfigCommand();
            }
            else if (args[0] == "init")
            {
                command = new JulesInitCommand();
            }
            else if (args[0] == "up")
            {
                command = new JulesUpCommand();
            }
            else if (args[0] == "undo")
            {
                command = new JulesUndoCommand();
            }
            else
            {
                throw new InvalidOperationException("unknown command");
            }
        }
        // double argument commands
        else
        {
            if (args[0] == "create")
            {
                command = new JulesCreateCommand(action: Actions.Create, name: args[1]);
            }
            else
            {
                throw new InvalidOperationException("unknown command");
            }
        }

        return command;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("jules [ACTION] [DRIVER] [CONNECTION STRING] [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("Actions:");
        Console.WriteLine("\t- init: initialize/prepare the database for migrations");
        Console.WriteLine("\t- makeconfig: scaffold the configuration file in the current working directory");
        Console.WriteLine("\t- create: create a new migration file, in the specified migration directory");
        Console.WriteLine("\t- up: apply migrations");
        Console.WriteLine("\t- undo: undo migrations");
        Console.WriteLine();
        Console.WriteLine("Drivers:");
        Console.WriteLine("\t- sqlite, mssql, psql");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("\t- dir: migrations dir (default is './Migrations/')");
    }
}


