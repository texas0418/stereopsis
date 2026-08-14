using System;
using System.Collections;
using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// A station with a sequence of tactile beats (MECHANISMS.txt):
    /// wiggle-then-lift, twist-then-reverse, press-panel-then-pull.
    /// Each tap advances the current beat if its requirements pass;
    /// otherwise the beat's locked line plays. State (reveals, flags)
    /// commits the moment a beat completes — animation is garnish,
    /// per the state-first rule.
    ///
    /// Replaces Mechanism on flagship stations; simple one-shots keep
    /// using Mechanism.
    /// </summary>
    public sealed class BeatSequence : MonoBehaviour
    {
        [Serializable]
        public struct Beat
        {
            [TextArea(1, 3)] public string message;       // said when the beat completes
            [TextArea(1, 3)] public string lockedMessage; // said when blocked
            public string requiresItemId;
            public string requiresFlag;
            [Tooltip("Taps needed to complete this beat. 0 = 1.")]
            public int taps;
            public Vector3 moveDelta;
            public Vector3 rotateDelta;
            public GameObject[] reveals;
            public string setsFlag;
        }

        [SerializeField] Beat[] beats = new Beat[0];
        [Tooltip("Transform animated by beat deltas. Defaults to this.")]
        [SerializeField] Transform animated;
        [SerializeField] float secondsPerBeat = 0.45f;

        int _index;
        int _tapsDone;
        bool _poseCaptured;
        Vector3 _homePos;
        Quaternion _homeRot;

        public bool IsComplete => _index >= beats.Length;
        public int CurrentBeat => _index;
        public event Action<int> BeatCompleted;
        public event Action Completed;

        void Awake() => EnsurePose();

        void EnsurePose()
        {
            if (_poseCaptured) return;
            var t = animated != null ? animated : transform;
            _homePos = t.localPosition;
            _homeRot = t.localRotation;
            _poseCaptured = true;
        }

        /// <summary>Save-system restore: jump to the state after
        /// `index` completed beats — cumulative end pose, no messages,
        /// no animation. Reveals are handled by the save system's
        /// active-state pass.</summary>
        public void RestoreTo(int index)
        {
            EnsurePose();
            _index = Mathf.Clamp(index, 0, beats.Length);
            _tapsDone = 0;
            var pos = _homePos;
            var rot = _homeRot;
            for (int i = 0; i < _index; i++)
            {
                pos += beats[i].moveDelta;
                rot *= Quaternion.Euler(beats[i].rotateDelta);
            }
            var t = animated != null ? animated : transform;
            t.localPosition = pos;
            t.localRotation = rot;
        }

        /// <summary>One tap on the station. Returns true if anything
        /// happened (advance or partial tap); false only when blocked.</summary>
        public bool TryAdvance(Stereopsis.Core.Inventory bag)
        {
            if (IsComplete) return false;
            var b = beats[_index];

            if (!string.IsNullOrEmpty(b.requiresFlag) && !GameFlags.Has(b.requiresFlag))
            {
                if (!string.IsNullOrEmpty(b.lockedMessage)) DebugHud.Say(b.lockedMessage);
                return false;
            }
            if (!string.IsNullOrEmpty(b.requiresItemId) &&
                (bag == null || !bag.Has(b.requiresItemId)))
            {
                if (!string.IsNullOrEmpty(b.lockedMessage)) DebugHud.Say(b.lockedMessage);
                return false;
            }

            _tapsDone++;
            int needed = Mathf.Max(1, b.taps);
            if (_tapsDone < needed)
            {
                // partial progress; the locked line doubles as feedback
                if (!string.IsNullOrEmpty(b.lockedMessage)) DebugHud.Say(b.lockedMessage);
                return true;
            }

            // beat completes: state first
            _tapsDone = 0;
            _index++;
            if (!string.IsNullOrEmpty(b.setsFlag)) GameFlags.Set(b.setsFlag);
            if (b.reveals != null)
                for (int i = 0; i < b.reveals.Length; i++)
                    if (b.reveals[i] != null) b.reveals[i].SetActive(true);
            if (!string.IsNullOrEmpty(b.message)) DebugHud.Say(b.message);
            BeatCompleted?.Invoke(_index - 1);
            if (IsComplete) Completed?.Invoke();

            if ((b.moveDelta != Vector3.zero || b.rotateDelta != Vector3.zero)
                && isActiveAndEnabled)
                StartCoroutine(Animate(b.moveDelta, b.rotateDelta));
            return true;
        }

        IEnumerator Animate(Vector3 move, Vector3 rotate)
        {
            var t = animated != null ? animated : transform;
            var p0 = t.localPosition;
            var r0 = t.localRotation;
            var p1 = p0 + move;
            var r1 = r0 * Quaternion.Euler(rotate);
            float x = 0f;
            while (x < 1f)
            {
                x += UnityEngine.Time.deltaTime / Mathf.Max(0.05f, secondsPerBeat);
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
                t.localPosition = Vector3.Lerp(p0, p1, e);
                t.localRotation = Quaternion.Slerp(r0, r1, e);
                yield return null;
            }
        }
    }
}
