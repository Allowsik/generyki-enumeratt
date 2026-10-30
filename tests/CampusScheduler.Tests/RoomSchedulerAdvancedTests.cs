using CampusScheduler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusScheduler.Tests;

[TestClass]
public class RoomSchedulerAdvancedTests
{
    private static readonly DateTimeOffset Day = new(2026, 11, 16, 0, 0, 0, TimeSpan.FromHours(1));
    private static DateTimeOffset At(int hour, int minute = 0) => Day.AddHours(hour).AddMinutes(minute);

    private static RoomScheduler Seed()
    {
        RoomScheduler scheduler = new("South", 10);
        scheduler.TrySchedule("Math", "A", At(9), At(10), 30);
        scheduler.TrySchedule("Algorithms", "A", At(12), At(13, 30), 20);
        scheduler.TrySchedule("Chemistry", "B", At(9), At(11), 12);
        return scheduler;
    }

    [TestMethod]
    public void IsRoomFree_HandlesTouchingTimeIntervals()
    {
        RoomScheduler scheduler = Seed();
        Assert.IsTrue(scheduler.IsRoomFree("a", At(10), At(12)));
        Assert.IsFalse(scheduler.IsRoomFree("A", At(9), At(9, 30)));
        Assert.IsTrue(scheduler.IsRoomFree("C", At(9), At(13)));
    }

    [TestMethod]
    public void EventsOn_UsesQuerySyntaxAndIsDeferred()
    {
        RoomScheduler scheduler = Seed();
        IEnumerable<Reservation> query = scheduler.EventsOn(new DateOnly(2026, 11, 17));
        scheduler.TrySchedule("Tomorrow", "D", At(9).AddDays(1), At(10).AddDays(1), 6);
        CollectionAssert.AreEqual(new[] { "Tomorrow" }, query.Select(r => r.EventTitle).ToArray());
    }

    [TestMethod]
    public void AttendeesByRoom_GroupsCaseInsensitively()
    {
        RoomScheduler scheduler = Seed();
        Assert.AreEqual(62, scheduler.TotalAttendees);
        var result = scheduler.AttendeesByRoom();
        Assert.AreEqual(50, result["a"]);
        Assert.AreEqual(12, result["B"]);
    }

    [TestMethod]
    public void FreeSlots_ClipsReservationsAndRespectsMinimumDuration()
    {
        RoomScheduler scheduler = Seed();
        TimeSlot[] gaps = scheduler.FreeSlots("A", At(8), At(15), TimeSpan.FromHours(1)).ToArray();
        Assert.AreEqual(3, gaps.Length);
        Assert.AreEqual(new TimeSlot(At(8), At(9)), gaps[0]);
        Assert.AreEqual(new TimeSlot(At(10), At(12)), gaps[1]);
        Assert.AreEqual(new TimeSlot(At(13, 30), At(15)), gaps[2]);
    }

    [TestMethod]
    public void FreeSlots_IsLazyAndReflectsAddedReservations()
    {
        RoomScheduler scheduler = Seed();
        IEnumerable<TimeSlot> query = scheduler.FreeSlots("C", At(8), At(12), TimeSpan.FromMinutes(30));
        scheduler.TrySchedule("New", "C", At(9), At(10), 10);
        Assert.AreEqual(2, query.Count());
    }

    [TestMethod]
    public void FreeSlots_FullyBookedProducesEmptyResult()
    {
        RoomScheduler scheduler = Seed();
        Assert.AreEqual(0, scheduler.FreeSlots("A", At(9), At(10), TimeSpan.FromMinutes(1)).Count());
    }

    [TestMethod]
    public void FreeSlots_ValidatesParameters()
    {
        RoomScheduler scheduler = Seed();
        Assert.ThrowsException<ArgumentException>(() => scheduler.FreeSlots("A", At(11), At(10), TimeSpan.FromMinutes(1)));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => scheduler.FreeSlots("A", At(9), At(10), TimeSpan.Zero));
    }

    [TestMethod]
    public void CancelAndUndo_RestoreLastReservation()
    {
        RoomScheduler scheduler = Seed();
        Reservation original = scheduler[0];
        Assert.IsTrue(scheduler.Cancel(original));
        Assert.AreEqual(1, scheduler.UndoCount);
        Assert.IsTrue(scheduler.TryUndoCancellation());
        Assert.AreEqual(0, scheduler.UndoCount);
        Assert.AreEqual(3, scheduler.Count);
    }

    [TestMethod]
    public void Undo_RefusesConflictingRestoreAndKeepsHistory()
    {
        RoomScheduler scheduler = Seed();
        Reservation original = scheduler[0];
        scheduler.Cancel(original);
        scheduler.TrySchedule("Replacement", "A", At(9), At(10), 20);
        Assert.IsFalse(scheduler.TryUndoCancellation());
        Assert.AreEqual(1, scheduler.UndoCount);
    }

    [TestMethod]
    public void BusiestRooms_RanksByBookedDuration()
    {
        RoomScheduler scheduler = Seed();
        RoomUsage[] mostBusy = scheduler.BusiestRooms(2).ToArray();
        Assert.AreEqual("A", mostBusy[0].Room);
        Assert.AreEqual(TimeSpan.FromHours(2.5), mostBusy[0].TotalDuration);
        Assert.AreEqual("B", mostBusy[1].Room);
    }

    [TestMethod]
    public void AddRange_CannotBypassSchedulerConflictCheck()
    {
        RoomScheduler scheduler = Seed();
        Reservation conflict = new("Overlap", "A", At(9), At(10), 5);
        Reservation valid = new("Vacant", "C", At(9), At(10), 5);
        Assert.AreEqual(1, scheduler.AddRange([conflict, valid]));
        Assert.AreEqual(4, scheduler.Count);
    }
}
