using NUnit.Framework;
using Stereopsis.Core;

public class InventoryTests
{
    [Test]
    public void CapacityIsEnforcedAndFreedByRemoval()
    {
        var inv = new Inventory();
        Assert.IsTrue(inv.TryAdd("brass-key"));
        Assert.IsTrue(inv.TryAdd("unburnt-corner"));
        Assert.IsTrue(inv.TryAdd("samuels-seal"));
        Assert.IsTrue(inv.TryAdd("pry-bar"));
        Assert.IsTrue(inv.IsFull);
        Assert.IsFalse(inv.TryAdd("one-too-many"));

        Assert.IsTrue(inv.Remove("brass-key"));
        Assert.IsTrue(inv.TryAdd("one-more-fits-now"));
    }

    [Test]
    public void NoDuplicatesNoEmptyIds()
    {
        var inv = new Inventory();
        Assert.IsTrue(inv.TryAdd("ledger-page"));
        Assert.IsFalse(inv.TryAdd("ledger-page"));
        Assert.IsFalse(inv.TryAdd(""));
        Assert.IsFalse(inv.TryAdd(null));
    }

    [Test]
    public void RemovingAbsentItemFails()
    {
        var inv = new Inventory();
        Assert.IsFalse(inv.Remove("ghost"));
    }
}
