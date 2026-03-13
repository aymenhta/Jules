/*
Jules - database migration tool
Copyright (C) 2026 Aymen Hitta

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <https://www.gnu.org/licenses/>.
*/

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

        if (args[0] == "--verions" || args[0] == "-v")
        {
            PrintVersion();
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
            if (args.Contains("--showStackTrace"))
            {
                throw;
            }
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
                command = new JulesCreateCommand(name: args[1]);
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
        Console.WriteLine("jules [COMMAND] [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("\t- makeconfig: scaffold the configuration file in the current working directory");
        Console.WriteLine("\t- init: initialize/prepare the database for migrations");
        Console.WriteLine("\t- create: create a new migration file, in the specified migration directory");
        Console.WriteLine("\t- up: apply migrations");
        Console.WriteLine("\t- undo: undo migrations");
        // Console.WriteLine();
        // Console.WriteLine("Drivers:");
        // Console.WriteLine("\t- sqlite, mssql, psql");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("\t- --showStackTrace: show exceptions stack traces");
    }

    private static void PrintVersion()
    {
        Console.WriteLine("Jules V1.0.1");
    }
}


