using System;
using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// The foley library: named events mapped to layered clips.
    /// Grey-box ships the slots empty; land-day audio becomes
    /// drag-and-drop into this asset instead of a wiring pass.
    /// Per MECHANISMS.txt every event wants at least two layers:
    /// a high transient and a low body.
    /// </summary>
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "Stereopsis/Sfx Library")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string key;
            [Tooltip("All layers play together: transient + body.")]
            public AudioClip[] layers = new AudioClip[0];
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Random pitch spread, e.g. 0.03 = ±3%, so repeats never sound mechanical.")]
            public float pitchJitter = 0.03f;
        }

        public Entry[] entries = new Entry[0];

        public Entry Find(string key)
        {
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].key == key) return entries[i];
            return null;
        }
    }
}
