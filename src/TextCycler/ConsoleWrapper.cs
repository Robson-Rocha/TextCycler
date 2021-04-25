using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TextCycler.Tests")]
namespace TextCycler
{
    public class ConsoleWrapper : IConsole
    {
        public ConsoleColor ForegroundColor { get => Console.ForegroundColor; set => Console.ForegroundColor = value; }

        public void AddHistory(params string[] text)
        {
            ReadLine2.AddHistory(text);
        }

        public string Read(string prompt, string @default)
        {
            return ReadLine2.Read(prompt, @default);
        }

        public void WriteLine(string value)
        {
            Console.WriteLine(value);
        }
    }
}
