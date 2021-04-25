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

        [JsonPropertyName("nextTextIndex")]
        public int? NextTextIndex { get; set; }

        [JsonPropertyName("texts")]
        public string[] Texts { get; set; }

        [JsonPropertyName("sequences")]
        public string[][] Sequences { get; set; }

        [JsonPropertyName("sequencePositions")]
        public int[] SequencePositions { get; set; }

        [JsonPropertyName("targetFile")]
        public string TargetFile { get; set; }

        [JsonPropertyName("lastWrittenText")]
        public string LastWrittenText { get; set; }

        [JsonPropertyName("lastTextIndexUsedInMenu")]
        public int? LastTextIndexUsedInMenu { get; set; }

        [JsonIgnore]
        public string ConfigPath { get; set; }

        public void Save()
        {
            File.WriteAllBytes(ConfigPath, JsonSerializer.SerializeToUtf8Bytes(this, options: new JsonSerializerOptions { WriteIndented = true, IgnoreNullValues = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        }
    }
}
