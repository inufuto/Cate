using System.Collections.Generic;

namespace Inu.Language
{
    public class NormalArgument
    {
        public readonly List<string> Values = new List<string>();
        public readonly Dictionary<string, List<string>> Options = new Dictionary<string, List<string>>();

        public NormalArgument(string[] args)
        {
            var i = 0;
            while (i < args.Length) {
                if (IsOption(args[i])) {
                    ++i;
                    var key = args[i].Substring(1).ToUpper();
                    if (!Options.TryGetValue(key, out var values)) {
                        values = new List<string>();
                    }
                    if (i >= args.Length) continue;
                    values.Add(args[i]);
                    ++i;
                }
                else {
                    Values.Add(args[i]);
                    ++i;
                }
            }
        }

        private static bool IsOption(string arg)
        {
            return arg.StartsWith('-') || arg.StartsWith('/');
        }
    }
}
