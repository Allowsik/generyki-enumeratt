using CampusScheduler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusScheduler.Tests;

[TestClass]
public class ReservationWaitlistTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 16, 9, 0, 0, TimeSpan.FromHours(1));
    private static Reservation Make(string title, string room) =>
        new(title, room, Start, Start.AddHours(1), 10);

    [TestMethod]
    public void Process_AddsAvailableReservationsInFifoOrder()
    {
        RoomScheduler scheduler = new("North", 3);
        ReservationWaitlist waiting = new();
        waiting.Enqueue(Make("A", "A101"));
        waiting.Enqueue(Make("B", "B204"));
        List<Reservation> accepted = waiting.Process(scheduler);
        CollectionAssert.AreEqual(new[] { "A", "B" }, accepted.Select(r => r.EventTitle).ToArray());
        Assert.AreEqual(0, waiting.Count);
    }

    [TestMethod]
    public void Process_LeavesConflictsWaiting()
    {
        RoomScheduler scheduler = new("North", 3);
        Reservation occupied = Make("Busy", "A101");
        scheduler.TryAdd(occupied);
        ReservationWaitlist waiting = new();
        waiting.Enqueue(Make("Later", "A101"));
        waiting.Enqueue(Make("Free", "B204"));
        Assert.AreEqual(1, waiting.Process(scheduler).Count);
        Assert.AreEqual("Later", waiting.Single().EventTitle);
        scheduler.Remove(occupied);
        Assert.AreEqual(1, waiting.Process(scheduler).Count);
        Assert.AreEqual(0, waiting.Count);
    }

    [TestMethod]
    public void Process_RespectsCapacity()
    {
        RoomScheduler scheduler = new("North", 1);
        ReservationWaitlist waiting = new();
        waiting.Enqueue(Make("First", "A101"));
        waiting.Enqueue(Make("Second", "B204"));
        Assert.AreEqual(1, waiting.Process(scheduler).Count);
        Assert.AreEqual(1, waiting.Count);
    }

    [TestMethod]
    public void Waitlist_ImplementsGenericAndNonGenericEnumeration()
    {
        ReservationWaitlist waiting = new();
        waiting.Enqueue(Make("First", "A101"));
        System.Collections.IEnumerable nonGeneric = waiting;
        Assert.AreEqual(1, nonGeneric.Cast<Reservation>().Count());
    }
}
