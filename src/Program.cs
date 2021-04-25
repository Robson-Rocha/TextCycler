using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace TextCycler
{
    public class Program
    {
        #region Public Properties for Command Options
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

        [Option(CommandOptionType.MultipleValue, ShortName = "v", LongName = "variable", ShowInHelpText = true,
                ValueName = "{VariableName}[:{DefaultValue}]", Description = "Asks for the value of a variable, which can be used in a replacement token in the format #VARIABLE_NAME#. Optionally, you can provide a default value to the variable, which will be displayed at prompt, using a colon after the variable name, and providing the value after that colon.")]
        public string[] Variables { get; set; }

        [Option(CommandOptionType.MultipleValue, ShortName = "vv", LongName = "variableValue", ShowInHelpText = true,
                ValueName = "{VariableName}:{Value}", Description = "Sets the value of a variable, which can be used in a replacement token in the format #VARIABLE_NAME#. You must provide the value for the variable using a colon after the variable name, and providing the value after that colon.")]
        public string[] VariablesValues { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "d", LongName = "delay", ShowInHelpText = true,
                ValueName = "Delay to be waited for after setting the target file text, in seconds", Description = "If defined, waits for the specified number of seconds after setting the target file. Useful for letting the result message be read.")]
        public int? Delay { get; set; }

        [Option(CommandOptionType.NoValue, ShortName = "m", LongName = "menu", ShowInHelpText = true,
                Description = "Displays a menu for selecting the desired text.")]
        public bool Menu { get; set; }
        #endregion

        #region Private Properties
        private Config CurrentConfig { get; set; }

        private string Text { get; set; }

        private Dictionary<int, string> ParsedSequenceValues { get; set; }
        #endregion

        #region Private Static Methods
        private static DateTime RoundToNearest(DateTime dt, TimeSpan d)
        {
            long delta = dt.Ticks % d.Ticks;
            bool roundUp = delta > d.Ticks / 2;
            long offset = roundUp ? d.Ticks : 0;

            return new DateTime(dt.Ticks + offset - delta, dt.Kind);
        }
        #endregion

        #region Private Methods
        private void Fail(string message)
        {
            ConsoleColor currentConsoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = currentConsoleColor;
            if (Delay != null)
            {
                Thread.Sleep(Delay.Value * 1000);
            }
            Environment.Exit(-1);
        }

        private void Succeed(string message, bool exitImmediately = true)
        {
            ConsoleColor currentConsoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(message);
            Console.ForegroundColor = currentConsoleColor;
            if (Delay != null && exitImmediately)
            {
                Thread.Sleep(Delay.Value * 1000);
            }
            if (exitImmediately)
            {
                Environment.Exit(0);
            }
        }

        private void TryGenerateConfigFile()
        {
            if (!File.Exists(ConfigFile))
            {
                if (GenerateConfigFile)
                {
                    Config blankConfig = new Config
                    {
                        NextTextIndex = 0,
                        TargetFile = "targetfile.txt",
                        Texts = new[]
                        {
                            "First text",
                            "Second text, with Current Time (#TIME#), and Rounded to Nearest 5 Minutes Time (#NTIME#)",
                            "Third Text, with the sequence 0 (#SEQUENCE_00#) from 01 to 05",
                            "Fourth Text, with the sequence 1 (#SEQUENCE_01#) using text values"
                        },
                        Sequences = new[]
                        {
                            new [] { "01", "02", "03", "04", "05" },
                            new [] { "lorem", "ipsum", "dolor" }
                        },
                        SequencePositions = new[] { 0, 0 },
                        LastTextIndexUsedInMenu = 0,
                        LastWrittenText = "",
                        ConfigPath = ConfigFile
                    };
                    blankConfig.Save();
                    Succeed($"A sample config file was generated at '{ConfigFile}'.");
                }
                Fail($"The config file {ConfigFile} does not exists or is inaccessible.");
            }
        }

        private void TryLoadConfigFile()
        {
            try
            {
                CurrentConfig = Config.Load(ConfigFile);
                if (CurrentConfig.NextTextIndex == null)
                {
                    CurrentConfig.NextTextIndex = 0;
                }
                if (CurrentConfig.SequencePositions == null)
                {
                    CurrentConfig.SequencePositions = new int[CurrentConfig.Texts.Length];
                }
            }
            catch (Exception ex)
            {
                Fail($"The config file could not be parsed, or has errors: '{ex.Message}'");
            }
        }

        private void ValidateOptions()
        {
            if (!PromptForText)
            {
                if (CurrentConfig.Texts.Length == 0)
                {
                    Fail($"At least one text must be defined in the 'texts' array at the config file if the --prompt option is not supplied.");
                }

                if (TextIndex != null && TextIndex > CurrentConfig.Texts.Length - 1)
                {
                    Fail($"As you have {CurrentConfig.Texts.Length} texts defined in the supplied config file, the index, if supplied, must be between 0 and {CurrentConfig.Texts.Length - 1}.");
                }

                if (Menu && CycleInterval != null)
                {
                    Fail("You can't use the '-m' and '-a' options at the same time.");
                }
            }
            else
            {
                if (CycleInterval != null)
                {
                    Fail("You can't use the '-p' and '-a' options at the same time.");
                }

                if (Menu)
                {
                    Fail("You can't use the '-p' and '-m' options at the same time.");
                }
            }
        }

        private void TryParseSequenceValues()
        {
            ParsedSequenceValues = new Dictionary<int, string>();
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
                    if (!int.TryParse(pair[0], out int sequenceIndex) || sequenceIndex >= CurrentConfig.Sequences.Length)
                    {
                        Fail($"Sequence index must be numeric, and as you declared {CurrentConfig.Sequences.Length} sequence sequences, the index must be between 0 and {CurrentConfig.Sequences.Length - 1}.");
                    }
                    ParsedSequenceValues.Add(sequenceIndex, pair[1]);
                }
            }
        }

        private void TrySetTargetFile()
        {
            TargetFile ??= CurrentConfig.TargetFile;

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
        }

        private void ParseInitialText()
        {
            if (PromptForText)
            {
                Text = ReadLine2.Read("Enter your text: ", CurrentConfig.LastWrittenText);
            }
            else if (Menu)
            {
                for (int i = 0; i < CurrentConfig.Texts.Length; i++)
                {
                    Console.WriteLine($"[{i}] {CurrentConfig.Texts[i]}");
                }
                Console.WriteLine("[#] [Enter a custom text]");
                ReadLine2.AddHistory(Enumerable.Range(0, CurrentConfig.Texts.Length).Select(i => i.ToString()).ToArray());

                int index = -1;
                int lastUsedTextIndex = (CurrentConfig.LastTextIndexUsedInMenu ?? 0);

                do
                {
                    string enteredIndex = ReadLine2.Read("\r\nEnter the desired text index: ", (TextIndex ?? lastUsedTextIndex).ToString());
                    if (enteredIndex == "#")
                    {
                        Text = ReadLine2.Read("Enter your text: ", CurrentConfig.LastWrittenText);
                        return;
                    }
                    else if (!int.TryParse(enteredIndex, out index) || index < 0 || index >= CurrentConfig.Texts.Length)
                    {
                        Console.WriteLine($"'{enteredIndex}' is not a valid index. Enter a value between 0 and {CurrentConfig.Texts.Length - 1}.");
                        index = -1;
                    }
                } while (index == -1);
                CurrentConfig.LastTextIndexUsedInMenu = index;
                Text = CurrentConfig.Texts[index];
            }
            else
            {
                Text = CurrentConfig.Texts[TextIndex ?? CurrentConfig.NextTextIndex ?? 0];
            }
        }

        private void ParseVariables()
        {
            (string variableName, string defaultValue) SplitVariable(string v)
            {
                v = v.Replace("\\:", "##COLON##");
                string[] parts = v.Split(':');
                string name = parts[0].Replace("##COLON##", ":");
                string @default = parts.Length > 1 ? parts[1].Replace("##COLON##", ":") : "";
                return (name, @default);
            }

            if (Variables != null)
            {
                foreach ((string variableName, string defaultValue) in Variables.Select(SplitVariable))
                {
                    string variableValue = ReadLine2.Read($"Enter the value for #{variableName}#: ", defaultValue);
                    Text = Text.Replace($"#{variableName}#", variableValue);
                }
            }

            if (VariablesValues != null)
            {
                foreach ((string variableName, string variableValue) in VariablesValues.Select(SplitVariable))
                {
                    if (string.IsNullOrEmpty(variableName))
                    {
                        Fail($"No value was provided for the variable '{variableName}'");
                    }
                    Text = Text.Replace($"#{variableName}#", variableValue);
                }
            }
        }

        private void ParseSequences()
        {
            int sequencesLength = CurrentConfig.Sequences.Length;
            int sequencePositionsLength = CurrentConfig.SequencePositions.Length;
            if (sequencesLength > 0 && Regex.IsMatch(Text, @"#SEQUENCE_\d{2}#"))
            {
                if (sequencesLength != sequencePositionsLength)
                {
                    CurrentConfig.SequencePositions = new int[CurrentConfig.Texts.Length];
                }

                for (int i = 0; i < sequencesLength; i++)
                {
                    string sequenceKey = $"#SEQUENCE_{i:00}#";
                    if (Text.Contains(sequenceKey))
                    {
                        string sequenceValue = ParsedSequenceValues.ContainsKey(i) ? ParsedSequenceValues[i] : null;
                        Text = Text.Replace(sequenceKey, sequenceValue ?? CurrentConfig.Sequences[i][CurrentConfig.SequencePositions[i]]);
                        if (sequenceValue == null)
                        {
                            CurrentConfig.SequencePositions[i]++;
                            if (CurrentConfig.SequencePositions[i] == CurrentConfig.Sequences[i].Length)
                            {
                                CurrentConfig.SequencePositions[i] = 0;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<(string, int)> GetMatches(string pattern)
        {
            foreach (Match match in Regex.Matches(Text, pattern))
            {
                string matchedText = match.Groups[1].Value;
                int matchedValue = int.Parse(matchedText);
                yield return (matchedText, matchedValue);
            }
        }

        private void ParseTIME()
        {
            DateTime currentTime = CurrentTime.Value;
            if (Text.Contains("#TIME#"))
            {
                Text = Text.Replace("#TIME#", currentTime.ToString("HH:mm"));
            }

            foreach ((string matchedText, int matchedValue) in GetMatches(@"#TIME\+(\d+)#"))
            {
                Text = Text.Replace($"#TIME+{matchedText}#", currentTime.AddMinutes(matchedValue).ToString("HH:mm"));
            }

            foreach ((string matchedText, int matchedValue) in GetMatches(@"#TIME\-(\d+)#"))
            {
                Text = Text.Replace($"#TIME-{matchedText}#", currentTime.AddMinutes(-matchedValue).ToString("HH:mm"));
            }
        }

        private void ParseNTIME()
        {
            DateTime currentTime = CurrentTime.Value;
            if (Text.Contains("#NTIME#"))
            {
                Text = Text.Replace("#NTIME#", RoundToNearest(currentTime, TimeSpan.FromMinutes(5)).ToString("HH:mm"));
            }

            foreach ((string matchedText, int matchedValue) in GetMatches(@"#NTIME\+(\d+)#"))
            {
                Text = Text.Replace($"#NTIME+{matchedText}#", RoundToNearest(currentTime.AddMinutes(matchedValue), TimeSpan.FromMinutes(5)).ToString("HH:mm"));
            }

            foreach ((string matchedText, int matchedValue) in GetMatches(@"#NTIME\-(\d+)#"))
            {
                Text = Text.Replace($"#NTIME-{matchedText}#", RoundToNearest(currentTime.AddMinutes(-matchedValue), TimeSpan.FromMinutes(5)).ToString("HH:mm"));
            }
        }

        private void ParseTIME12()
        {
            DateTime currentTime = CurrentTime.Value;

            if (Text.Contains("#TIME12#"))
            {
                Text = Text.Replace($"#TIME12#", currentTime.ToString("hh:mmtt"));
            }

            foreach ((string matchedText, int matchedValue) in GetMatches(@"#TIME12\+(\d+)#"))
            {
                Text = Text.Replace($"#TIME12+{matchedText}#", currentTime.AddMinutes(matchedValue).ToString("hh:mmtt"));
            }

            foreach ((string matchedText, int matchedValue) in GetMatches(@"#TIME12\-(\d+)#"))
            {
                Text = Text.Replace($"#TIME12-{matchedText}#", currentTime.AddMinutes(-matchedValue).ToString("hh:mmtt"));
            }
        }

        private void ParseNTIME12()
        {
            DateTime currentTime = CurrentTime.Value;

            if (Text.Contains("#NTIME12#"))
            {
                Text = Text.Replace($"#NTIME12#", RoundToNearest(currentTime, TimeSpan.FromMinutes(5)).ToString("hh:mmtt"));
            }

            foreach ((string matchedText, int matchedValue) in GetMatches(@"#NTIME12\+(\d+)#"))
            {
                Text = Text.Replace($"#NTIME12+{matchedText}#", RoundToNearest(currentTime.AddMinutes(matchedValue), TimeSpan.FromMinutes(5)).ToString("hh:mmtt"));
            }

            foreach ((string matchedText, int matchedValue) in GetMatches(@"#NTIME12\-(\d+)#"))
            {
                Text = Text.Replace($"#NTIME12-{matchedText}#", RoundToNearest(currentTime.AddMinutes(-matchedValue), TimeSpan.FromMinutes(5)).ToString("hh:mmtt"));
            }
        }

        private void UpdateTargetFile()
        {
            File.WriteAllText(TargetFile, Text, System.Text.Encoding.UTF8);
            Succeed($"The text '{Text}' was written to '{TargetFile}'", false);
        }

        private void UpdateConfigFile()
        {
            if (!PromptForText && TextIndex == null && !Menu)
            {
                CurrentConfig.NextTextIndex++;
                if (CurrentConfig.NextTextIndex == CurrentConfig.Texts.Length)
                {
                    CurrentConfig.NextTextIndex = 0;
                }
            }

            CurrentConfig.LastWrittenText = Text;

            File.WriteAllBytes(ConfigFile, JsonSerializer.SerializeToUtf8Bytes(CurrentConfig, options: new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        }

        private bool WaitNextCycle()
        {
            if (CycleInterval == null)
            {
                return false;
            }

            Thread.Sleep(CycleInterval.Value * 1000);
            CurrentTime = DateTime.Now;
            return true;
        }
        #endregion

        #region Public Methods
        public static void Main(string[] args)
        {
            CommandLineApplication.Execute<Program>(args);
        }

        public void OnExecute()
        {
            try
            {
                TryGenerateConfigFile();

                TryLoadConfigFile();

                ValidateOptions();

                TrySetTargetFile();

                TryParseSequenceValues();

                CurrentTime ??= DateTime.Now;

                do
                {
                    ParseInitialText();

                    ParseVariables();

                    ParseSequences();

                    ParseTIME();

                    ParseNTIME();

                    ParseTIME12();

                    ParseNTIME12();

                    UpdateTargetFile();

                    UpdateConfigFile();

                } while (WaitNextCycle());

            }
            catch (Exception ex)
            {
                string errorFileName = Path.GetTempFileName();
                File.WriteAllText(errorFileName, ex.ToString());
                Fail($"Oops. Something went wrong.\r\nThe details of the error were written to '{errorFileName}'");
            }
        }
        #endregion    
    }
}
