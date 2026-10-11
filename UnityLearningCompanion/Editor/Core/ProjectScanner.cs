using System.Collections.Generic;
using System.Linq;

namespace UnityLearningCompanion
{
    public sealed class ScanReport
    {
        public readonly List<Fact> Facts = new List<Fact>();

        /// Problems found by checking the project's own rules. Each is a claim the user can verify at its Source.
        public readonly List<Fact> Warnings = new List<Fact>();

        public IEnumerable<Fact> VerifiedFacts => Facts.Where(f => f.Confidence == Confidence.Verified);
        public IEnumerable<Fact> InferredFacts => Facts.Where(f => f.Confidence == Confidence.Inferred);
    }

    /// Builds a report from source files. Pure function of its input: no file IO, no writes.
    public static class ProjectScanner
    {
        private static readonly string[] UnityBaseTypes = { "MonoBehaviour", "ScriptableObject", "EditorWindow" };

        public static ScanReport Scan(IReadOnlyList<SourceFile> files)
        {
            var report = new ScanReport();
            var asmdefs = new List<(SourceFile file, AsmdefInfo info)>();

            foreach (SourceFile file in files)
            {
                if (file.ProjectPath.EndsWith(".asmdef"))
                {
                    AsmdefInfo info = AsmdefParser.Parse(file.Text);
                    asmdefs.Add((file, info));
                    ReportAsmdef(report, file, info);
                }
            }

            foreach (SourceFile file in files.Where(f => f.ProjectPath.EndsWith(".cs")))
            {
                ScanCSharp(report, file);
            }

            CheckRules(report, files, asmdefs);
            return report;
        }

        private static void ReportAsmdef(ScanReport report, SourceFile file, AsmdefInfo info)
        {
            report.Facts.Add(new Fact(FactKind.Assembly, Confidence.Verified, info.Name,
                info.NoEngineReferences ? "(noEngineReferences)" : string.Empty, file.ProjectPath));

            foreach (string reference in info.References)
            {
                report.Facts.Add(new Fact(FactKind.AssemblyReference, Confidence.Verified, info.Name, "-> " + reference, file.ProjectPath));
            }
        }

        private static void ScanCSharp(ScanReport report, SourceFile file)
        {
            List<string> usings = CSharpScanner.FindUsings(file.Text, out List<int> usingLines);
            for (int i = 0; i < usings.Count; i++)
            {
                report.Facts.Add(new Fact(FactKind.Import, Confidence.Verified, usings[i], string.Empty,
                    $"{file.ProjectPath}:{usingLines[i]}"));
            }

            foreach (string ns in CSharpScanner.FindNamespaces(file.Text))
            {
                report.Facts.Add(new Fact(FactKind.Namespace, Confidence.Verified, ns, string.Empty, file.ProjectPath));
            }

            foreach (TypeDeclaration type in CSharpScanner.FindTypes(file.Text))
            {
                report.Facts.Add(new Fact(FactKind.Type, Confidence.Verified, type.Name, type.Kind,
                    $"{file.ProjectPath}:{type.Line}"));

                foreach (string baseType in type.BaseTypes)
                {
                    // The base name is read from the text, but what it means for Unity is an inference from the name.
                    Confidence confidence = UnityBaseTypes.Contains(baseType) ? Confidence.Inferred : Confidence.Verified;
                    report.Facts.Add(new Fact(FactKind.BaseType, confidence, type.Name, ": " + baseType,
                        $"{file.ProjectPath}:{type.Line}"));
                }
            }
        }

        // Project-rule checks. Each rule is stated so a reader can see exactly what it tests.
        private static void CheckRules(ScanReport report, IReadOnlyList<SourceFile> files,
            List<(SourceFile file, AsmdefInfo info)> asmdefs)
        {
            foreach (SourceFile file in files.Where(f => f.ProjectPath.EndsWith(".cs")))
            {
                AsmdefInfo owner = NearestAsmdef(file, asmdefs);
                if (owner == null)
                {
                    continue;
                }

                List<string> usings = CSharpScanner.FindUsings(file.Text, out List<int> lines);
                for (int i = 0; i < usings.Count; i++)
                {
                    string source = $"{file.ProjectPath}:{lines[i]}";

                    // Rule 1: an assembly declared noEngineReferences must not import UnityEngine.
                    if (owner.NoEngineReferences && (usings[i] == "UnityEngine" || usings[i].StartsWith("UnityEngine.")))
                    {
                        report.Warnings.Add(new Fact(FactKind.Warning, Confidence.Verified, owner.Name,
                            $"imports {usings[i]} but is declared noEngineReferences", source));
                    }

                    // Rule 2: an assembly whose platforms exclude Editor must not import UnityEditor.
                    bool editorOnly = owner.IncludePlatforms.Contains("Editor");
                    if (!editorOnly && (usings[i] == "UnityEditor" || usings[i].StartsWith("UnityEditor.")))
                    {
                        report.Warnings.Add(new Fact(FactKind.Warning, Confidence.Verified, owner.Name,
                            $"imports {usings[i]} but is not Editor-only", source));
                    }
                }
            }
        }

        // The asmdef that governs a file is the one in the nearest ancestor folder. Unity uses the same rule.
        private static AsmdefInfo NearestAsmdef(SourceFile file, List<(SourceFile file, AsmdefInfo info)> asmdefs)
        {
            AsmdefInfo best = null;
            int bestDepth = -1;
            foreach (var (asmdefFile, info) in asmdefs)
            {
                string folder = asmdefFile.Directory;
                bool inside = folder.Length == 0 || file.ProjectPath.StartsWith(folder + "/");
                if (inside && folder.Length > bestDepth)
                {
                    best = info;
                    bestDepth = folder.Length;
                }
            }

            return best;
        }
    }
}
