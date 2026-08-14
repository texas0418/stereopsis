using System.Collections;
using Stereopsis.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Stereopsis
{
    /// <summary>
    /// The travel ritual (DECISIONS 49), grey-box controls:
    ///   S        raise / lower the stereoscope
    ///   1..4     seat a card while raised (1698, 1774, 1861, 1922)
    ///   E        eject the seated card — snaps home to the present
    ///   drag up  run the focus rail; past the snap point it commits
    ///
    /// While raised with a card seated, the ghost of that era hangs over
    /// the room, sharpening as the rail advances. At full focus the
    /// ghost becomes the room. Claims the InteractionGate for the whole
    /// time it is raised, so the camera and hands stand down.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    [RequireComponent(typeof(EraDirector))]
    public sealed class StereoscopeController : MonoBehaviour
    {
        [SerializeField] float raisedFov = 50f;
        [SerializeField] float fovLerpSpeed = 6f;
        [SerializeField] float railPerPixel = 0.0025f;
        [SerializeField] float snapPoint = 0.7f;
        [Tooltip("Grey-box convenience: start with all four cards found.")]
        [SerializeField] bool debugAllCards = true;

        [Tooltip("Start owning the device. Off = it must be found in play.")]
        [SerializeField] bool debugStartWithDevice = true;

        EraDirector _director;
        Camera _cam;
        GhostPreview _ghost;
        bool _raised;
        float _rail;
        bool _animating;
        float _loweredFov;
        Vector2 _lastPointer;
        bool _dragging;
        bool _hasDevice;

        public bool IsRaised => _raised;
        public bool HasDevice => _hasDevice;

        /// <summary>Called when the player takes Abigail's stereoscope.
        /// quiet = save-system restore, no announcement.</summary>
        public void GrantDevice(bool quiet = false)
        {
            if (_hasDevice) return;
            _hasDevice = true;
            if (!quiet)
                DebugHud.Say("A stereoscope, wrapped in oilcloth. Press S to raise it.");
        }

        void Awake()
        {
            _director = GetComponent<EraDirector>();
            _cam = Camera.main;
            _ghost = new GhostPreview();
            _loweredFov = _cam.fieldOfView;
        }

        void Start()
        {
            _hasDevice = debugStartWithDevice;
            if (debugAllCards)
            {
                var s = _director.State;
                s.CollectCard(StereoCard.Card1922);
                s.CollectCard(StereoCard.Card1861);
                s.CollectCard(StereoCard.Card1774);
                s.CollectCard(StereoCard.Card1698);
            }
        }

        void Update()
        {
            float targetFov = _raised ? raisedFov : _loweredFov;
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov,
                fovLerpSpeed * UnityEngine.Time.deltaTime);

            if (_animating) return;

            if (RaiseTogglePressed())
            {
                if (_raised) Lower();
                else Raise();
                return;
            }

            if (!_raised) return;

            var s = _director.State;

            if (SeatPressed(out var card))
            {
                if (s.SeatCard(card)) Sfx.Play("scope.seat");
                RefreshGhost();
            }
            else if (EjectPressed())
            {
                if (s.Eject()) Sfx.Play("scope.eject");
                RefreshGhost();
            }

            // ---- the focus rail ----
            if (PointerDown(out var pos))
            {
                _dragging = true;
                _lastPointer = pos;
            }
            else if (_dragging && PointerHeld(out pos))
            {
                float dy = pos.y - _lastPointer.y;
                _lastPointer = pos;
                if (CanTravel(s))
                {
                    _rail = Mathf.Clamp01(_rail + dy * railPerPixel);
                    _ghost.SetAlpha(GhostAlpha());
                }
            }
            else if (_dragging)
            {
                _dragging = false;
                if (CanTravel(s) && _rail >= snapPoint) StartCoroutine(CommitRoutine());
                else if (_rail > 0f) StartCoroutine(RelaxRail());
            }
        }

        static bool CanTravel(TimeState s) =>
            s.SeatedCard != StereoCard.None && s.SeatedCard.EraOf() != s.CurrentEra;

        void Raise()
        {
            if (!_hasDevice) return;
            if (!InteractionGate.Claim(this)) return;
            _raised = true;
            _rail = 0f;
            Sfx.Play("scope.raise");
            RefreshGhost();
        }

        void Lower()
        {
            _ghost.Hide(false);
            _rail = 0f;
            _dragging = false;
            _raised = false;
            Sfx.Play("scope.lower");
            InteractionGate.Release(this);
        }

        void RefreshGhost()
        {
            var s = _director.State;
            if (CanTravel(s))
                _ghost.Show(_director.RootOf(s.SeatedCard.EraOf()), GhostAlpha());
            else
                _ghost.Hide(false);
        }

        float GhostAlpha() => 0.12f + 0.55f * _rail;

        IEnumerator CommitRoutine()
        {
            _animating = true;
            Sfx.Play("scope.commit");
            // run the rail home: the ghost sharpens to full
            while (_rail < 1f)
            {
                _rail = Mathf.MoveTowards(_rail, 1f, 3f * UnityEngine.Time.deltaTime);
                _ghost.SetAlpha(GhostAlpha());
                yield return null;
            }
            // the ghost becomes the room
            _ghost.Hide(true);
            _director.State.Commit();
            _animating = false;
            Lower();
        }

        IEnumerator RelaxRail()
        {
            _animating = true;
            while (_rail > 0f)
            {
                _rail = Mathf.MoveTowards(_rail, 0f, 4f * UnityEngine.Time.deltaTime);
                _ghost.SetAlpha(GhostAlpha());
                yield return null;
            }
            _animating = false;
        }

        // ---- input, both handlers ------------------------------------

        static bool RaiseTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.S);
#endif
        }

        static bool EjectPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        static bool SeatPressed(out StereoCard card)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame) { card = StereoCard.Card1698; return true; }
                if (k.digit2Key.wasPressedThisFrame) { card = StereoCard.Card1774; return true; }
                if (k.digit3Key.wasPressedThisFrame) { card = StereoCard.Card1861; return true; }
                if (k.digit4Key.wasPressedThisFrame) { card = StereoCard.Card1922; return true; }
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) { card = StereoCard.Card1698; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { card = StereoCard.Card1774; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { card = StereoCard.Card1861; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { card = StereoCard.Card1922; return true; }
#endif
            card = StereoCard.None;
            return false;
        }

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
    }
}
