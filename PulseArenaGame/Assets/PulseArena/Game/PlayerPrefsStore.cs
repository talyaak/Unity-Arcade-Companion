using PulseArena.Core;
using UnityEngine;

namespace PulseArena.Game
{
    /// Device-local storage for leaderboard and profile data. PlayerPrefs is not secure and not shared between devices.
    /// Its contents can be edited by the player, so nothing stored here is trusted for competitive results.
    public sealed class PlayerPrefsStore : IKeyValueStore
    {
        public string Get(string key)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        }

        public void Set(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }
    }
}
