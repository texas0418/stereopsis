using System.Collections.Generic;
using NUnit.Framework;
using Stereopsis.Core;

public class TimeStateTests
{
    TimeState _t;
    List<(Era from, Era to)> _hops;

    [SetUp]
    public void SetUp()
    {
        _t = new TimeState();
        _hops = new List<(Era, Era)>();
        _t.EraChanged += (from, to) => _hops.Add((from, to));
    }

    [Test]
    public void StartsInPresentWithEmptySlot()
    {
        Assert.AreEqual(Era.Present, _t.CurrentEra);
        Assert.AreEqual(StereoCard.None, _t.SeatedCard);
    }

    [Test]
    public void CannotSeatAnUncollectedCard()
    {
        Assert.IsFalse(_t.SeatCard(StereoCard.Card1922));
        Assert.AreEqual(StereoCard.None, _t.SeatedCard);
    }

    [Test]
    public void SeatingAloneNeverTravels()
    {
        _t.CollectCard(StereoCard.Card1922);
        Assert.IsTrue(_t.SeatCard(StereoCard.Card1922));
        Assert.AreEqual(Era.Present, _t.CurrentEra);
        Assert.IsEmpty(_hops);
    }

    [Test]
    public void SeatThenCommitTravels()
    {
        _t.CollectCard(StereoCard.Card1922);
        _t.SeatCard(StereoCard.Card1922);
        Assert.IsTrue(_t.Commit());
        Assert.AreEqual(Era.Y1922, _t.CurrentEra);
        CollectionAssert.AreEqual(new[] { (Era.Present, Era.Y1922) }, _hops);
    }

    [Test]
    public void CommitWithEmptySlotFails()
    {
        Assert.IsFalse(_t.Commit());
        Assert.IsEmpty(_hops);
    }

    [Test]
    public void CommitWhenAlreadyThereIsANoOp()
    {
        _t.CollectCard(StereoCard.Card1861);
        _t.SeatCard(StereoCard.Card1861);
        _t.Commit();
        Assert.IsFalse(_t.Commit());
        Assert.AreEqual(1, _hops.Count);
    }

    [Test]
    public void EjectSnapsHomeFromAnywhere()
    {
        _t.CollectCard(StereoCard.Card1774);
        _t.SeatCard(StereoCard.Card1774);
        _t.Commit();

        Assert.IsTrue(_t.Eject());
        Assert.AreEqual(Era.Present, _t.CurrentEra);
        Assert.AreEqual(StereoCard.None, _t.SeatedCard);
        Assert.AreEqual((Era.Y1774, Era.Present), _hops[_hops.Count - 1]);
    }

    [Test]
    public void EjectWithEmptySlotFails()
    {
        Assert.IsFalse(_t.Eject());
    }

    [Test]
    public void SwappingCardsWhileTravelledPassesThroughPresent()
    {
        // The seal moment (CHAIN section 2): standing in 1774 with the
        // seal in hand, seat the 1861 card to go open Abigail's box.
        _t.CollectCard(StereoCard.Card1774);
        _t.CollectCard(StereoCard.Card1861);
        _t.SeatCard(StereoCard.Card1774);
        _t.Commit();
        _hops.Clear();

        Assert.IsTrue(_t.SeatCard(StereoCard.Card1861));
        Assert.AreEqual(Era.Present, _t.CurrentEra); // tether rule
        Assert.IsTrue(_t.Commit());
        Assert.AreEqual(Era.Y1861, _t.CurrentEra);
        CollectionAssert.AreEqual(
            new[] { (Era.Y1774, Era.Present), (Era.Present, Era.Y1861) },
            _hops);
    }

    [Test]
    public void CollectingIsIdempotent()
    {
        int fired = 0;
        _t.CardCollected += _ => fired++;
        Assert.IsTrue(_t.CollectCard(StereoCard.Card1698));
        Assert.IsFalse(_t.CollectCard(StereoCard.Card1698));
        Assert.AreEqual(1, fired);
        Assert.IsTrue(_t.HasCard(StereoCard.Card1698));
    }

    [Test]
    public void ReseatingTheSameCardIsANoOp()
    {
        _t.CollectCard(StereoCard.Card1922);
        _t.SeatCard(StereoCard.Card1922);
        Assert.IsFalse(_t.SeatCard(StereoCard.Card1922));
    }

    [Test]
    public void CollectingNoneFails()
    {
        Assert.IsFalse(_t.CollectCard(StereoCard.None));
    }
}
