using UnityEngine;

namespace Stereopsis
{
    /// <summary>
    /// A point of interest the camera can snap to (DECISIONS 30).
    /// Put this on an object with a collider; the camera glides in to
    /// Distance from Target and orbits within the clamps. The Room's
    /// grammar: you never walk, you are handed viewpoints.
    /// </summary>
    public sealed class FocusPoint : MonoBehaviour
    {
        [Tooltip("What the camera looks at. Defaults to this transform.")]
        [SerializeField] Transform target;

        [Tooltip("Camera distance from the target, metres.")]
        [SerializeField] float distance = 0.9f;

        [Header("Orbit clamps, degrees")]
        [SerializeField] float yawLimit = 45f;
        [SerializeField] float pitchMin = 5f;
        [SerializeField] float pitchMax = 60f;
        [SerializeField] float defaultPitch = 15f;

        public Transform Target => target != null ? target : transform;
        public float Distance => Mathf.Max(0.1f, distance);
        public float YawLimit => yawLimit;
        public float PitchMin => pitchMin;
        public float PitchMax => pitchMax;
        public float DefaultPitch => Mathf.Clamp(defaultPitch, pitchMin, pitchMax);
    }
}
