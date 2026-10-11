using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityLearningCompanion
{
    public sealed class TypeDeclaration
    {
        public TypeDeclaration(string name, string kind, IReadOnlyList<string> baseTypes, int line)
        {
            Name = name;
            Kind = kind;
            BaseTypes = baseTypes;
            Line = line;
        }

        public string Name { get; }
        public string Kind { get; }
        public IReadOnlyList<string> BaseTypes { get; }
        public int Line { get; }
    }

    /// Line-based scan of C# text. It finds usings, namespaces and type declarations. It does not parse C#,
    /// so generics, nested types and preprocessor branches can be misread. Those results are marked inferred.
    public static class CSharpScanner
    {
        private static readonly Regex UsingLine = new Regex(@"^\s*using\s+([A-Za-z_][\w.]*)\s*;");
        private static readonly Regex NamespaceLine = new Regex(@"^\s*namespace\s+([A-Za-z_][\w.]*)");
        private static readonly Regex TypeLine = new Regex(
            @"^\s*(?:public\s+|internal\s+|private\s+|protected\s+)?(?:sealed\s+|abstract\s+|static\s+|partial\s+|readonly\s+)*(class|struct|interface|enum)\s+([A-Za-z_]\w*)\s*(?::\s*([^{]+))?");

        public static List<string> FindUsings(string text, out List<int> lines)
        {
            var usings = new List<string>();
            lines = new List<int>();
            string[] all = text.Split('\n');
            for (int i = 0; i < all.Length; i++)
            {
                Match m = UsingLine.Match(all[i]);
                if (m.Success)
                {
                    usings.Add(m.Groups[1].Value);
                    lines.Add(i + 1);
                }
            }

            return usings;
        }

        public static List<string> FindNamespaces(string text)
        {
            var result = new List<string>();
            foreach (string line in text.Split('\n'))
            {
                Match m = NamespaceLine.Match(line);
                if (m.Success)
                {
                    result.Add(m.Groups[1].Value);
                }
            }

            return result;
        }

        public static List<TypeDeclaration> FindTypes(string text)
        {
            var result = new List<TypeDeclaration>();
            string[] all = text.Split('\n');
            for (int i = 0; i < all.Length; i++)
            {
                Match m = TypeLine.Match(all[i]);
                if (!m.Success)
                {
                    continue;
                }

                var bases = new List<string>();
                if (m.Groups[3].Success)
                {
                    foreach (string part in m.Groups[3].Value.Split(','))
                    {
                        string trimmed = part.Trim();
                        if (trimmed.Length > 0)
                        {
                            bases.Add(trimmed);
                        }
                    }
                }

                result.Add(new TypeDeclaration(m.Groups[2].Value, m.Groups[1].Value, bases, i + 1));
            }

            return result;
        }
    }
}
