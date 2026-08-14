using System;
using System.Collections;
using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// A one-way openable: boards that lever up, lids that grind off,
    /// bricks that pull out. Optionally requires an inventory item,
    /// optionally reveals hidden objects when opened. Grey-box animation
    /// is a simple move/rotate of the animated transform.
    /// </summary>
    public sealed class Mechanism : MonoBehaviour
    {
        [Tooltip("Inventory item id needed to open. Empty = opens freely.")]
        [SerializeField] string requiresItemId = "";
        [SerializeField] bool consumesItem;
        [Tooltip("Objects activated when this opens.")]
        [SerializeField] GameObject[] reveals = new GameObject[0];
        [Tooltip("Transform to animate. Defaults to this one.")]
        [SerializeField] Transform animated;
        [SerializeField] Vector3 moveDelta;
        [SerializeField] Vector3 rotateDelta;
        [SerializeField] float seconds = 0.6f;
        [SerializeField] string openMessage = "";
        [SerializeField] string lockedMessage = "";

        [Tooltip("A mechanism that never opens — it exists to say no. " +
                 "The 1922 brick: sound mortar, wrong century.")]
        [SerializeField] bool neverOpens;

        [Tooltip("Knowledge flag required to open. Empty = none.")]
        [SerializeField] string requiresFlag = "";

        [Tooltip("Knowledge flag set when opened. Empty = none.")]
        [SerializeField] string setsFlag = "";

        bool _open;
        bool _poseCaptured;
        Vector3 _homePos;
        Quaternion _homeRot;

        public bool IsOpen => _open;
        public event Action Opened;

        void Awake() => EnsurePose();

        void EnsurePose()
        {
            if (_poseCaptured) return;
            var t = animated != null ? animated : transform;
            _homePos = t.localPosition;
            _homeRot = t.localRotation;
            _poseCaptured = true;
        }

        /// <summary>Save-system restore: mark open and jump to the end
        /// pose, no messages, no animation. Reveals are handled by the
        /// save system's active-state pass.</summary>
        public void RestoreOpen()
        {
            EnsurePose();
            _open = true;
            var t = animated != null ? animated : transform;
            t.localPosition = _homePos + moveDelta;
            t.localRotation = _homeRot * Quaternion.Euler(rotateDelta);
        }

        /// <summary>Save-system restore: mark closed and return to the
        /// original pose.</summary>
        public void RestoreClosed()
        {
            EnsurePose();
            _open = false;
            var t = animated != null ? animated : transform;
            t.localPosition = _homePos;
            t.localRotation = _homeRot;
        }

        public void TryOpen(Stereopsis.Core.Inventory bag)
        {
            if (_open) return;
            if (neverOpens)
            {
                if (!string.IsNullOrEmpty(lockedMessage)) DebugHud.Say(lockedMessage);
                return;
            }
            if (!string.IsNullOrEmpty(requiresFlag) && !GameFlags.Has(requiresFlag))
            {
                if (!string.IsNullOrEmpty(lockedMessage)) DebugHud.Say(lockedMessage);
                return;
            }
            if (!string.IsNullOrEmpty(requiresItemId) &&
                (bag == null || !bag.Has(requiresItemId)))
            {
                if (!string.IsNullOrEmpty(lockedMessage)) DebugHud.Say(lockedMessage);
                return;
            }
            _open = true;
            if (consumesItem && !string.IsNullOrEmpty(requiresItemId))
                bag.Remove(requiresItemId);
            if (!string.IsNullOrEmpty(setsFlag)) GameFlags.Set(setsFlag);
            if (!string.IsNullOrEmpty(openMessage)) DebugHud.Say(openMessage);

            // State first, animation as garnish: reveals and events must
            // never depend on a frame ticking. If the animation is killed
            // mid-flight (era switch, backgrounded editor), the game state
            // is already correct and nothing is lost.
            for (int i = 0; i < reveals.Length; i++)
                if (reveals[i] != null) reveals[i].SetActive(true);
            Opened?.Invoke();

            if (isActiveAndEnabled) StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            var t = animated != null ? animated : transform;
            var p0 = t.localPosition;
            var r0 = t.localRotation;
            var p1 = p0 + moveDelta;
            var r1 = r0 * Quaternion.Euler(rotateDelta);
            float x = 0f;
            while (x < 1f)
            {
                x += UnityEngine.Time.deltaTime / Mathf.Max(0.05f, seconds);
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
                t.localPosition = Vector3.Lerp(p0, p1, e);
                t.localRotation = Quaternion.Slerp(r0, r1, e);
                yield return null;
            }
        }
    }
}
