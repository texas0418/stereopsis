namespace Stereopsis
{
    /// <summary>
    /// One interaction at a time. Whoever is mid-gesture (inspecting an
    /// object, working a mechanism, raising the stereoscope) claims the
    /// gate; the camera and other systems stand down until it's released.
    /// </summary>
    public static class InteractionGate
    {
        public static object Owner { get; private set; }
        public static bool Busy => Owner != null;

        public static bool Claim(object owner)
        {
            if (Owner != null && Owner != owner) return false;
            Owner = owner;
            return true;
        }

        public static void Release(object owner)
        {
            if (Owner == owner) Owner = null;
        }
    }
}
