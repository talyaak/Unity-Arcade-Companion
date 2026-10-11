using System;
using System.Collections.Generic;

namespace PulseArena.Core
{
    /// Named on/off switches with compiled-in defaults. Remote values can only override flags that exist in the defaults,
    /// so a typo in a remote payload cannot create a new feature.
    public sealed class FeatureFlags
    {
        private readonly Dictionary<string, bool> _values;
        private readonly DiagnosticsLog _log;

        public FeatureFlags(IReadOnlyDictionary<string, bool> defaults, DiagnosticsLog log)
        {
            if (defaults == null)
            {
                throw new ArgumentNullException(nameof(defaults));
            }

            _values = new Dictionary<string, bool>(defaults, StringComparer.Ordinal);
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// Unknown flags are false, so a missing flag fails closed.
        public bool IsEnabled(string name)
        {
            return _values.TryGetValue(name, out bool on) && on;
        }

        /// Applies overrides for known flags only. Returns the names that were ignored.
        public IReadOnlyList<string> ApplyOverrides(IReadOnlyDictionary<string, bool> overrides)
        {
            var ignored = new List<string>();
            if (overrides == null)
            {
                return ignored;
            }

            foreach (KeyValuePair<string, bool> pair in overrides)
            {
                if (_values.ContainsKey(pair.Key))
                {
                    _values[pair.Key] = pair.Value;
                }
                else
                {
                    ignored.Add(pair.Key);
                }
            }

            if (ignored.Count > 0)
            {
                _log.Record(DiagnosticLevel.Warning, "flags.unknown_ignored", string.Join(",", ignored));
            }

            return ignored;
        }
    }
}
