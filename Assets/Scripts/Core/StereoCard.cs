namespace Stereopsis.Core
{
    /// <summary>
    /// The four stereo cards (CHAIN section 1). Each card is a physical
    /// object found in play and unlocks exactly one era. There is no card
    /// for the present: an empty slot shows the room as it is now.
    /// </summary>
    public enum StereoCard
    {
        None = 0,
        Card1922 = 1922,
        Card1861 = 1861,
        Card1774 = 1774,
        Card1698 = 1698,
    }

    public static class StereoCardExtensions
    {
        /// <summary>The era this card travels to. None maps to Present,
        /// which is also what an empty slot shows through the glass.</summary>
        public static Era EraOf(this StereoCard card) => card switch
        {
            StereoCard.Card1922 => Era.Y1922,
            StereoCard.Card1861 => Era.Y1861,
            StereoCard.Card1774 => Era.Y1774,
            StereoCard.Card1698 => Era.Y1698,
            _ => Era.Present,
        };
    }
}
