using System.Collections;
using NUnit.Framework;
using Stereopsis;
using Stereopsis.Core;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// The whole game, played by machine: every beat of CHAIN.txt from the
/// pried board to the name on the wall. This is the project's spine
/// test — if it is green, the game is completable; if a change breaks
/// any lock, key, gate, or loop, it fails here first.
/// </summary>
public class WalkthroughTests
{
    Transform _room;
    EraDirector _dir;
    StereoscopeController _scope;

    GameObject G(string path)
    {
        var t = _room.Find(path);
        Assert.IsNotNull(t, "missing scene object: " + path);
        return t.gameObject;
    }

    Mechanism M(string path) => G(path).GetComponent<Mechanism>();

    void Seq(string path)
    {
        var s = G(path).GetComponent<BeatSequence>();
        Assert.IsNotNull(s, "no BeatSequence on " + path);
        for (int i = 0; i < 12 && !s.IsComplete; i++) s.TryAdvance(_dir.Bag);
        Assert.IsTrue(s.IsComplete, "sequence did not complete: " + path);
    }

    [UnityTest]
    public IEnumerator FullGame_FromBoardToName()
    {
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Stereopsis.unity",
            new UnityEngine.SceneManagement.LoadSceneParameters(
                UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
        yield return null;
        yield return null;

        GameFlags.Clear();
        _room = GameObject.Find("Room").transform;
        var systems = GameObject.Find("_Systems");
        _dir = systems.GetComponent<EraDirector>();
        _scope = systems.GetComponent<StereoscopeController>();
        var state = _dir.State;

        // ---- ACT 1: the present. Earn everything. ----
        Assert.IsTrue(_dir.Bag.Has("pry-bar"), "the appraiser brings a pry bar");
        Assert.IsFalse(_scope.HasDevice, "no device until it is found");

        Seq("Era_Present/LooseBoard_ByHearth");
        Assert.IsTrue(G("Era_Present/Strap").activeSelf, "strap revealed");
        Assert.IsTrue(G("Era_Present/RottedPurse").activeSelf, "purse revealed");
        Assert.IsTrue(_dir.Bag.TryAdd("rotted-purse"));
        G("Era_Present/RottedPurse").SetActive(false);

        Seq("Era_Present/RustedTin");
        Assert.IsTrue(G("Era_Present/Card_1922").activeSelf, "1922 card revealed");
        Assert.IsTrue(state.CollectCard(StereoCard.Card1922));
        G("Era_Present/Card_1922").SetActive(false);

        M("Shell/ChimneyBreast").TryOpen(_dir.Bag);
        Assert.IsTrue(G("Era_Present/OiledBundle").activeSelf, "bundle on the smoke ledge");
        _scope.GrantDevice(quiet: true);
        G("Era_Present/OiledBundle").SetActive(false);
        Assert.IsTrue(_scope.HasDevice);
        yield return null;

        // ---- first travel ----
        state.SeatCard(StereoCard.Card1922);
        Assert.IsTrue(state.Commit());
        Assert.AreEqual(Era.Y1922, state.CurrentEra);
        Assert.IsTrue(_room.Find("Era_1922").gameObject.activeSelf);
        Assert.IsFalse(_room.Find("Era_Present").gameObject.activeSelf);

        // ---- ACT 2: 1922, the journalist ----
        M("Era_1922/TypewriterCase").TryOpen(_dir.Bag);
        M("Era_1922/SeaChest_AsTable").TryOpen(_dir.Bag);
        M("Era_1922/Wallpaper_OverFirebox").TryOpen(_dir.Bag);
        Assert.IsTrue(G("Era_1922/CarbonFlimsy").activeSelf);
        Assert.IsTrue(G("Era_1922/JournalistFiles").activeSelf);
        Assert.IsTrue(G("Era_1922/ConfessionBundle").activeSelf);
        Assert.IsTrue(G("Era_1922/ChippedBrick_1922").activeSelf);
        M("Era_1922/ChippedBrick_1922").TryOpen(_dir.Bag);
        Assert.IsFalse(M("Era_1922/ChippedBrick_1922").IsOpen,
            "the 1922 brick must refuse forever");
        yield return null;

        // ---- ACT 3: the small loop, home for the brick ----
        Assert.IsTrue(state.Eject());
        Assert.AreEqual(Era.Present, state.CurrentEra);
        M("Era_Present/ChippedBrick_Present").TryOpen(_dir.Bag);
        Assert.IsTrue(M("Era_Present/ChippedBrick_Present").IsOpen,
            "a century of failed mortar gives up the brick");
        Assert.IsTrue(G("Era_Present/Card_1861").activeSelf);
        Assert.IsTrue(state.CollectCard(StereoCard.Card1861));
        G("Era_Present/Card_1861").SetActive(false);
        state.SeatCard(StereoCard.Card1861);
        Assert.IsTrue(state.Commit());
        Assert.AreEqual(Era.Y1861, state.CurrentEra);

        // ---- ACT 4: 1861, Abigail ----
        M("Era_1861/DeskDrawer").TryOpen(_dir.Bag);
        Assert.IsFalse(M("Era_1861/DeskDrawer").IsOpen, "drawer locked without the key");

        Seq("Era_1861/OddBottle");
        Assert.IsTrue(G("Era_1861/BrassKey").activeSelf, "key out of the silent bottle");
        Assert.IsTrue(_dir.Bag.TryAdd("brass-key"));
        G("Era_1861/BrassKey").SetActive(false);

        M("Era_1861/DeskDrawer").TryOpen(_dir.Bag);
        Assert.IsTrue(M("Era_1861/DeskDrawer").IsOpen);
        M("Era_1861/Workbox").TryOpen(_dir.Bag);
        M("Era_1861/ParlourStove_OnHearth").TryOpen(_dir.Bag);
        M("Era_1861/CardBox").TryOpen(_dir.Bag);
        Assert.IsTrue(G("Era_1861/Card_1774").activeSelf);
        Assert.IsTrue(G("Era_1861/SealedCompartment").activeSelf);
        M("Era_1861/SealedCompartment").TryOpen(_dir.Bag);
        Assert.IsFalse(M("Era_1861/SealedCompartment").IsOpen,
            "sealed until the seal exists");
        Assert.IsTrue(state.CollectCard(StereoCard.Card1774));
        G("Era_1861/Card_1774").SetActive(false);
        yield return null;

        // ---- ACT 5: 1774, Samuel ----
        state.SeatCard(StereoCard.Card1774);
        Assert.IsTrue(state.Commit());
        M("Era_1774/PlasterPatch_Wet").TryOpen(_dir.Bag);
        M("Era_1774/SlopeLid").TryOpen(_dir.Bag);
        Seq("Era_1774/ProspectDoor");
        M("Era_1774/WindowSeatBox").TryOpen(_dir.Bag);
        Assert.IsTrue(G("Era_1774/DeedBook").activeSelf);
        Assert.IsTrue(G("Era_1774/Broadside").activeSelf);
        Assert.IsTrue(G("Era_1774/SamuelsSeal").activeSelf);
        Assert.IsTrue(G("Era_1774/Docket").activeSelf);
        GameFlags.Set(G("Era_1774/Broadside").GetComponent<Readable>().SetsFlag);
        Assert.IsTrue(GameFlags.Has("knows-name"), "the broadside teaches the name");
        Assert.IsTrue(_dir.Bag.TryAdd("samuels-seal"));
        G("Era_1774/SamuelsSeal").SetActive(false);
        Assert.IsTrue(_dir.Bag.IsFull, "bag exactly full at the seal moment");

        // ---- ACT 6: the large loop, back to 1861 with the seal ----
        state.SeatCard(StereoCard.Card1861);
        Assert.IsTrue(state.Commit());
        M("Era_1861/SealedCompartment").TryOpen(_dir.Bag);
        Assert.IsTrue(M("Era_1861/SealedCompartment").IsOpen,
            "her great-grandfather's die opens her lock");
        Assert.IsTrue(G("Era_1861/Card_1698").activeSelf);
        Assert.IsTrue(state.CollectCard(StereoCard.Card1698));
        G("Era_1861/Card_1698").SetActive(false);
        yield return null;

        // ---- ACT 7: 1698, confirmation ----
        state.SeatCard(StereoCard.Card1698);
        Assert.IsTrue(state.Commit());
        M("Era_1698/FreshBoard").TryOpen(_dir.Bag);
        M("Era_1698/SeaChest_Recess").TryOpen(_dir.Bag);
        M("Era_1698/Strongbox_OnChest").TryOpen(_dir.Bag);
        M("Era_1698/Hearthstone_1698").TryOpen(_dir.Bag);
        Assert.IsTrue(G("Era_1698/FreshPurse").activeSelf);
        Assert.IsTrue(G("Era_1698/VanesCoat").activeSelf);
        Assert.IsTrue(G("Era_1698/InsuranceReceipts").activeSelf);
        Assert.IsTrue(G("Era_1698/WhatIsUnderTheStone").activeSelf, "Merrick");

        // ---- ACT 8: the ending ----
        Assert.IsTrue(state.Eject());
        Assert.AreEqual(Era.Present, state.CurrentEra);
        M("Era_Present/NamePatch").TryOpen(_dir.Bag);
        Assert.IsTrue(M("Era_Present/NamePatch").IsOpen, "the name can be written");
        Assert.IsTrue(G("Era_Present/TheNameWritten").activeSelf, "JONAS REED");
        yield return null;
    }
}
