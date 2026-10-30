using CampusScheduler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusScheduler.Tests;

[TestClass]
public class DirectoryAnalyticsTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 16, 9, 0, 0, TimeSpan.FromHours(1));

    [TestMethod]
    public void Overview_UsesLeftJoinAndIncludesEmptyRooms()
    {
        RoomDirectory rooms = new();
        rooms.Register(new RoomDefinition("A", 30));
        rooms.Register(new RoomDefinition("B", 50));
        RoomScheduler schedule = new("Main", 3);
        schedule.TrySchedule("Workshop", "A", At, At.AddHours(2), 18);
        List<RoomOverview> report = rooms.Overview(schedule);
        Assert.AreEqual(2, report.Count);
        Assert.AreEqual(1, report[0].ReservationCount);
        Assert.AreEqual(TimeSpan.FromHours(2), report[0].BookedTime);
        Assert.AreEqual(0, report[1].ReservationCount);
    }

    [TestMethod]
    public void EquipmentIndex_UsesSelectManyAndGroupBy()
    {
        RoomDirectory rooms = new();
        rooms.Register(new RoomDefinition("B", 10, ["Wi-Fi", "Projector"]));
        rooms.Register(new RoomDefinition("A", 10, ["wi-fi"]));
        SortedDictionary<string, List<string>> index = rooms.EquipmentIndex();
        CollectionAssert.AreEqual(new[] { "A", "B" }, index["WI-FI"]);
        CollectionAssert.AreEqual(new[] { "B" }, index["Projector"]);
    }

    [TestMethod]
    public void SharedRooms_UsesSetIntersectionIgnoringCase()
    {
        Reservation a = new("A", "room-1", At, At.AddHours(1), 5);
        Reservation b = new("B", "Room-2", At, At.AddHours(1), 5);
        Reservation c = new("C", "ROOM-1", At, At.AddHours(1), 5);
        CollectionAssert.AreEqual(new[] { "room-1" }, new[] { a, b }.SharedRooms([c]).ToArray());
    }
}
