using System;
using System.Collections.Generic;

namespace Inu.Language
{
    public class NormalArgument
    {
        public readonly List<string> Values = new List<string>();

        public NormalArgument(string[] args, Func<string, string?, bool>? function = null)
        {
            var i = 0;
            while (i < args.Length) {
                if (IsOption(args[i])) {
                    var key = args[i].Substring(1).ToUpper();
                    ++i;
                    if (function == null) continue;
                    var value = (i < args.Length) ? args[i] : null;
                    if (function(key, value)) {
                        ++i;
                    }
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