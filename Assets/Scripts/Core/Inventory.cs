using System;
using System.Collections.Generic;

namespace Stereopsis.Core
{
    /// <summary>
    /// The hand inventory (DECISIONS 11): tiny, lock-and-key, never a
    /// resource pool. Items are string ids owned by the puzzle data.
    /// Cards are not items — they live in TimeState. Items cross eras
    /// freely (DECISIONS 12).
    /// </summary>
    public sealed class Inventory
    {
        public const int Capacity = 4;

        readonly List<string> _items = new List<string>();

        public event Action<string> ItemAdded;
        public event Action<string> ItemRemoved;

        public IReadOnlyList<string> Items => _items;
        public bool IsFull => _items.Count >= Capacity;
        public bool Has(string id) => _items.Contains(id);

        public bool TryAdd(string id)
        {
            if (string.IsNullOrEmpty(id) || IsFull || _items.Contains(id)) return false;
            _items.Add(id);
            ItemAdded?.Invoke(id);
            return true;
        }

        public bool Remove(string id)
        {
            if (!_items.Remove(id)) return false;
            ItemRemoved?.Invoke(id);
            return true;
        }
    }
}
