using CampusScheduler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusScheduler.Tests;

[TestClass]
public class CapacityCollectionTests
{
    [TestMethod]
    public void Constructor_RejectsInvalidCapacity() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CapacityCollection<int>(0));

    [TestMethod]
    public void TryAdd_RespectsCapacity()
    {
        CapacityCollection<int> store = new(2);
        Assert.IsTrue(store.TryAdd(4));
        Assert.IsTrue(store.TryAdd(7));
        Assert.IsFalse(store.TryAdd(9));
        Assert.IsTrue(store.IsFull);
        Assert.AreEqual(2, store.Count);
    }

    [TestMethod]
    public void Enumeration_PreservesInsertionOrder()
    {
        CapacityCollection<int> store = new(3);
        store.TryAdd(8);
        store.TryAdd(2);
        store.TryAdd(5);
        CollectionAssert.AreEqual(new[] { 8, 2, 5 }, store.ToArray());
        Assert.AreEqual(2, store[1]);
    }

    [TestMethod]
    public void Remove_OnlyRemovesFirstOccurrence()
    {
        CapacityCollection<int> store = new(4);
        store.TryAdd(2);
        store.TryAdd(4);
        store.TryAdd(2);
        Assert.IsTrue(store.Remove(2));
        CollectionAssert.AreEqual(new[] { 4, 2 }, store.ToArray());
        Assert.IsFalse(store.Remove(99));
    }

    [TestMethod]
    public void Max_FindsLargestValue()
    {
        CapacityCollection<int> store = new(3);
        store.TryAdd(4);
        store.TryAdd(11);
        store.TryAdd(8);
        Assert.AreEqual(11, store.Max());
    }

    [TestMethod]
    public void Max_ThrowsWhenEmpty() =>
        Assert.ThrowsException<InvalidOperationException>(() => new CapacityCollection<int>(2).Max());

    [TestMethod]
    public void Sorted_DoesNotChangeStoredOrder()
    {
        CapacityCollection<int> store = new(3);
        store.TryAdd(9);
        store.TryAdd(1);
        store.TryAdd(5);
        CollectionAssert.AreEqual(new[] { 1, 5, 9 }, store.Sorted().ToArray());
        CollectionAssert.AreEqual(new[] { 9, 1, 5 }, store.ToArray());
    }

    [TestMethod]
    public void Find_IsLazy()
    {
        CapacityCollection<int> store = new(3);
        store.TryAdd(4);
        IEnumerable<int> query = store.Find(x => x % 2 == 0);
        store.TryAdd(6);
        CollectionAssert.AreEqual(new[] { 4, 6 }, query.ToArray());
    }

    [TestMethod]
    public void Snapshot_IsIndependent()
    {
        CapacityCollection<int> store = new(2);
        store.TryAdd(3);
        IReadOnlyList<int> snapshot = store.Snapshot();
        store.TryAdd(7);
        CollectionAssert.AreEqual(new[] { 3 }, snapshot.ToArray());
    }

    [TestMethod]
    public void RemoveWhere_DeletesMatches()
    {
        CapacityCollection<int> store = new(5);
        foreach (int i in new[] { 1, 2, 3, 4, 5 }) store.TryAdd(i);
        Assert.AreEqual(2, store.RemoveWhere(x => x % 2 == 0));
        CollectionAssert.AreEqual(new[] { 1, 3, 5 }, store.ToArray());
    }
}
