using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TextCycler
{
    public class Config
    {
        public int currentText { get; set; }
        public string[] texts { get; set; }
        public int[][] counters { get; set; }
        public int[] counterPositions { get; set; }
        public string targetFile { get; set; }
    }

    class Program
    {
        static private void Fail(string message)
        {
            var currentConsoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine(message);
            Console.ForegroundColor = currentConsoleColor;
            Environment.Exit(-1);
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Fail("You need to set a config file in the first argument!");
            }

            string configFile = args[0];
            if (!File.Exists(configFile))
            {
                Fail($"The config file {configFile} does not exists or is inaccessible.");
            }

            Config config = null;
            try
            {
                config = JsonSerializer.Deserialize<Config>(File.ReadAllText(args[0]));
            }
            catch(Exception ex)
            {
                Fail($"The config file could not be parsed, or has errors: '{ex.Message}'");
            }
            
            if (config.texts.Length == 0)
            {
                Fail($"At least one text must be defined in the 'texts' array at the config file.");
            }

            int? currentText = null;
            if (args.Length == 2)
            {
                int i = 0;
                if (!int.TryParse(args[1], out i))
                {
                    Fail($"The second argument, if supplied, defines the desired text index within the config file, thus it needs to be numeric.");
                }
                currentText = i;
                if (currentText > config.texts.Length-1)
                {
                    Fail($"As you have {config.texts.Length} texts defined in the supplied config file, the index, if supplied, must be between 0 and {config.texts.Length -1}.");
                }
            }

            if (string.IsNullOrWhiteSpace(config.targetFile))
            {
                Fail($"The target file was not set.");
            }

            if (!File.Exists(config.targetFile))
            {
                Fail($"The target file '{config.targetFile}' does not exists.");
            }

            string text = config.texts[currentText ?? config.currentText];

            int countersLength = config.counters.Length;
            int counterPositionsLength = config.counterPositions.Length;
            if (countersLength > 0 && Regex.IsMatch(text, @"#COUNTER_\d{2}#"))
            {
                if (countersLength != counterPositionsLength)
                {
                    Fail($"You have defined {countersLength} counter sequences, and {counterPositionsLength} counter positions in the config file. The quantity of positions must match the quantity of counter sequences.");
                }

                for (int i = 0; i < countersLength; i++)
                {
                    string counterKey = $"#COUNTER_{i+1:00}#";
                    if (text.Contains(counterKey))
                    {
                        text = text.Replace(counterKey, config.counters[i][config.counterPositions[i]].ToString("00"));
                        config.counterPositions[i]++;
                        if (config.counterPositions[i] == config.counters[i].Length)
                        {
                            config.counterPositions[i] = 0;
                        }
                    }
                }
            }

            if (Regex.IsMatch(text, @"#TIME\+\d{2}#"))
            {
                for(int i = 1; i<=99; i++)
                {
                    text = text.Replace($"#TIME+{i:00}#", DateTime.Now.AddMinutes(i).ToString("HH:mm"));
                }
            }

            File.WriteAllText(config.targetFile, text, System.Text.Encoding.UTF8);

            if (currentText == null)
            {
                config.currentText++;
                if (config.currentText == config.texts.Length)
                {
                    config.currentText = 0;
                }
            }

            File.WriteAllBytes(configFile, JsonSerializer.SerializeToUtf8Bytes(config, options: new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        }
    }
}
