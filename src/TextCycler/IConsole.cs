using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TextCycler.Tests")]
namespace TextCycler
{
    public interface IConsole
    {
        ConsoleColor ForegroundColor { get; set; }

        void WriteLine(string value);

        string Read(string prompt, string @default);

        void AddHistory(params string[] text);
    }
}
