using UnityEngine;

namespace Stereopsis
{
    /// <summary>A stereo card lying in the world. Tap to collect —
    /// unlocking its year on the device (CHAIN section 1).</summary>
    public sealed class CardPickup : MonoBehaviour
    {
        [SerializeField] Stereopsis.Core.StereoCard card;
        public Stereopsis.Core.StereoCard Card => card;
    }
}
