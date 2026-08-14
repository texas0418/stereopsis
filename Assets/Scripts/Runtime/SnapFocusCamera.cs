using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Stereopsis
{
    /// <summary>
    /// The snap-to-focus rig (DECISIONS 30). Two states:
    ///   Overview — resting at the overview pose (the doorway).
    ///   Focused  — orbiting a FocusPoint within its clamps.
    /// Tap a FocusPoint collider to glide in; drag to orbit; tap
    /// nothing (or right-click / second finger) to glide back out.
    /// No walking, ever.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class SnapFocusCamera : MonoBehaviour
    {
        [SerializeField] Transform overviewPose;
        [SerializeField] float transitionSeconds = 0.4f;
        [SerializeField] float orbitDegreesPerPixel = 0.2f;
        [SerializeField] LayerMask focusMask = ~0;

        Camera _cam;
        FocusPoint _focus;
        Coroutine _moving;
        float _yaw;       // offset from approach yaw, clamped to ±YawLimit
        float _pitch;
        float _baseYaw;   // yaw of the approach direction at focus time
        Vector2 _lastPointer;
        bool _dragging;

        public bool IsFocused => _focus != null;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (overviewPose != null)
                transform.SetPositionAndRotation(overviewPose.position, overviewPose.rotation);
        }

        void Update()
        {
            if (_moving != null) return;

            // hands full: someone else owns the current gesture
            if (InteractionGate.Busy) { _dragging = false; return; }

            if (BackRequested())
            {
                if (_focus != null) ReturnToOverview();
                return;
            }

            if (PointerDown(out var pos))
            {
                _lastPointer = pos;
                var fp = PickUtil.Pick<FocusPoint>(_cam, pos, focusMask, 1f);
                if (fp != null && fp != _focus) { FocusOn(fp); return; }
                if (_focus != null) { _dragging = true; }         // orbit from anywhere while focused
            }
            else if (PointerHeld(out pos) && _dragging && _focus != null)
            {
                var delta = pos - _lastPointer;
                _lastPointer = pos;
                _yaw = Mathf.Clamp(_yaw + delta.x * orbitDegreesPerPixel, -_focus.YawLimit, _focus.YawLimit);
                _pitch = Mathf.Clamp(_pitch - delta.y * orbitDegreesPerPixel, _focus.PitchMin, _focus.PitchMax);
                ApplyOrbit();
            }
            else
            {
                _dragging = false;
            }
        }

        public void FocusOn(FocusPoint fp)
        {
            _focus = fp;
            _yaw = 0f;
            _pitch = fp.DefaultPitch;

            // Approach from wherever the camera is now, horizontally.
            var toTarget = fp.Target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) toTarget = fp.Target.forward;
            _baseYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

            StartMove(OrbitPosition(), OrbitRotation());
        }

        public void ReturnToOverview()
        {
            _focus = null;
            _dragging = false;
            if (overviewPose != null)
                StartMove(overviewPose.position, overviewPose.rotation);
        }

        void ApplyOrbit()
        {
            transform.SetPositionAndRotation(OrbitPosition(), OrbitRotation());
        }

        Quaternion OrbitRotation() => Quaternion.Euler(_pitch, _baseYaw + _yaw, 0f);

        Vector3 OrbitPosition() =>
            _focus.Target.position + OrbitRotation() * (Vector3.back * _focus.Distance);

        void StartMove(Vector3 pos, Quaternion rot)
        {
            if (_moving != null) StopCoroutine(_moving);
            _moving = StartCoroutine(MoveTo(pos, rot));
        }

        IEnumerator MoveTo(Vector3 pos, Quaternion rot)
        {
            var p0 = transform.position;
            var r0 = transform.rotation;
            float t = 0f;
            while (t < 1f)
            {
                t += UnityEngine.Time.deltaTime / Mathf.Max(0.01f, transitionSeconds);
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                transform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(p0, pos, e),
                    Quaternion.SlerpUnclamped(r0, rot, e));
                yield return null;
            }
            _moving = null;
        }

        // ---- input, both handlers ------------------------------------

        static bool PointerDown(out Vector2 pos)
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pos = Mouse.current.position.ReadValue();
                return true;
            }
#else
            if (Input.GetMouseButtonDown(0)) { pos = Input.mousePosition; return true; }
#endif
            pos = default;
            return false;
        }

        static bool PointerHeld(out Vector2 pos)
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.isPressed)
            {
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                pos = Mouse.current.position.ReadValue();
                return true;
            }
#else
            if (Input.GetMouseButton(0)) { pos = Input.mousePosition; return true; }
#endif
            pos = default;
            return false;
        }

        static bool BackRequested()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 1 &&
                Touchscreen.current.touches[1].press.wasPressedThisFrame) return true;
#else
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) return true;
#endif
            return false;
        }
    }
}
