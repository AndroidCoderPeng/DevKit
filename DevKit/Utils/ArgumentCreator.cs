using System.Collections.Generic;

namespace DevKit.Utils
{
    public class ArgumentCreator : List<string>
    {
        public ArgumentCreator Append(string argument)
        {
            Add(argument);
            return this;
        }

        public string ToCommandLine()
        {
            return string.Join(" ", this);
        }
    }
}