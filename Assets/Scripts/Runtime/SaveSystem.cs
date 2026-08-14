using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stereopsis.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Stereopsis
{
    /// <summary>
    /// Save and restore the whole game. Logical state (era, cards, bag,
    /// flags, device) is replayed through the public APIs so events fire
    /// and the room follows; physical state (what is open, what is
    /// visible, where the levered board sits) is restored directly.
    ///
    /// Identity is the scene path under Room, which is stable because
    /// this game is one room forever.
    ///
    /// Dev keys: F5 save, F9 load. Autosaves on every era change.
    /// Never loads automatically — a fresh Play is always a fresh game.
    /// </summary>
    [RequireComponent(typeof(EraDirector))]
    public sealed class SaveSystem : MonoBehaviour
    {
        [System.Serializable]
        class SeqState
        {
            public string path;
            public int index;
        }

        [System.Serializable]
        class SaveData
        {
            public int version = 1;
            public int era;
            public int seatedCard;
            public List<int> cards = new List<int>();
            public List<string> bag = new List<string>();
            public List<string> flags = new List<string>();
            public bool hasDevice;
            public List<string> inactivePaths = new List<string>();
            public List<string> openMechanisms = new List<string>();
            public List<SeqState> sequences = new List<SeqState>();
        }

        EraDirector _director;
        StereoscopeController _scope;
        Transform _room;
        bool _restoring;

        // Manual and autosave are separate slots: an era-change autosave
        // must never clobber a save the player made on purpose.
        static string ManualPath =>
            Path.Combine(Application.persistentDataPath, "stereopsis_save.json");
        static string AutoPath =>
            Path.Combine(Application.persistentDataPath, "stereopsis_autosave.json");

        void Awake()
        {
            _director = GetComponent<EraDirector>();
            _scope = GetComponent<StereoscopeController>();
            var room = GameObject.Find("Room");
            _room = room != null ? room.transform : null;
        }

        void OnEnable() => _director.State.EraChanged += OnEraChanged;
        void OnDisable() => _director.State.EraChanged -= OnEraChanged;

        void OnEraChanged(Era from, Era to)
        {
            if (!_restoring) SaveToDisk(AutoPath);
        }

        void Update()
        {
            if (SavePressed()) { SaveToDisk(ManualPath); DebugHud.Say("Saved."); }
            else if (LoadPressed())
            {
                if (LoadFromDisk()) DebugHud.Say("Loaded.");
                else DebugHud.Say("No save found.");
            }
        }

        // ---- capture ---------------------------------------------------

        public void SaveToDisk() => SaveToDisk(ManualPath);

        public void SaveToDisk(string path)
        {
            if (_room == null) return;
            var d = new SaveData
            {
                era = (int)_director.State.CurrentEra,
                seatedCard = (int)_director.State.SeatedCard,
                cards = _director.State.CollectedCards.Select(c => (int)c).ToList(),
                bag = _director.Bag.Items.ToList(),
                flags = GameFlags.All.ToList(),
                hasDevice = _scope != null && _scope.HasDevice,
            };

            foreach (var t in Tracked())
            {
                string tp = PathOf(t);
                if (!t.gameObject.activeSelf) d.inactivePaths.Add(tp);

                var m = t.GetComponent<Mechanism>();
                if (m != null && m.IsOpen) d.openMechanisms.Add(tp);

                var s = t.GetComponent<BeatSequence>();
                if (s != null && s.CurrentBeat > 0)
                    d.sequences.Add(new SeqState { path = tp, index = s.CurrentBeat });
            }

            File.WriteAllText(path, JsonUtility.ToJson(d, true));
        }

        // ---- restore ---------------------------------------------------

        /// <summary>Loads the manual slot, falling back to the autosave.</summary>
        public bool LoadFromDisk()
        {
            if (File.Exists(ManualPath)) return LoadFromDisk(ManualPath);
            if (File.Exists(AutoPath)) return LoadFromDisk(AutoPath);
            return false;
        }

        public bool LoadFromDisk(string path)
        {
            if (_room == null || !File.Exists(path)) return false;
            var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            _restoring = true;
            try { ApplyData(d); }
            finally { _restoring = false; }
            return true;
        }

        void ApplyData(SaveData d)
        {

            // logical state, replayed through the real APIs
            GameFlags.Clear();
            foreach (var f in d.flags) GameFlags.Set(f);

            foreach (var id in _director.Bag.Items.ToList()) _director.Bag.Remove(id);
            foreach (var id in d.bag) _director.Bag.TryAdd(id);

            var state = _director.State;
            foreach (var c in d.cards) state.CollectCard((StereoCard)c);
            if (d.hasDevice && _scope != null) _scope.GrantDevice(quiet: true);

            // travel: the tether invariant guarantees seated == era's
            // card whenever we are not in the present.
            state.Eject();
            if ((Era)d.era != Era.Present)
            {
                state.SeatCard((StereoCard)d.seatedCard);
                state.Commit();
            }
            else if ((StereoCard)d.seatedCard != StereoCard.None)
            {
                state.SeatCard((StereoCard)d.seatedCard);
            }

            // physical state, applied directly
            var inactive = new HashSet<string>(d.inactivePaths);
            var open = new HashSet<string>(d.openMechanisms);
            var seqs = d.sequences.ToDictionary(s => s.path, s => s.index);

            foreach (var t in Tracked())
            {
                string path = PathOf(t);
                bool shouldBeActive = !inactive.Contains(path);
                if (t.gameObject.activeSelf != shouldBeActive)
                    t.gameObject.SetActive(shouldBeActive);

                var m = t.GetComponent<Mechanism>();
                if (m != null)
                {
                    if (open.Contains(path)) m.RestoreOpen();
                    else if (m.IsOpen) m.RestoreClosed();
                }

                var s = t.GetComponent<BeatSequence>();
                if (s != null)
                    s.RestoreTo(seqs.TryGetValue(path, out int idx) ? idx : 0);
            }
        }

        // ---- helpers ---------------------------------------------------

        /// <summary>Every transform under Room except the organizational
        /// roots themselves (Shell and the five era roots, whose active
        /// states belong to the EraDirector).</summary>
        IEnumerable<Transform> Tracked()
        {
            foreach (Transform structural in _room)
                foreach (var t in structural.GetComponentsInChildren<Transform>(true))
                    if (t != structural) yield return t;
        }

        string PathOf(Transform t)
        {
            var parts = new List<string>();
            while (t != null && t != _room)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        static bool SavePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F5);
#endif
        }

        static bool LoadPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F9);
#endif
        }
    }
}
