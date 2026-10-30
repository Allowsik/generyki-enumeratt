using CampusScheduler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusScheduler.Tests;

[TestClass]
public class CapacityCollectionAdvancedTests
{
    [TestMethod]
    public void MinAndMax_UseIComparable()
    {
        CapacityCollection<int> values = new(5);
        values.AddRange([7, 2, 15, 4]);
        Assert.AreEqual(2, values.Min());
        Assert.AreEqual(15, values.Max());
    }

    [TestMethod]
    public void Min_OnEmptyCollection_Throws() =>
        Assert.ThrowsException<InvalidOperationException>(() => new CapacityCollection<int>(2).Min());

    [TestMethod]
    public void AddRange_RespectsCapacityAndInsertionOrder()
    {
        CapacityCollection<int> values = new(3);
        Assert.AreEqual(3, values.AddRange([7, 2, 15, 4]));
        CollectionAssert.AreEqual(new[] { 7, 2, 15 }, values.ToArray());
    }

    [TestMethod]
    public void AddRange_OnItself_UsesSnapshot()
    {
        CapacityCollection<int> values = new(5);
        values.AddRange([1, 2]);
        Assert.AreEqual(2, values.AddRange(values));
        CollectionAssert.AreEqual(new[] { 1, 2, 1, 2 }, values.ToArray());
    }

    [TestMethod]
    public void ContainsAndIndexOf_UseEquality()
    {
        CapacityCollection<string> values = new(3);
        values.AddRange(["A", "B"]);
        Assert.IsTrue(values.Contains("B"));
        Assert.AreEqual(1, values.IndexOf("B"));
        Assert.AreEqual(-1, values.IndexOf("b"));
    }

    [TestMethod]
    public void RemoveAtAndClear_UpdateCount()
    {
        CapacityCollection<int> values = new(3);
        values.AddRange([1, 2, 3]);
        values.RemoveAt(1);
        CollectionAssert.AreEqual(new[] { 1, 3 }, values.ToArray());
        values.Clear();
        Assert.IsTrue(values.IsEmpty);
    }

    [TestMethod]
    public void TryTakeLast_RemovesInReverseInsertionOrder()
    {
        CapacityCollection<int> values = new(3);
        values.AddRange([10, 20]);
        Assert.IsTrue(values.TryTakeLast(out int result));
        Assert.AreEqual(20, result);
        Assert.IsTrue(values.TryTakeLast(out result));
        Assert.AreEqual(10, result);
        Assert.IsFalse(values.TryTakeLast(out _));
    }

    [TestMethod]
    public void Window_IsLazyAndHonorsSkipAndTake()
    {
        CapacityCollection<int> values = new(6);
        values.AddRange([1, 2, 3]);
        IEnumerable<int> window = values.Window(2, 2);
        values.TryAdd(4);
        CollectionAssert.AreEqual(new[] { 3, 4 }, window.ToArray());
        Assert.AreEqual(0, values.Window(1, 0).Count());
    }

    [TestMethod]
    public void EveryNth_YieldsPositionsZeroTwoFour()
    {
        CapacityCollection<int> values = new(6);
        values.AddRange([11, 22, 33, 44, 55, 66]);
        CollectionAssert.AreEqual(new[] { 11, 33, 55 }, values.EveryNth(2).ToArray());
    }

    [TestMethod]
    public void WindowAndEveryNth_RejectInvalidParameters()
    {
        CapacityCollection<int> values = new(3);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => values.Window(-1, 2));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => values.Window(0, -2));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => values.EveryNth(0));
    }

    [TestMethod]
    public void Snapshot_DoesNotChangeAfterMutation()
    {
        CapacityCollection<int> values = new(3);
        values.AddRange([3, 4]);
        IReadOnlyList<int> before = values.Snapshot();
        values.Remove(3);
        CollectionAssert.AreEqual(new[] { 3, 4 }, before.ToArray());
    }

    [TestMethod]
    public void NonGenericIEnumerable_CanBeEnumerated()
    {
        CapacityCollection<int> values = new(3);
        values.AddRange([2, 3]);
        System.Collections.IEnumerable sequence = values;
        List<int> found = new();
        foreach (object item in sequence) found.Add((int)item);
        CollectionAssert.AreEqual(new[] { 2, 3 }, found);
    }
}
