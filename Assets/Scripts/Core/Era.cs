namespace Stereopsis.Core
{
    /// <summary>
    /// The five states of the room (DECISIONS 31). Values are the literal
    /// year for the four historical eras; Present is 0 so that
    /// default(Era) == Era.Present — home is the default, everywhere.
    /// </summary>
    public enum Era
    {
        Present = 0,
        Y1922 = 1922,
        Y1861 = 1861,
        Y1774 = 1774,
        Y1698 = 1698,
    }
}
