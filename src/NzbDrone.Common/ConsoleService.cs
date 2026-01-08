using System;
using System.Diagnostics;
using System.IO;

namespace NzbDrone.Common
{
    public interface IConsoleService
    {
        void PrintHelp();
    }

    public class ConsoleService : IConsoleService
    {
        public static bool IsConsoleAvailable => Console.In != StreamReader.Null;

        public void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("     Usage: {0} <command> ", Process.GetCurrentProcess().MainModule.ModuleName);
            Console.WriteLine("     Commands:");
            Console.WriteLine("                 /{0} Don't open Readarr in a browser", StartupContext.NO_BROWSER);
            Console.WriteLine("                 /{0} Start Readarr terminating any other instances", StartupContext.TERMINATE);
            Console.WriteLine("                 /{0}=path Path to use as the AppData location (stores database, config, logs, etc)", StartupContext.APPDATA);
            Console.WriteLine("                 <No Arguments>  Run application in console mode.");
        }
    }
}
