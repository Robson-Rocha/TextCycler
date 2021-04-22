using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;

namespace TextCycler
{

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

        static private void Succeed(string message, bool exitImmediately = true)
        {
            var currentConsoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            System.Console.WriteLine(message);
            Console.ForegroundColor = currentConsoleColor;
            if (exitImmediately)
                Environment.Exit(0);
        }

        private static DateTime RoundToNearest(DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            bool roundUp = delta > d.Ticks / 2;
            var offset = roundUp ? d.Ticks : 0;

            return new DateTime(dt.Ticks + offset - delta, dt.Kind);
        }

        private static bool WaitNextCycle(int? interval)
        {
            if (interval == null)
                return false;
            Thread.Sleep(interval.Value * 1000);
            return true;
        }

        static void Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        [Required]
        [Option(CommandOptionType.SingleValue, ShortName = "c", LongName = "configFile", ShowInHelpText = true, 
                ValueName = "Path to the JSON config file", Description = "The config file to be used. To generate a blank config file, use the --generateConfig option with this option, and a blank config file will be generated in this path.")]
        public string ConfigFile { get; set; }

        [Option(CommandOptionType.NoValue, LongName = "generateConfig", ShowInHelpText = false)]
        public bool GenerateConfigFile { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "i", LongName = "index", ShowInHelpText = true,
                ValueName = "Index of the text to be set on the output file", Description = "Defines index of the text to be set on the output file. If not provided, TextCycler will cicle through the texts provided in the config file.")]
        public int? TextIndex { get; set; }

        [Option(CommandOptionType.MultipleValue, ShortName = "s", LongName = "sequenceValue", ShowInHelpText = true,
                ValueName = "{SequenceIndex},{SequenceValue}", Description = "Defines an arbitrary value for a sequence. Specify the sequence index and the value, separated by a comma.")]
        public string[] SequenceValues { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "f", LongName = "targetFile", ShowInHelpText = true,
                ValueName = "Path to the file to write the next text", Description = "Overrides the 'targetFile' setting in the config file with a path to a file in which the next text will be written to.")]
        public string TargetFile { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "t", LongName = "time", ShowInHelpText = true,
                ValueName = "Time", Description = "Overrides the current time for the time replacement tokens.")]
        public DateTime? CurrentTime { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "a", LongName = "autoCycle", ShowInHelpText = true,
                ValueName = "Interval in seconds between each cycle", Description = "Automatically cycle the texts, pausing for the specified interval.")]
        public int? CycleInterval { get; set; }

        [Option(CommandOptionType.NoValue, ShortName = "p", LongName = "prompt", ShowInHelpText = true,
                Description = "Prompts for text.")]
        public bool PromptForText { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used implicitly by CommandLineApplication.Execute")]
        private void OnExecute()
        {
            if (!File.Exists(ConfigFile))
            {
                if (GenerateConfigFile)
                {
                    Config blankConfig = new Config
                    {
                        nextTextIndex = null,
                        targetFile = "targetfile.txt",
                        texts = new [] 
                        {
                            "First text", 
                            "Second text, with Current Time (#TIME#), and Rounded to Nearest 5 Minutes Time (#NTIME#)", 
                            "Third Text, with the sequence 0 (#SEQUENCE_00#) from 01 to 05",
                            "Fourth Text, with the sequence 1 (#SEQUENCE_01#) using text values"
                        },
                        sequences = new [] 
                        { 
                            new [] { "01", "02", "03", "04", "05" },
                            new [] { "lorem", "ipsum", "dolor" }
                        },
                        sequencePositions = null,
                        ConfigPath = ConfigFile
                    };
                    blankConfig.Save();
                    Succeed($"A sample config file was generated at '{ConfigFile}'.");
                }
                Fail($"The config file {ConfigFile} does not exists or is inaccessible.");
            }

            Config config = null;
            try
            {
                config = Config.Load(ConfigFile);
            }
            catch(Exception ex)
            {
                Fail($"The config file could not be parsed, or has errors: '{ex.Message}'");
            }
            
            if (config.texts.Length == 0 && !PromptForText)
            {
                Fail($"At least one text must be defined in the 'texts' array at the config file.");
            }

            if (TextIndex != null && !PromptForText && TextIndex > config.texts.Length-1)
            {
                Fail($"As you have {config.texts.Length} texts defined in the supplied config file, the index, if supplied, must be between 0 and {config.texts.Length -1}.");
            }

            TargetFile = TargetFile ?? config.targetFile;

            if (string.IsNullOrWhiteSpace(TargetFile))
            {
                Fail($"The target file was not set neither at the config file nor with the --targetFile option.");
            }

            if (!File.Exists(TargetFile))
            {
                try
                {
                    File.WriteAllText(TargetFile, "");
                }
                catch (Exception ex)
                {
                    Fail($"The target file '{TargetFile}' does not exist, and cannot be created: {ex.Message}");
                }            
            }

            Dictionary<int, string> sequenceValues = new Dictionary<int, string>();
            if (SequenceValues != null && SequenceValues.Any())
            {
                foreach (string sequenceValue in SequenceValues)
                {
                    if (!sequenceValue.Contains(','))
                    {
                        Fail("Sequence values must be specified in the {sequenceIndex},{sequenceValue}, ex: '1,lorem'. If you need to place a comma in the sequence text, use '\\,' to escape it.");
                    }
                    string[] pair = sequenceValue.Replace("\\,", "##COMMA##").Split(',').Select(p => p.Replace("##COMMA##", ",")).ToArray();
                    if (pair.Length != 2)
                    {
                        Fail("Sequence values must be specified in the {sequenceIndex},{sequenceValue}, ex: '1,lorem'. If you need to place a comma in the sequence text, use '\\,' to escape it.");
                    }
                    if (!int.TryParse(pair[0], out int sequenceIndex) || sequenceIndex >= config.sequences.Length)
                    {
                        Fail($"Sequence index must be numeric, and as you declared {config.sequences.Length} sequence sequences, the index must be between 0 and {config.sequences.Length-1}.");
                    }
                    sequenceValues.Add(sequenceIndex, pair[1]);
                }
            }

            if (config.nextTextIndex == null)
            {
                config.nextTextIndex = 0;
            }

            do
            {
                string text = null;
                if (PromptForText)
                {
                    Console.Write("Enter your text: ");
                    text = Console.ReadLine();
                }
                else
                {
                    text = config.texts[TextIndex ?? config.nextTextIndex ?? 0];
                }

                if (config.sequencePositions == null)
                {
                    config.sequencePositions = new int[config.texts.Length];
                }

                int sequencesLength = config.sequences.Length;
                int sequencePositionsLength = config.sequencePositions.Length;
                if (sequencesLength > 0 && Regex.IsMatch(text, @"#SEQUENCE_\d{2}#"))
                {
                    if (sequencesLength != sequencePositionsLength)
                    {
                        config.sequencePositions = new int[config.texts.Length];
                    }

                    for (int i = 0; i < sequencesLength; i++)
                    {
                        string sequenceKey = $"#SEQUENCE_{i:00}#";
                        if (text.Contains(sequenceKey))
                        {
                            string sequenceValue = sequenceValues.ContainsKey(i) ? sequenceValues[i] : null;
                            text = text.Replace(sequenceKey, sequenceValue ?? config.sequences[i][config.sequencePositions[i]]);
                            if (sequenceValue == null)
                            {
                                config.sequencePositions[i]++;
                                if (config.sequencePositions[i] == config.sequences[i].Length)
                                {
                                    config.sequencePositions[i] = 0;
                                }
                            }
                        }
                    }
                }

                DateTime currentTime = CurrentTime ?? DateTime.Now;

                if (text.Contains("#TIME#"))
                {
                    text = text.Replace($"#TIME#", currentTime.ToString("HH:mm"));
                }

                if (text.Contains("#NTIME#"))
                {
                    text = text.Replace($"#NTIME#", RoundToNearest(currentTime, TimeSpan.FromMinutes(5)).ToString("HH:mm"));
                }

                if (Regex.IsMatch(text, @"#TIME\+\d{1,2}#"))
                {
                    for (int i = 1; i <= 99; i++)
                    {
                        text = text.Replace($"#TIME+{i}#", currentTime.AddMinutes(i).ToString("HH:mm"));
                        text = text.Replace($"#TIME+{i:00}#", currentTime.AddMinutes(i).ToString("HH:mm"));
                    }
                }

                if (Regex.IsMatch(text, @"#NTIME\+\d{1,2}#"))
                {
                    for (int i = 1; i <= 99; i++)
                    {
                        text = text.Replace($"#NTIME+{i}#", RoundToNearest(currentTime.AddMinutes(i), TimeSpan.FromMinutes(5)).ToString("HH:mm"));
                        text = text.Replace($"#NTIME+{i:00}#", RoundToNearest(currentTime.AddMinutes(i), TimeSpan.FromMinutes(5)).ToString("HH:mm"));
                    }
                }

                if (Regex.IsMatch(text, @"#TIME\-\d{1,2}#"))
                {
                    for (int i = 1; i <= 99; i++)
                    {
                        text = text.Replace($"#TIME-{i}#", currentTime.AddMinutes(-i).ToString("HH:mm"));
                        text = text.Replace($"#TIME-{i:00}#", currentTime.AddMinutes(-i).ToString("HH:mm"));
                    }
                }

                if (Regex.IsMatch(text, @"#NTIME\-\d{1,2}#"))
                {
                    for (int i = 1; i <= 99; i++)
                    {
                        text = text.Replace($"#NTIME-{i}#", RoundToNearest(currentTime.AddMinutes(-i), TimeSpan.FromMinutes(5)).ToString("HH:mm"));
                        text = text.Replace($"#NTIME-{i:00}#", RoundToNearest(currentTime.AddMinutes(-i), TimeSpan.FromMinutes(5)).ToString("HH:mm"));
                    }
                }

                if (text.Contains("#TIME12#"))
                {
                    text = text.Replace($"#TIME12#", currentTime.ToString("hh:mmtt"));
                }

                if (text.Contains("#NTIME12#"))
                {
                    text = text.Replace($"#NTIME12#", RoundToNearest(currentTime, TimeSpan.FromMinutes(5)).ToString("hh:mmtt"));
                }

                if (Regex.IsMatch(text, @"#TIME12\+\d{1,2}#"))
                {
                    for (int i = 1; i <= 99; i++)
                    {
                        text = text.Replace($"#TIME12+{i}#", currentTime.AddMinutes(i).ToString("hh:mmtt"));
                        text = text.Replace($"#TIME12+{i:00}#", currentTime.AddMinutes(i).ToString("hh:mmtt"));
                    }
                }

                if (Regex.IsMatch(text, @"#NTIME12\+\d{1,2}#"))
                {
                    for (int i = 1; i <= 99; i++)
                    {
                        text = text.Replace($"#NTIME12+{i}#", RoundToNearest(currentTime.AddMinutes(i), TimeSpan.FromMinutes(5)).ToString("hh:mmtt"));
                        text = text.Replace($"#NTIME12+{i:00}#", RoundToNearest(currentTime.AddMinutes(i), TimeSpan.FromMinutes(5)).ToString("hh:mmtt"));
                    }
                }

                if (Regex.IsMatch(text, @"#TIME12\-\d{1,2}#"))
                {
                    for (int i = 1; i <= 99; i++)
                    {
                        text = text.Replace($"#TIME12-{i}#", currentTime.AddMinutes(-i).ToString("hh:mmtt"));
                        text = text.Replace($"#TIME12-{i:00}#", currentTime.AddMinutes(-i).ToString("hh:mmtt"));
                    }
                }

                if (Regex.IsMatch(text, @"#NTIME12\-\d{1,2}#"))
                {
                    for (int i = 1; i <= 99; i++)
                    {
                        text = text.Replace($"#NTIME12-{i}#", RoundToNearest(currentTime.AddMinutes(-i), TimeSpan.FromMinutes(5)).ToString("hh:mmtt"));
                        text = text.Replace($"#NTIME12-{i:00}#", RoundToNearest(currentTime.AddMinutes(-i), TimeSpan.FromMinutes(5)).ToString("hh:mmtt"));
                    }
                }

                File.WriteAllText(TargetFile, text, System.Text.Encoding.UTF8);

                if (!PromptForText && TextIndex == null)
                {
                    config.nextTextIndex++;
                    if (config.nextTextIndex == config.texts.Length)
                    {
                        config.nextTextIndex = 0;
                    }
                }

                File.WriteAllBytes(ConfigFile, JsonSerializer.SerializeToUtf8Bytes(config, options: new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

                Succeed($"The text '{text}' was written to '{TargetFile}'", false); 
            } while (WaitNextCycle(CycleInterval));
        }
    }
}
