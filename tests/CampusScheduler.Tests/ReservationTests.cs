using CampusScheduler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusScheduler.Tests;

[TestClass]
public class ReservationTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 16, 9, 0, 0, TimeSpan.FromHours(1));

    private static Reservation Make(string title = "Algorithms", int participants = 12, int startOffset = 0) =>
        new(title, "A101", Start.AddHours(startOffset), Start.AddHours(startOffset + 1), participants);

    [TestMethod]
    public void Constructor_RejectsBlankTitle() =>
        Assert.ThrowsException<ArgumentException>(() => new Reservation("  ", "A101", Start, Start.AddHours(1), 5));

    [TestMethod]
    public void Constructor_RejectsBlankRoom() =>
        Assert.ThrowsException<ArgumentException>(() => new Reservation("Workshop", "  ", Start, Start.AddHours(1), 5));

    [TestMethod]
    public void Constructor_RejectsInvalidInterval() =>
        Assert.ThrowsException<ArgumentException>(() => new Reservation("Workshop", "A101", Start, Start, 5));

    [TestMethod]
    public void Constructor_RejectsNonpositiveParticipants() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Reservation("Workshop", "A101", Start, Start.AddHours(1), 0));

    [TestMethod]
    public void CompareTo_UsesChronologicalOrder()
    {
        Assert.IsTrue(Make(startOffset: 0).CompareTo(Make(startOffset: 1)) < 0);
        Assert.IsTrue(Make().CompareTo(null) > 0);
    }

    [TestMethod]
    public void PopularityComparer_SortsLargestEventsFirst()
    {
        Reservation[] values = { Make("Small", 3), Make("Big", 30), Make("Middle", 12) };
        Array.Sort(values, new ReservationPopularityComparer());
        CollectionAssert.AreEqual(new[] { "Big", "Middle", "Small" }, values.Select(x => x.EventTitle).ToArray());
    }
}
