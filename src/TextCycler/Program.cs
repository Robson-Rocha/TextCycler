using McMaster.Extensions.CommandLineUtils;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TextCycler.Tests")]
namespace TextCycler
{
    public static class Program
    {
        #region Public Methods
        public static void Main(string[] args)
        {
            Console.WriteLine($"TextCycler v{Assembly.GetExecutingAssembly().GetName().Version} by Robson Rocha de Araujo");
            Console.WriteLine("https://github.com/robson-rocha/textcycler");
            Console.WriteLine();
            CommandLineApplication.Execute<TextCycler>(args);
        }
#endregion
    }
}
