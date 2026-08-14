using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// The audio hub. Sfx.Play("mech.open") from anywhere: layered
    /// clips with pitch jitter when the library has them; silence — but
    /// tracked, see the DebugHud line — when it does not. Sound is half
    /// of every beat, so the wiring exists before a single clip does.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        const int PoolSize = 8;

        [SerializeField] SfxLibrary library;

        static Sfx _instance;
        AudioSource[] _pool;
        int _next;

        /// <summary>The most recent key requested — proves the wiring
        /// fires even while every slot is empty.</summary>
        public static string LastKey { get; private set; } = "";

        void Awake()
        {
            _instance = this;
            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D in grey-box; 3D when the room is real
                _pool[i] = src;
            }
        }

        public static void Play(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            LastKey = key;
            if (_instance == null || _instance.library == null) return;
            var e = _instance.library.Find(key);
            if (e == null || e.layers == null) return;
            foreach (var clip in e.layers)
            {
                if (clip == null) continue;
                var src = _instance._pool[_instance._next];
                _instance._next = (_instance._next + 1) % PoolSize;
                src.pitch = 1f + Random.Range(-e.pitchJitter, e.pitchJitter);
                src.PlayOneShot(clip, e.volume);
            }
        }
    }
}
