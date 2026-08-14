using System.Collections;
using Stereopsis.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Stereopsis
{
    /// <summary>
    /// Runs the pickup/inspect gesture: tap an Inspectable, it lifts to
    /// the eye; drag turns it in your fingers (with a little inertia);
    /// right-click / Escape / second finger puts it back exactly where
    /// it was. Claims the InteractionGate for the whole gesture so the
    /// camera stands still while your hands are full.
    ///
    /// Never reparents the object — it moves in world space and its
    /// original local pose is restored on return, so era roots stay
    /// clean. If the era changes mid-inspection (debug travel), the
    /// object snaps home instantly.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Camera))]
    public sealed class InspectionController : MonoBehaviour
    {
        [SerializeField] float rotateDegreesPerPixel = 0.35f;
        [SerializeField] float inertiaDamping = 4f;
        [SerializeField] LayerMask inspectMask = ~0;

        enum Phase { Idle, Lifting, Holding, Returning }

        Camera _cam;
        Phase _phase = Phase.Idle;
        Inspectable _held;
        Transform _heldT;
        Vector3 _homeLocalPos;
        Quaternion _homeLocalRot;
        Vector2 _lastPointer;
        Vector2 _angVel;
        bool _dragging;
        EraDirector _director;

        public bool IsInspecting => _phase != Phase.Idle;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _director = FindFirstObjectByType<EraDirector>();
        }

        void OnEnable()
        {
            if (_director != null) _director.State.EraChanged += OnEraChanged;
        }

        void OnDisable()
        {
            if (_director != null) _director.State.EraChanged -= OnEraChanged;
        }

        void OnEraChanged(Era from, Era to)
        {
            // The room just changed around a held object: put it back
            // instantly, no animation, before its root deactivates.
            if (_phase == Phase.Idle) return;
            StopAllCoroutines();
            RestoreHome();
            EndGesture();
        }

        void Update()
        {
            switch (_phase)
            {
                case Phase.Idle: UpdateIdle(); break;
                case Phase.Holding: UpdateHolding(); break;
                // Lifting/Returning are coroutine-driven.
            }
        }

        void UpdateIdle()
        {
            if (InteractionGate.Busy) return;
            if (!PointerDown(out var pos)) return;

            // Tap priority: device > card > mechanism > inspectable.
            var device = PickUtil.Pick<DevicePickup>(_cam, pos, inspectMask, 0.6f);
            if (device != null)
            {
                var scope = FindFirstObjectByType<StereoscopeController>();
                if (scope != null) scope.GrantDevice();
                device.gameObject.SetActive(false);
                return;
            }

            var cardPk = PickUtil.Pick<CardPickup>(_cam, pos, inspectMask, 0.6f);
            if (cardPk != null)
            {
                _director.State.CollectCard(cardPk.Card);
                DebugHud.Say("A stereo card. The year on the mount reads " + (int)cardPk.Card + ".");
                cardPk.gameObject.SetActive(false);
                return;
            }

            var doc = PickUtil.Pick<Readable>(_cam, pos, inspectMask, 0.6f);
            if (doc != null)
            {
                GameFlags.Set(doc.SetsFlag);
                DocViewer.Show(doc.Title, doc.Body);
                return;
            }

            var seq = PickUtil.Pick<BeatSequence>(_cam, pos, inspectMask, 0.6f);
            if (seq != null && !seq.IsComplete)
            {
                seq.TryAdvance(_director != null ? _director.Bag : null);
                return;
            }

            var mech = PickUtil.Pick<Mechanism>(_cam, pos, inspectMask, 0.6f);
            if (mech != null && !mech.IsOpen)
            {
                mech.TryOpen(_director != null ? _director.Bag : null);
                return;
            }

            var target = PickUtil.Pick<Inspectable>(_cam, pos, inspectMask, 0.6f);
            if (target == null) return;
            if (!InteractionGate.Claim(this)) return;

            _held = target;
            _heldT = target.transform;
            _homeLocalPos = _heldT.localPosition;
            _homeLocalRot = _heldT.localRotation;
            _lastPointer = pos;
            _angVel = Vector2.zero;
            _phase = Phase.Lifting;
            StartCoroutine(LiftToEye());
        }

        void UpdateHolding()
        {
            if (TakePressed() && _held.Takeable)
            {
                if (_director != null && _director.Bag.TryAdd(_held.ItemId))
                {
                    DebugHud.Say("Taken: " + _held.ItemId);
                    var taken = _heldT;
                    EndGesture();
                    taken.gameObject.SetActive(false);
                }
                else
                {
                    DebugHud.Say("Your hands are full.");
                }
                return;
            }

            if (BackRequested())
            {
                _phase = Phase.Returning;
                StartCoroutine(ReturnHome());
                return;
            }

            if (PointerDown(out var downPos))
            {
                _dragging = true;
                _lastPointer = downPos;
            }
            else if (_dragging && PointerHeld(out var pos))
            {
                var delta = pos - _lastPointer;
                _lastPointer = pos;
                _angVel = delta / Mathf.Max(UnityEngine.Time.deltaTime, 0.001f);
                Spin(delta);
            }
            else
            {
                _dragging = false;
                // let it coast, then settle
                if (_angVel.sqrMagnitude > 1f)
                {
                    Spin(_angVel * UnityEngine.Time.deltaTime);
                    _angVel = Vector2.Lerp(_angVel, Vector2.zero,
                        inertiaDamping * UnityEngine.Time.deltaTime);
                }
            }

            // track the eye even if the camera drifts
            _heldT.position = Vector3.Lerp(_heldT.position, HoldPoint(),
                12f * UnityEngine.Time.deltaTime);
        }

        void Spin(Vector2 pixels)
        {
            _heldT.rotation =
                Quaternion.AngleAxis(-pixels.x * rotateDegreesPerPixel, _cam.transform.up) *
                Quaternion.AngleAxis(pixels.y * rotateDegreesPerPixel, _cam.transform.right) *
                _heldT.rotation;
        }

        Vector3 HoldPoint() =>
            _cam.transform.position + _cam.transform.forward * _held.HoldDistance;

        IEnumerator LiftToEye()
        {
            var p0 = _heldT.position;
            var r0 = _heldT.rotation;
            float t = 0f;
            while (t < 1f)
            {
                t += UnityEngine.Time.deltaTime / _held.LiftSeconds;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                _heldT.position = Vector3.Lerp(p0, HoldPoint(), e);
                _heldT.rotation = r0; // keep its found orientation on the way up
                yield return null;
            }
            _dragging = false;
            _phase = Phase.Holding;
        }

        IEnumerator ReturnHome()
        {
            var p0 = _heldT.position;
            var r0 = _heldT.rotation;
            var parent = _heldT.parent;
            float t = 0f;
            while (t < 1f)
            {
                t += UnityEngine.Time.deltaTime / _held.LiftSeconds;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                var targetPos = parent != null
                    ? parent.TransformPoint(_homeLocalPos) : _homeLocalPos;
                var targetRot = parent != null
                    ? parent.rotation * _homeLocalRot : _homeLocalRot;
                _heldT.position = Vector3.Lerp(p0, targetPos, e);
                _heldT.rotation = Quaternion.Slerp(r0, targetRot, e);
                yield return null;
            }
            RestoreHome();
            EndGesture();
        }

        void RestoreHome()
        {
            if (_heldT == null) return;
            _heldT.localPosition = _homeLocalPos;
            _heldT.localRotation = _homeLocalRot;
        }

        void EndGesture()
        {
            _held = null;
            _heldT = null;
            _dragging = false;
            _phase = Phase.Idle;
            InteractionGate.Release(this);
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

        static bool TakePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.T);
#endif
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
