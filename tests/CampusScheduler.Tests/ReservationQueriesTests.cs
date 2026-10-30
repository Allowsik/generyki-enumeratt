using CampusScheduler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusScheduler.Tests;

[TestClass]
public class ReservationQueriesTests
{
    private static readonly DateTimeOffset Day = new(2026, 11, 16, 0, 0, 0, TimeSpan.FromHours(1));

    private static List<Reservation> Data() =>
    [
        new("Math", "A", Day.AddHours(9), Day.AddHours(11), 10),
        new("math", "b", Day.AddHours(12), Day.AddHours(13), 30),
        new("Robotics", "A", Day.AddHours(14), Day.AddHours(15), 20)
    ];

    [TestMethod]
    public void LongerThan_FiltersAndDefersEvaluation()
    {
        List<Reservation> source = Data();
        IEnumerable<Reservation> query = source.LongerThan(TimeSpan.FromHours(1.5));
        source.Add(new Reservation("Workshop", "A", Day.AddHours(16), Day.AddHours(18), 6));
        CollectionAssert.AreEqual(new[] { "Math", "Workshop" }, query.Select(r => r.EventTitle).ToArray());
    }

    [TestMethod]
    public void SearchTitle_IsCaseInsensitive()
    {
        CollectionAssert.AreEqual(new[] { "Math", "math" },
            Data().SearchTitle("mAt").Select(r => r.EventTitle).ToArray());
    }

    [TestMethod]
    public void DistinctTitles_CollapsesCaseVariantsAndSorts()
    {
        CollectionAssert.AreEqual(new[] { "Math", "Robotics" }, Data().DistinctTitles().ToArray());
    }

    [TestMethod]
    public void UsageByRoom_GroupingAddsDurationsAndAttendees()
    {
        List<RoomUsage> report = Data().UsageByRoom();
        Assert.AreEqual(2, report.Count);
        Assert.AreEqual("A", report[0].Room);
        Assert.AreEqual(2, report[0].EventCount);
        Assert.AreEqual(TimeSpan.FromHours(3), report[0].TotalDuration);
        Assert.AreEqual(30, report[0].Attendees);
    }

    [TestMethod]
    public void ParticipantHours_UsesWeightedSum()
    {
        // 2h * 10 osób + 1h * 30 osób + 1h * 20 osób = 70 uczestnikogodzin.
        Assert.AreEqual(70d, Data().ParticipantHours(), 0.00001);
    }

    [TestMethod]
    public void AttendeesByDay_UsesDateOnlyAsSortedKey()
    {
        SortedDictionary<DateOnly, int> result = Data().AttendeesByDay();
        Assert.AreEqual(60, result[new DateOnly(2026, 11, 16)]);
    }

    [TestMethod]
    public void RunningAttendance_UsesYieldAndCumulativeSum()
    {
        var running = Data().RunningAttendance().ToArray();
        CollectionAssert.AreEqual(new[] { 10, 40, 60 },
            running.Select(x => x.CumulativeAttendees).ToArray());
    }

    [TestMethod]
    public void QueryExtensions_RejectNullOrInvalidArguments()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Data().LongerThan(TimeSpan.FromHours(-1)));
        Assert.ThrowsException<ArgumentException>(() => Data().SearchTitle("  "));
    }
}
