using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityLearningCompanion
{
    public sealed class AsmdefInfo
    {
        public string Name = string.Empty;
        public List<string> References = new List<string>();
        public List<string> IncludePlatforms = new List<string>();
        public bool NoEngineReferences;
    }

    /// Reads the fields this tool needs from an asmdef. It is a regex reader, not a JSON parser, so it handles the
    /// flat shape Unity writes. Anything it cannot read is left empty and must not be reported as a verified fact.
    public static class AsmdefParser
    {
        private static readonly Regex NameField = new Regex("\"name\"\\s*:\\s*\"([^\"]+)\"");
        private static readonly Regex NoEngineField = new Regex("\"noEngineReferences\"\\s*:\\s*true");
        private static readonly Regex ArrayField = new Regex("\"{0}\"\\s*:\\s*\\[([^\\]]*)\\]");
        private static readonly Regex StringItem = new Regex("\"([^\"]*)\"");

        public static AsmdefInfo Parse(string text)
        {
            var info = new AsmdefInfo();
            Match name = NameField.Match(text);
            if (name.Success)
            {
                info.Name = name.Groups[1].Value;
            }

            info.References = ReadArray(text, "references");
            info.IncludePlatforms = ReadArray(text, "includePlatforms");
            info.NoEngineReferences = NoEngineField.IsMatch(text);
            return info;
        }

        private static List<string> ReadArray(string text, string field)
        {
            var items = new List<string>();
            Match array = new Regex(string.Format(ArrayField.ToString(), field)).Match(text);
            if (!array.Success)
            {
                return items;
            }

            foreach (Match item in StringItem.Matches(array.Groups[1].Value))
            {
                items.Add(item.Groups[1].Value);
            }

            return items;
        }
    }
}
