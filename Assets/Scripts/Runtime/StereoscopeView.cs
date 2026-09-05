using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// The visible device: binds the real Holmes-Bates model to the focus
    /// rail. The card holder (card + its frame) slides along the rail axis
    /// as focus runs 0..1, and the seated card's face can be swapped. Pure
    /// view — it reads a focus value the StereoscopeController owns and
    /// never decides anything itself.
    ///
    /// Parts come from the imported FBX (base, card, card_rest, hood,
    /// lens, lens.001, wires); the holder parts are the ones that travel.
    /// </summary>
    public sealed class StereoscopeView : MonoBehaviour
    {
        [Tooltip("Parts that slide together as the card holder.")]
        [SerializeField] Transform[] holderParts;

        [Tooltip("Local axis the holder travels along (usually forward).")]
        [SerializeField] Vector3 railAxis = Vector3.forward;

        [Tooltip("Holder position at focus=0, metres along the axis.")]
        [SerializeField] float nearOffset = 0f;

        [Tooltip("Holder position at focus=1, metres along the axis.")]
        [SerializeField] float farOffset = 0.14f;

        [Tooltip("The card face renderer, for swapping the seated card.")]
        [SerializeField] Renderer cardRenderer;

        Vector3[] _home;

        void Awake()
        {
            if (holderParts != null)
            {
                _home = new Vector3[holderParts.Length];
                for (int i = 0; i < holderParts.Length; i++)
                    if (holderParts[i] != null) _home[i] = holderParts[i].localPosition;
            }
        }

        /// <summary>0 = holder near the hood (sharp), 1 = far end.</summary>
        public void SetFocus(float t)
        {
            if (holderParts == null || _home == null) return;
            float d = Mathf.Lerp(nearOffset, farOffset, Mathf.Clamp01(t));
            Vector3 delta = railAxis.normalized * d;
            for (int i = 0; i < holderParts.Length; i++)
                if (holderParts[i] != null)
                    holderParts[i].localPosition = _home[i] + delta;
        }

        /// <summary>Swap the visible card face (per seated era card).</summary>
        public void SetCardMaterial(Material m)
        {
            if (cardRenderer != null && m != null) cardRenderer.sharedMaterial = m;
        }
    }
}
