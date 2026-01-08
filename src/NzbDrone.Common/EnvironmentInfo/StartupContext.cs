using System.Collections.Generic;

namespace NzbDrone.Common.EnvironmentInfo
{
    public interface IStartupContext
    {
        HashSet<string> Flags { get; }
        Dictionary<string, string> Args { get; }
        bool Help { get; }

        string PreservedArguments { get; }
    }

    public class StartupContext : IStartupContext
    {
        public const string APPDATA = "data";
        public const string NO_BROWSER = "nobrowser";
        public const string HELP = "?";
        public const string TERMINATE = "terminateexisting";
        public const string RESTART = "restart";
        public const string NO_SINGLE_INSTANCE_CHECK = "nosingleinstancecheck";
        public const string EXIT_IMMEDIATELY = "exitimmediately";

        public StartupContext(params string[] args)
        {
            Flags = new HashSet<string>();
            Args = new Dictionary<string, string>();

            foreach (var s in args)
            {
                var flag = s.Trim(' ', '/', '-');

                var argParts = flag.Split('=');

                if (argParts.Length == 2)
                {
                    Args.Add(argParts[0].Trim().ToLower(), argParts[1].Trim(' ', '"'));
                }
                else
                {
                    Flags.Add(flag.ToLower());
                }
            }
        }

        public HashSet<string> Flags { get; private set; }
        public Dictionary<string, string> Args { get; private set; }

        public bool Help => Flags.Contains(HELP);
        public bool ExitImmediately => Flags.Contains(EXIT_IMMEDIATELY);

        public string PreservedArguments
        {
            get
            {
                var args = "";

                if (Args.ContainsKey(APPDATA))
                {
                    args = "/data=" + Args[APPDATA];
                }

                if (Flags.Contains(NO_BROWSER))
                {
                    args += " /" + NO_BROWSER;
                }

                return args.Trim();
            }
        }
    }
}
