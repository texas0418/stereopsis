using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Stereopsis
{
    /// <summary>
    /// Grey-box document reader: an OnGUI panel that claims the
    /// InteractionGate while open, so reading is a held moment rather
    /// than an overlay you act through. Esc or right-click puts the
    /// paper down. Replaced by real UI later.
    /// </summary>
    public sealed class DocViewer : MonoBehaviour
    {
        static DocViewer _instance;

        string _title = "";
        string _body = "";
        bool _open;

        void Awake() => _instance = this;

        public static void Show(string title, string body)
        {
            if (_instance == null || _instance._open) return;
            if (!InteractionGate.Claim(_instance)) return;
            _instance._title = title;
            _instance._body = body;
            _instance._open = true;
        }

        void Update()
        {
            if (_open && CloseRequested())
            {
                _open = false;
                InteractionGate.Release(this);
            }
        }

        void OnGUI()
        {
            if (!_open) return;

            float w = Mathf.Min(720f, Screen.width - 80f);
            float h = Mathf.Min(520f, Screen.height - 120f);
            var r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

            // stacked boxes for opacity with the default skin
            GUI.Box(r, ""); GUI.Box(r, ""); GUI.Box(r, "");

            var titleStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true };
            var bodyStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 14, wordWrap = true, richText = false };
            var hintStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 11, alignment = TextAnchor.MiddleRight };

            GUI.Label(new Rect(r.x + 24, r.y + 16, w - 48, 44), _title, titleStyle);
            GUI.Label(new Rect(r.x + 24, r.y + 62, w - 48, h - 100), _body, bodyStyle);
            GUI.Label(new Rect(r.x + 24, r.y + h - 32, w - 48, 22),
                "Esc to put it down", hintStyle);
        }

        static bool CloseRequested()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 1 &&
                Touchscreen.current.touches[1].press.wasPressedThisFrame) return true;
#else
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) return true;
#endif
            return false;
        }
    }
}
