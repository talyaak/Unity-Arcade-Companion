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
        private string _interviewAnswer = string.Empty;
        private string _interviewResult = string.Empty;
        private int _questionIndex;
        private bool _showMissions;

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

            if (GUILayout.Button(_showMissions ? "Hide missions" : "Show missions"))
            {
                _showMissions = !_showMissions;
            }

            if (_showMissions && _report != null)
            {
                foreach (Mission mission in MissionGenerator.From(_report))
                {
                    GUILayout.Label($"- {mission.Prompt}");
                }
            }

            DrawInterview();
        }

        private void DrawInterview()
        {
            Question question = Interview.Questions[_questionIndex];
            GUILayout.Label("Interview practice: " + question.Prompt);
            _interviewAnswer = GUILayout.TextField(_interviewAnswer);

            if (GUILayout.Button("Check my answer"))
            {
                InterviewScore score = Interview.Score(question, _interviewAnswer);
                _interviewResult = $"Covered {score.Covered.Count}/{question.Rubric.Length}. Not mentioned: " +
                                   (score.Missing.Count == 0 ? "none" : string.Join(", ", score.Missing));
            }

            GUILayout.Label(_interviewResult);

            if (GUILayout.Button("Next question"))
            {
                _questionIndex = (_questionIndex + 1) % Interview.Questions.Count;
                _interviewAnswer = string.Empty;
                _interviewResult = string.Empty;
            }
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
