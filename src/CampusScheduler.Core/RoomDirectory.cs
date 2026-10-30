namespace CampusScheduler.Core;

/// <summary>
/// Katalog sal. SortedDictionary zapewnia wyszukiwanie według kodu
/// i stałą, alfabetyczną kolejność iteracji (nie jest to kolejność dodania).
/// </summary>
public sealed class RoomDirectory : IEnumerable<RoomDefinition>
{
    private readonly SortedDictionary<string, RoomDefinition> _rooms =
        new(StringComparer.OrdinalIgnoreCase);

    public int Count => _rooms.Count;

    /// <summary>False, jeśli kod sali już występuje (bez rozróżniania wielkości liter).</summary>
    public bool Register(RoomDefinition room)
    {
        ArgumentNullException.ThrowIfNull(room);
        return _rooms.TryAdd(room.Code, room);
    }

    public bool Remove(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Room code is required.", nameof(code));
        return _rooms.Remove(code.Trim());
    }

    public RoomDefinition? Find(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Room code is required.", nameof(code));
        return _rooms.TryGetValue(code.Trim(), out RoomDefinition? room) ? room : null;
    }

    /// <summary>
    /// Zapytanie leniwe. Sala musi mieć wymagane miejsca i wyposażenie,
    /// a harmonogram nie może zawierać konfliktu dla podanego przedziału.
    /// </summary>
    public IEnumerable<RoomDefinition> FindAvailable(
        RoomScheduler scheduler,
        DateTimeOffset start,
        DateTimeOffset end,
        int minimumSeats,
        string? requiredEquipment = null)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        if (end <= start) throw new ArgumentException("Invalid time window.", nameof(end));
        if (minimumSeats <= 0) throw new ArgumentOutOfRangeException(nameof(minimumSeats));
        if (requiredEquipment is not null && string.IsNullOrWhiteSpace(requiredEquipment))
            throw new ArgumentException("Equipment name cannot be blank.", nameof(requiredEquipment));

        return FindAvailableIterator(scheduler, start, end, minimumSeats, requiredEquipment);
    }

    private IEnumerable<RoomDefinition> FindAvailableIterator(
        RoomScheduler scheduler, DateTimeOffset start, DateTimeOffset end,
        int minimumSeats, string? requiredEquipment)
    {
        foreach (RoomDefinition room in _rooms.Values)
        {
            if (room.Seats < minimumSeats) continue;
            if (requiredEquipment is not null && !room.HasEquipment(requiredEquipment)) continue;
            if (scheduler.IsRoomFree(room.Code, start, end)) yield return room;
        }
    }

    public IEnumerator<RoomDefinition> GetEnumerator()
    {
        foreach (RoomDefinition room in _rooms.Values)
            yield return room;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
