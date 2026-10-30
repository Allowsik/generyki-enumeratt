using CampusScheduler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusScheduler.Tests;

[TestClass]
public class RoomDirectoryTests
{
    private static readonly DateTimeOffset Day = new(2026, 11, 16, 0, 0, 0, TimeSpan.FromHours(1));
    private static DateTimeOffset At(int hour) => Day.AddHours(hour);

    [TestMethod]
    public void RoomDefinition_ValidatesData()
    {
        Assert.ThrowsException<ArgumentException>(() => new RoomDefinition(" ", 3));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new RoomDefinition("A", 0));
        Assert.ThrowsException<ArgumentException>(() => new RoomDefinition("A", 4, [" "]));
    }

    [TestMethod]
    public void Equipment_UsesSortedSetAndIgnoresCase()
    {
        RoomDefinition room = new("Lab", 20, ["Wi-Fi", "projector", "PROJECTOR"]);
        Assert.AreEqual(2, room.Equipment.Count);
        CollectionAssert.AreEqual(new[] { "projector", "Wi-Fi" }, room.Equipment.ToArray());
        Assert.IsTrue(room.HasEquipment("PROJECTOR"));
    }

    [TestMethod]
    public void Register_DeduplicatesCaseInsensitively()
    {
        RoomDirectory directory = new();
        Assert.IsTrue(directory.Register(new RoomDefinition("B2", 30)));
        Assert.IsFalse(directory.Register(new RoomDefinition("b2", 45)));
        Assert.AreEqual(1, directory.Count);
        Assert.AreEqual(30, directory.Find("b2")!.Seats);
    }

    [TestMethod]
    public void Iteration_IsInSortedCodeOrder()
    {
        RoomDirectory directory = new();
        directory.Register(new RoomDefinition("B2", 20));
        directory.Register(new RoomDefinition("A1", 10));
        CollectionAssert.AreEqual(new[] { "A1", "B2" }, directory.Select(x => x.Code).ToArray());
    }

    [TestMethod]
    public void RemoveAndFind_WorkForMissingAndExistingRooms()
    {
        RoomDirectory directory = new();
        directory.Register(new RoomDefinition("A", 10));
        Assert.IsNull(directory.Find("B"));
        Assert.IsTrue(directory.Remove("a"));
        Assert.IsFalse(directory.Remove("a"));
    }

    [TestMethod]
    public void FindAvailable_AppliesSeatsEquipmentAndConflict()
    {
        RoomDirectory directory = new();
        directory.Register(new RoomDefinition("A", 40, ["Projector"]));
        directory.Register(new RoomDefinition("B", 25, ["Projector"]));
        directory.Register(new RoomDefinition("C", 50, ["Wi-Fi"]));
        directory.Register(new RoomDefinition("D", 60, ["Projector"]));

        RoomScheduler scheduler = new("Main", 5);
        scheduler.TrySchedule("Busy", "A", At(9), At(11), 20);
        CollectionAssert.AreEqual(new[] { "D" },
            directory.FindAvailable(scheduler, At(10), At(12), 30, "projector")
                .Select(x => x.Code).ToArray());
    }

    [TestMethod]
    public void FindAvailable_IsDeferred()
    {
        RoomDirectory directory = new();
        directory.Register(new RoomDefinition("A", 30));
        RoomScheduler scheduler = new("Main", 3);
        IEnumerable<RoomDefinition> query = directory.FindAvailable(scheduler, At(9), At(10), 10);
        scheduler.TrySchedule("New", "A", At(9), At(10), 10);
        Assert.AreEqual(0, query.Count());
    }

    [TestMethod]
    public void FindAvailable_ValidatesBeforeEnumeration()
    {
        RoomDirectory directory = new();
        RoomScheduler scheduler = new("Main", 1);
        Assert.ThrowsException<ArgumentException>(() => directory.FindAvailable(scheduler, At(10), At(9), 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => directory.FindAvailable(scheduler, At(9), At(10), 0));
    }
}
