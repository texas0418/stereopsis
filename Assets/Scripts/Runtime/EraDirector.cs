using Stereopsis.Core;
using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// Owns the TimeState machine and makes travel visible: exactly one
    /// era root is active at any moment. Everything else in the scene
    /// subscribes to State.EraChanged rather than polling.
    /// </summary>
    public sealed class EraDirector : MonoBehaviour
    {
        [Header("Era roots (Room/...)")]
        [SerializeField] GameObject presentRoot;
        [SerializeField] GameObject root1922;
        [SerializeField] GameObject root1861;
        [SerializeField] GameObject root1774;
        [SerializeField] GameObject root1698;

        public TimeState State { get; } = new TimeState();

        /// <summary>The hand inventory. Tiny by design (DECISIONS 11).</summary>
        public Inventory Bag { get; } = new Inventory();

        void Awake()
        {
            // The one thing you bring into this game from outside it.
            // Awake, not Start: critical state must never wait for the
            // first frame (a backgrounded editor may not tick one).
            Bag.TryAdd("pry-bar");
        }

        /// <summary>The scene root that carries an era's dressing.</summary>
        public GameObject RootOf(Era era) =>
            era == Era.Present ? presentRoot :
            era == Era.Y1922 ? root1922 :
            era == Era.Y1861 ? root1861 :
            era == Era.Y1774 ? root1774 : root1698;

        void OnEnable()
        {
            State.EraChanged += OnEraChanged;
            Apply(State.CurrentEra);
        }

        void OnDisable() => State.EraChanged -= OnEraChanged;

        void OnEraChanged(Era from, Era to)
        {
            Sfx.Play("travel.arrive");
            Apply(to);
        }

        void Apply(Era era)
        {
            SetActiveSafe(presentRoot, era == Era.Present);
            SetActiveSafe(root1922, era == Era.Y1922);
            SetActiveSafe(root1861, era == Era.Y1861);
            SetActiveSafe(root1774, era == Era.Y1774);
            SetActiveSafe(root1698, era == Era.Y1698);
        }

        static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

#if UNITY_EDITOR
        // Editor-only debug travel, usable long before the stereoscope
        // exists: right-click the component header in the Inspector.
        [ContextMenu("Debug/Collect All Cards")]
        void DebugCollectAll()
        {
            State.CollectCard(StereoCard.Card1922);
            State.CollectCard(StereoCard.Card1861);
            State.CollectCard(StereoCard.Card1774);
            State.CollectCard(StereoCard.Card1698);
        }

        [ContextMenu("Debug/Travel 1922")] void DebugTravel1922() => DebugTravel(StereoCard.Card1922);
        [ContextMenu("Debug/Travel 1861")] void DebugTravel1861() => DebugTravel(StereoCard.Card1861);
        [ContextMenu("Debug/Travel 1774")] void DebugTravel1774() => DebugTravel(StereoCard.Card1774);
        [ContextMenu("Debug/Travel 1698")] void DebugTravel1698() => DebugTravel(StereoCard.Card1698);
        [ContextMenu("Debug/Eject (Home)")] void DebugEject() => State.Eject();

        void DebugTravel(StereoCard card)
        {
            State.CollectCard(card);
            State.SeatCard(card);
            State.Commit();
        }
#endif
    }
}
