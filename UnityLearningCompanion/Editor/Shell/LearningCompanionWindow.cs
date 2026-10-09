using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityLearningCompanion;

namespace UnityLearningCompanion.Shell
{
    /// Thin Editor UI. It reads the project, runs the Core scanner, and shows results. It writes nothing to the project.
    public sealed class LearningCompanionWindow : EditorWindow
    {
        private ScanReport _report;
        private string _question = "What is an asmdef?";
        private string _answer = string.Empty;

        [MenuItem("Window/Learning Companion")]
        public static void Open()
        {
            GetWindow<LearningCompanionWindow>("Learning Companion");
        }

        private void OnGUI()
        {
            GUILayout.Label("Read-only. Nothing is written to your project.");

            if (GUILayout.Button("Scan project"))
            {
                Scan();
            }

            _question = GUILayout.TextField(_question);
            if (GUILayout.Button("Explain"))
            {
                _answer = new ExplainerRouter(null).Explain(_question, _report ?? new ScanReport(), new PayloadPolicy());
            }

            if (_report != null)
            {
                GUILayout.Label($"Facts: {_report.Facts.Count}  Warnings: {_report.Warnings.Count}");
                foreach (Fact warning in _report.Warnings)
                {
                    GUILayout.Label($"[warning] {warning.Subject}: {warning.Detail} ({warning.Source})");
                }
            }

            GUILayout.Label(_answer);
        }

        private void Scan()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            var files = new List<SourceFile>();
            foreach (string path in Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories))
            {
                if (!path.EndsWith(".cs") && !path.EndsWith(".asmdef"))
                {
                    continue;
                }

                string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                files.Add(new SourceFile(relative, File.ReadAllText(path)));
            }

            // Full rescan on every press. ScanCache can drive incremental refresh later.
            _report = ProjectScanner.Scan(files);
        }
    }
}
