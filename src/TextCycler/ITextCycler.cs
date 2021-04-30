using System;
using System.Collections.Generic;

namespace TextCycler
{
    public interface ITextCycler
    {
        bool AskForVariables { get; set; }
        bool ClearTargetFile { get; set; }
        string ConfigFile { get; set; }
        DateTime CurrentTime { get; set; }
        int? CycleInterval { get; set; }
        int? Delay { get; set; }
        bool GenerateConfigFile { get; set; }
        DateTime? InitialTime { get; set; }
        bool Menu { get; set; }
        bool PromptForText { get; set; }
        string[] SequenceValues { get; set; }
        string TargetFile { get; set; }
        int? TextIndex { get; set; }
        string[] Variables { get; set; }
        List<string> VariablesValues { get; set; }

        void OnExecute();
        void ParseCOUNTDOWN();
        void ParseInitialText();
        void ParseNTIME();
        void ParseNTIME12();
        void ParseSEQUENCE();
        void ParseTIME();
        void ParseTIME12();
        void ParseVariables();
        bool TryGenerateConfigFile();
        void TryLoadConfigFile();
        void TryParseSequenceValues();
        void TrySetTargetFile();
        void UpdateConfigFile();
        void UpdateTargetFile();
        void ValidateOptions();
        void ValidateOptionsWhenNotPromptedForText();
        bool WaitNextCycle();
    }
}