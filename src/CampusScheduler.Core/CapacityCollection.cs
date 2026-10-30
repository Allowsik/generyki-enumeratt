using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CampusScheduler.Core;

/// <summary>
/// Kolekcja generyczna o ustalonej maksymalnej pojemności.
/// Zachowuje kolejność wstawiania, a do wyszukiwania maksimum wymaga IComparable{T}.
/// Nie implementuje ICollection{T}: jego Add ma inny kontrakt (void zamiast bool).
/// </summary>
public class CapacityCollection<T> : IEnumerable<T> where T : IComparable<T>
{
    // Pole pozostaje prywatne: kod zewnętrzny nie może ominąć ograniczenia pojemności.
    private readonly List<T> _items = new();

    public int Capacity { get; }
    public int Count => _items.Count;
    public bool IsFull => Count >= Capacity;
    public bool IsEmpty => Count == 0;
    public T this[int index] => _items[index];

    public CapacityCollection(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
    }

    /// <summary>Dodaje element, jeśli jest miejsce. true oznacza powodzenie.</summary>
    public virtual bool TryAdd(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (IsFull) return false;
        _items.Add(item);
        return true;
    }

    /// <summary>
    /// Dodaje tyle elementów, ile zaakceptuje TryAdd (także w klasach pochodnych).
    /// Kopia wejścia zapobiega awarii przy AddRange(collection) na tej samej kolekcji.
    /// </summary>
    public int AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        T[] incoming = items.ToArray();
        int added = 0;
        foreach (T item in incoming)
            if (TryAdd(item)) added++;
        return added;
    }

    /// <summary>Usuwa pierwsze wystąpienie według EqualityComparer{T}.Default.</summary>
    public bool Remove(T item) => _items.Remove(item);

    /// <summary>Usuwa element pod indeksem; poza zakresem zgłaszany jest wyjątek.</summary>
    public void RemoveAt(int index) => _items.RemoveAt(index);

    public void Clear() => _items.Clear();
    public bool Contains(T item) => _items.Contains(item);
    public int IndexOf(T item) => _items.IndexOf(item);

    /// <summary>Usuwa i zwraca ostatnio dodany element (semantyka stosu).</summary>
    public bool TryTakeLast([MaybeNullWhen(false)] out T item)
    {
        if (IsEmpty)
        {
            item = default;
            return false;
        }
        int last = Count - 1;
        item = _items[last];
        _items.RemoveAt(last);
        return true;
    }

    public T Max()
    {
        if (IsEmpty) throw new InvalidOperationException("Collection is empty.");
        T max = _items[0];
        foreach (T current in _items)
            if (current.CompareTo(max) > 0) max = current;
        return max;
    }

    public T Min()
    {
        if (IsEmpty) throw new InvalidOperationException("Collection is empty.");
        T min = _items[0];
        foreach (T current in _items)
            if (current.CompareTo(min) < 0) min = current;
        return min;
    }

    /// <summary>Sortuje KOPIĘ; kolejność przechowywania nie zmienia się.</summary>
    public IReadOnlyList<T> Sorted(IComparer<T>? comparer = null)
    {
        List<T> copy = new(_items);
        copy.Sort(comparer ?? Comparer<T>.Default);
        return copy;
    }

    /// <summary>Leniwe filtrowanie: kolekcja jest odczytywana podczas foreach.</summary>
    public IEnumerable<T> Find(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return FindIterator(predicate);
    }

    private IEnumerable<T> FindIterator(Func<T, bool> predicate)
    {
        foreach (T item in _items)
            if (predicate(item)) yield return item;
    }

    /// <summary>Pomija skip elementów i zwraca maksymalnie take kolejnych.</summary>
    public IEnumerable<T> Window(int skip, int take)
    {
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        if (take < 0) throw new ArgumentOutOfRangeException(nameof(take));
        return WindowIterator(skip, take);
    }

    private IEnumerable<T> WindowIterator(int skip, int take)
    {
        if (take == 0) yield break;
        int seen = 0, returned = 0;
        foreach (T item in _items)
        {
            if (seen++ < skip) continue;
            yield return item;
            if (++returned == take) yield break;
        }
    }

    /// <summary>Co step-ty element, zaczynając od pozycji 0 (0, step, 2*step...).</summary>
    public IEnumerable<T> EveryNth(int step)
    {
        if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step));
        return EveryNthIterator(step);
    }

    private IEnumerable<T> EveryNthIterator(int step)
    {
        int index = 0;
        foreach (T item in _items)
        {
            if (index % step == 0) yield return item;
            index++;
        }
    }

    /// <summary>
    /// Snapshot zawiera stan z momentu wywołania. Dodanie/usunięcie elementów później
    /// nie zmienia otrzymanej listy. Dla obiektów referencyjnych nie kopiuje samych obiektów.
    /// </summary>
    public IReadOnlyList<T> Snapshot() => Array.AsReadOnly(_items.ToArray());

    /// <summary>Bezpieczne masowe usuwanie: nie zmieniamy List w trakcie foreach.</summary>
    public int RemoveWhere(Predicate<T> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _items.RemoveAll(predicate);
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (T item in _items)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
