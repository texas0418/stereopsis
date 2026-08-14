using System.Collections.Generic;
using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// Renders an era root as a translucent ghost over the live room —
    /// the stereoscope's preview (DECISIONS 25). Activates the root,
    /// swaps every renderer to one unlit transparent material, and
    /// disables the root's colliders so the ghost can be seen but never
    /// touched. Restores everything on hide. Deliberately vague: shape
    /// and presence, not detail.
    /// </summary>
    public sealed class GhostPreview
    {
        readonly Material _ghostMat;
        readonly Dictionary<Renderer, Material[]> _saved = new Dictionary<Renderer, Material[]>();
        readonly List<Collider> _disabled = new List<Collider>();
        GameObject _root;

        public GhostPreview()
        {
            _ghostMat = new Material(Shader.Find("Sprites/Default"));
            _ghostMat.color = new Color(0.65f, 0.8f, 1f, 0.15f);
        }

        public bool Active => _root != null;

        public void Show(GameObject root, float alpha)
        {
            if (_root == root) { SetAlpha(alpha); return; }
            Hide(false);
            if (root == null) return;
            _root = root;
            _root.SetActive(true);
            foreach (var r in _root.GetComponentsInChildren<Renderer>(true))
            {
                _saved[r] = r.sharedMaterials;
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = _ghostMat;
                r.sharedMaterials = mats;
            }
            foreach (var c in _root.GetComponentsInChildren<Collider>(true))
            {
                if (c.enabled) { c.enabled = false; _disabled.Add(c); }
            }
            SetAlpha(alpha);
        }

        public void SetAlpha(float a)
        {
            var c = _ghostMat.color;
            c.a = a;
            _ghostMat.color = c;
        }

        /// <summary>Restore materials and colliders. keepActive leaves
        /// the root enabled — used at commit, the moment the ghost
        /// becomes the real era.</summary>
        public void Hide(bool keepActive)
        {
            if (_root == null) return;
            foreach (var kv in _saved)
                if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
            _saved.Clear();
            foreach (var c in _disabled)
                if (c != null) c.enabled = true;
            _disabled.Clear();
            if (!keepActive) _root.SetActive(false);
            _root = null;
        }
    }
}
