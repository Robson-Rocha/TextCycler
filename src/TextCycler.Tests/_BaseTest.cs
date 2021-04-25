using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace TextCycler.Tests
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseTest
    {
        public const string configFile = "test.json";
        public const string targetFile = "targetfile.txt";

        public void CreateConfig(string configFile = configFile)
        {
            DeleteConfig(configFile);
            DeleteTarget(targetFile);
            var p_arrange = new Program
            {
                ConfigFile = configFile,
                GenerateConfigFile = true
            };
            p_arrange.TryGenerateConfigFile();
        }

        public void UpdateConfig(Action<Config> configAction, string configFile = configFile)
        {
            Config config = Config.Load(configFile);
            configAction(config);
            config.Save();
        }

        private void DeleteFile(string file)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        public void DeleteTarget(string targetFile = targetFile)
        {
            DeleteFile(targetFile);
        }

        public void DeleteConfig(string configFile = configFile)
        {
            DeleteFile(configFile);
        }
    }
}
