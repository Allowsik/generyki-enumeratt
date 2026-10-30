using CampusScheduler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusScheduler.Tests;

[TestClass]
public class RoomSchedulerTests
{
    private static readonly DateTimeOffset Day = new(2026, 11, 16, 0, 0, 0, TimeSpan.FromHours(1));
    private static DateTimeOffset At(int hour, int minute = 0) => Day.AddHours(hour).AddMinutes(minute);

    private static RoomScheduler Seeded()
    {
        RoomScheduler scheduler = new("North", 5);
        scheduler.TrySchedule("Algorithms", "A101", At(9), At(11), 20);
        scheduler.TrySchedule("Robotics", "B204", At(10), At(11), 12);
        scheduler.TrySchedule("Design", "A101", At(11), At(12), 30);
        return scheduler;
    }

    [TestMethod]
    public void TrySchedule_RejectsConflictingBooking()
    {
        RoomScheduler scheduler = Seeded();
        Assert.IsFalse(scheduler.TrySchedule("Conflict", "a101", At(10), At(12), 8));
        Assert.AreEqual(3, scheduler.Count);
    }

    [TestMethod]
    public void TrySchedule_AllowsAdjacentSlotsAndOtherRooms()
    {
        RoomScheduler scheduler = Seeded();
        Assert.IsTrue(scheduler.TrySchedule("Evening", "A101", At(12), At(13), 8));
        Assert.IsTrue(scheduler.TrySchedule("Parallel", "C303", At(9), At(11), 8));
    }

    [TestMethod]
    public void TryAdd_AlsoEnforcesConflictRule()
    {
        RoomScheduler scheduler = Seeded();
        Reservation overlapping = new("Extra", "A101", At(9), At(10), 5);
        Assert.IsFalse(scheduler.TryAdd(overlapping));
    }

    [TestMethod]
    public void TotalBookedTime_SumsDurations()
    {
        RoomScheduler scheduler = Seeded();
        Assert.AreEqual(TimeSpan.FromHours(4), scheduler.TotalBookedTime);
    }

    [TestMethod]
    public void EventsByRoom_CountsAndSortsRooms()
    {
        RoomScheduler scheduler = Seeded();
        var result = scheduler.EventsByRoom();
        CollectionAssert.AreEqual(new[] { "A101", "B204" }, result.Keys.ToArray());
        Assert.AreEqual(2, result["a101"]);
        Assert.AreEqual(1, result["B204"]);
    }

    [TestMethod]
    public void ForRoom_IsLazyAndCaseInsensitive()
    {
        RoomScheduler scheduler = Seeded();
        IEnumerable<Reservation> query = scheduler.ForRoom("a101");
        scheduler.TrySchedule("Night", "A101", At(13), At(14), 6);
        Assert.AreEqual(3, query.Count());
    }

    [TestMethod]
    public void ActiveAt_UsesHalfOpenTimeIntervals()
    {
        RoomScheduler scheduler = Seeded();
        CollectionAssert.AreEqual(
            new[] { "Design" },
            scheduler.ActiveAt(At(11)).Select(x => x.EventTitle).ToArray());
    }

    [TestMethod]
    public void RemoveFinished_DoesNotInvalidateEnumeration()
    {
        RoomScheduler scheduler = Seeded();
        Assert.AreEqual(2, scheduler.RemoveFinished(At(11)));
        Assert.AreEqual(1, scheduler.Count);
        Assert.AreEqual("Design", scheduler[0].EventTitle);
    }

    [TestMethod]
    public void MostPopular_UsesComparerWithoutChangingOriginalOrder()
    {
        RoomScheduler scheduler = Seeded();
        CollectionAssert.AreEqual(new[] { "Design", "Algorithms" },
            scheduler.MostPopular(2).Select(x => x.EventTitle).ToArray());
        Assert.AreEqual("Algorithms", scheduler[0].EventTitle);
    }

    [TestMethod]
    public void MostPopular_RejectsNegativeLimit() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Seeded().MostPopular(-1));

    [TestMethod]
    public void ExportCsv_EscapesCommasAndQuotes()
    {
        RoomScheduler scheduler = new("North", 2);
        scheduler.TrySchedule("Intro, \"C#\"", "A101", At(9), At(10), 8);
        StringAssert.Contains(scheduler.ExportCsv(), "\"Intro, \"\"C#\"\"\"");
    }

    [TestMethod]
    public void ToString_IncludesDynamicCountsAndDuration()
    {
        RoomScheduler scheduler = Seeded();
        Assert.AreEqual("North: 3/5 reservations, 240 booked minutes", scheduler.ToString());
    }
}
