using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TextCycler
{
    public class Config
    {
        public static Config Load(string path)
        {
            Config config = JsonSerializer.Deserialize<Config>(File.ReadAllText(path));
            config.ConfigPath = path;
            return config;
        }
        
        public int? nextTextIndex { get; set; }

        public string[] texts { get; set; }

        public string[][] sequences { get; set; }

        public int[] sequencePositions { get; set; }

        public string targetFile { get; set; }

        [JsonIgnore]
        public string ConfigPath { get; set; }

        public void Save()
        {
            File.WriteAllBytes(ConfigPath, JsonSerializer.SerializeToUtf8Bytes(this, options: new JsonSerializerOptions { WriteIndented = true, IgnoreNullValues = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        }
    }
}
