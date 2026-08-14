using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// A small object you can pick up and turn in your fingers — the
    /// second verb of the game (the first is snap-to-focus). Put this on
    /// an object with a collider. The InspectionController does the rest.
    /// </summary>
    public sealed class Inspectable : MonoBehaviour
    {
        [Tooltip("How close to the eye it is held, metres.")]
        [SerializeField] float holdDistance = 0.45f;

        [Tooltip("Seconds for the lift to the eye and the return.")]
        [SerializeField] float liftSeconds = 0.35f;

        [Tooltip("Inventory id. Empty = look only, cannot be taken.")]
        [SerializeField] string itemId = "";

        public float HoldDistance => Mathf.Max(0.15f, holdDistance);
        public float LiftSeconds => Mathf.Max(0.05f, liftSeconds);
        public string ItemId => itemId;
        public bool Takeable => !string.IsNullOrEmpty(itemId);
    }
}
