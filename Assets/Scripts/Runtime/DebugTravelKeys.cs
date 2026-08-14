#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Stereopsis.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Stereopsis
{
    /// <summary>
    /// Editor/dev-build only: number keys travel the timeline so era
    /// work is testable years before the stereoscope exists.
    ///   1 = 1698   2 = 1774   3 = 1861   4 = 1922   0 = eject (present)
    /// Strips itself out of release builds entirely.
    /// </summary>
    [RequireComponent(typeof(EraDirector))]
    public sealed class DebugTravelKeys : MonoBehaviour
    {
        EraDirector _director;

        void Awake() => _director = GetComponent<EraDirector>();

        void Update()
        {
            // stand down while any gesture owns the input — in particular
            // while the stereoscope is raised, where 1-4 mean seat-a-card
            if (InteractionGate.Busy) return;

            if (Pressed1()) Travel(StereoCard.Card1698);
            else if (Pressed2()) Travel(StereoCard.Card1774);
            else if (Pressed3()) Travel(StereoCard.Card1861);
            else if (Pressed4()) Travel(StereoCard.Card1922);
            else if (Pressed0()) _director.State.Eject();
        }

        void Travel(StereoCard card)
        {
            var s = _director.State;
            s.CollectCard(card); // debug shortcut skips the finding
            s.SeatCard(card);
            s.Commit();
        }

#if ENABLE_INPUT_SYSTEM
        static bool Pressed1() => Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;
        static bool Pressed2() => Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;
        static bool Pressed3() => Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame;
        static bool Pressed4() => Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame;
        static bool Pressed0() => Keyboard.current != null && Keyboard.current.digit0Key.wasPressedThisFrame;
#else
        static bool Pressed1() => Input.GetKeyDown(KeyCode.Alpha1);
        static bool Pressed2() => Input.GetKeyDown(KeyCode.Alpha2);
        static bool Pressed3() => Input.GetKeyDown(KeyCode.Alpha3);
        static bool Pressed4() => Input.GetKeyDown(KeyCode.Alpha4);
        static bool Pressed0() => Input.GetKeyDown(KeyCode.Alpha0);
#endif
    }
}
#endif
