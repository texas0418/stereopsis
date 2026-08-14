using System;
using System.Collections.Generic;

namespace Stereopsis.Core
{
    /// <summary>
    /// The travel state machine (DECISIONS 49, CHAIN section 1).
    ///
    /// Invariant: CurrentEra is always either Era.Present or the era of
    /// the seated card. The seated card is the tether — you cannot keep
    /// standing in a year whose card has left the slot. Consequences:
    ///   - Eject() from anywhere snaps home to the present, instantly.
    ///   - Seating a different card while travelled passes through the
    ///     present first (the room flickers home during the swap).
    ///   - Seating alone never travels; Commit() — running the focus
    ///     rail to full — is the only way into a year.
    ///
    /// Pure C#, no UnityEngine. The view layer renders; this decides.
    /// </summary>
    public sealed class TimeState
    {
        readonly HashSet<StereoCard> _collected = new HashSet<StereoCard>();

        public Era CurrentEra { get; private set; } = Era.Present;
        public StereoCard SeatedCard { get; private set; } = StereoCard.None;

        /// <summary>(from, to). Fired on every era transition, including
        /// the snap home inside a card swap.</summary>
        public event Action<Era, Era> EraChanged;
        public event Action<StereoCard> CardCollected;
        public event Action<StereoCard> CardSeated;
        public event Action<StereoCard> CardEjected;

        public bool HasCard(StereoCard card) => _collected.Contains(card);
        public IReadOnlyCollection<StereoCard> CollectedCards => _collected;

        /// <summary>Add a found card to the collection. Idempotent; the
        /// event fires only on first collection.</summary>
        public bool CollectCard(StereoCard card)
        {
            if (card == StereoCard.None || !_collected.Add(card)) return false;
            CardCollected?.Invoke(card);
            return true;
        }

        /// <summary>Seat a collected card in the slot. Swapping while
        /// travelled snaps home first (tether rule).</summary>
        public bool SeatCard(StereoCard card)
        {
            if (card == StereoCard.None || !_collected.Contains(card)) return false;
            if (SeatedCard == card) return false;
            if (SeatedCard != StereoCard.None) Eject();
            SeatedCard = card;
            CardSeated?.Invoke(card);
            return true;
        }

        /// <summary>Run the rail to full focus: travel to the seated
        /// card's era. Fails with an empty slot or when already there.</summary>
        public bool Commit()
        {
            if (SeatedCard == StereoCard.None) return false;
            var target = SeatedCard.EraOf();
            if (target == CurrentEra) return false;
            var from = CurrentEra;
            CurrentEra = target;
            EraChanged?.Invoke(from, target);
            return true;
        }

        /// <summary>Pull the card. Always legal, always lands in the
        /// present. This is the panic button.</summary>
        public bool Eject()
        {
            if (SeatedCard == StereoCard.None) return false;
            var card = SeatedCard;
            SeatedCard = StereoCard.None;
            if (CurrentEra != Era.Present)
            {
                var from = CurrentEra;
                CurrentEra = Era.Present;
                EraChanged?.Invoke(from, Era.Present);
            }
            CardEjected?.Invoke(card);
            return true;
        }
    }
}
