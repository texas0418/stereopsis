using System.Collections.Generic;

namespace Stereopsis
{
    /// <summary>
    /// What the player knows, as opposed to what they carry. Set by
    /// reading documents and opening mechanisms; checked by mechanisms
    /// that need knowledge rather than keys — you cannot write a name
    /// you have never learned.
    /// </summary>
    public static class GameFlags
    {
        static readonly HashSet<string> _flags = new HashSet<string>();

        public static bool Has(string flag) =>
            !string.IsNullOrEmpty(flag) && _flags.Contains(flag);

        public static void Set(string flag)
        {
            if (!string.IsNullOrEmpty(flag)) _flags.Add(flag);
        }

        public static void Clear() => _flags.Clear();
    }
}
