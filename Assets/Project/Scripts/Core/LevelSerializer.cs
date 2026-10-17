
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace TrayMatch.Core
{
    // Reads and writes level files (format: docs/ARCHITECTURE.md §4). Keys are camelCase and
    // directions are words ("up", "right", "down", "left"). Unknown keys such as "meta" are
    // ignored, so tools can store extra data in level files.
    public static class LevelSerializer
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter(new CamelCaseNamingStrategy()) },
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        public static string ToJson(Level level) =>
            JsonConvert.SerializeObject(level, Settings);

        public static Level FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new FormatException("Level JSON is empty.");

            Level level;
            try
            {
                level = JsonConvert.DeserializeObject<Level>(json, Settings);
            }
            catch (JsonException e)
            {
                throw new FormatException("Invalid level JSON: " + e.Message, e);
            }

            if (level == null)
                throw new FormatException("Level JSON is empty.");

            if (level.Format > Level.CurrentFormat)
                throw new FormatException(
                    $"Level format {level.Format} is newer than this game supports ({Level.CurrentFormat}).");

            return level;
        }
    }
}
