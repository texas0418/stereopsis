using Stereopsis.Core;
using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// Grey-box HUD: current era, seated card, collected cards, bag, and
    /// a transient message line so mechanisms can talk without UI art.
    /// Editor and dev builds only.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        static string _msg = "";
        static float _until;

        EraDirector _director;
        StereoscopeController _scope;

        public static void Say(string msg)
        {
            _msg = msg;
            _until = UnityEngine.Time.time + 4f;
        }

        void Awake()
        {
            _director = FindFirstObjectByType<EraDirector>();
            _scope = FindFirstObjectByType<StereoscopeController>();
        }

        void OnGUI()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return; // gameplay code may always Say(); only the drawing is dev-only
#endif
            if (_director == null) return;
            var s = _director.State;

            string era = s.CurrentEra == Era.Present ? "Present" : ((int)s.CurrentEra).ToString();
            string seated = s.SeatedCard == StereoCard.None ? "empty slot" : Name(s.SeatedCard);
            string cards = s.CollectedCards.Count == 0 ? "none" : Join(s.CollectedCards);
            string bag = _director.Bag.Items.Count == 0 ? "empty" : string.Join(", ", _director.Bag.Items);
            string device = _scope != null && _scope.HasDevice
                ? (_scope.IsRaised ? "raised" : "in hand") : "not found";

            GUI.Label(new Rect(12, 8, 1400, 24), "YEAR: " + era + "    stereoscope: " + device + "    card: " + seated);
            GUI.Label(new Rect(12, 30, 1400, 24), "cards found: " + cards);
            GUI.Label(new Rect(12, 52, 1400, 24), "bag: " + bag);

            if (UnityEngine.Time.time < _until && !string.IsNullOrEmpty(_msg))
                GUI.Label(new Rect(12, 84, 1400, 26), "» " + _msg);
        }

        static string Name(StereoCard c) => ((int)c).ToString();

        static string Join(System.Collections.Generic.IReadOnlyCollection<StereoCard> cards)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in cards)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(Name(c));
            }
            return sb.ToString();
        }
    }
}
