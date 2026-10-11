using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityLearningCompanion;

namespace UnityLearningCompanion.Tests
{
    // dotnet-only: these tests read the Pulse Arena project from disk relative to the test binary.
    // They live here, not under Tests/Editor, so Unity never compiles them and never skips them.
    public class PulseArenaFixtureTests
    {
        // Finds the Pulse Arena project next to this repo, by walking up from the test binary.
        private static string PulseArenaRoot()
        {
            string dir = AppContext.BaseDirectory;
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "PulseArenaGame", "Assets");
                if (Directory.Exists(candidate))
                {
                    return Path.Combine(dir, "PulseArenaGame");
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            return null;
        }

        private static List<SourceFile> LoadProject(string root)
        {
            var files = new List<SourceFile>();
            foreach (string path in Directory.GetFiles(Path.Combine(root, "Assets"), "*", SearchOption.AllDirectories)
                         .Where(p => p.EndsWith(".cs") || p.EndsWith(".asmdef")))
            {
                string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                files.Add(new SourceFile(relative, File.ReadAllText(path)));
            }

            return files;
        }

        // ---- Verified facts against the real Pulse Arena project ----

        [Test]
        public void PulseArenaCore_AsmdefFields_AreReadCorrectly()
        {
            string root = PulseArenaRoot();
            if (root == null) Assert.Ignore("Pulse Arena project not found next to this repo.");

            string text = System.IO.File.ReadAllText(Path.Combine(root, "Assets/PulseArena/Core/PulseArena.Core.asmdef"));
            AsmdefInfo info = AsmdefParser.Parse(text);

            Assert.AreEqual("PulseArena.Core", info.Name);
            Assert.IsTrue(info.NoEngineReferences);
            Assert.IsEmpty(info.References);
        }

        [Test]
        public void PulseArenaProject_HasNoRuleWarnings_AndFindsGameMonoBehaviourAsInferred()
        {
            string root = PulseArenaRoot();
            if (root == null) Assert.Ignore("Pulse Arena project not found next to this repo.");

            ScanReport report = ProjectScanner.Scan(LoadProject(root));

            Assert.IsEmpty(report.Warnings, "the current Pulse Arena layout should break no rule");
            Assert.IsTrue(report.Facts.Any(f => f.Kind == FactKind.Assembly && f.Subject == "PulseArena.Core" && f.Confidence == Confidence.Verified));
            Assert.IsTrue(report.Facts.Any(f => f.Kind == FactKind.BaseType && f.Subject == "ChallengeController"
                                              && f.Detail.Contains("MonoBehaviour") && f.Confidence == Confidence.Inferred));
        }

    }
}
