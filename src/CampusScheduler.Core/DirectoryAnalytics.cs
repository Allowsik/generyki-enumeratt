namespace CampusScheduler.Core;

/// <summary>LINQ łączący różne źródła danych: katalog sal i terminarz wydarzeń.</summary>
public static class DirectoryAnalytics
{
    /// <summary>
    /// GroupJoin (LEFT JOIN) zachowuje także sale bez wydarzeń.
    /// ToList wymusza wykonanie od razu, dzięki czemu raport jest snapshotem.
    /// </summary>
    public static List<RoomOverview> Overview(this RoomDirectory directory, RoomScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentNullException.ThrowIfNull(scheduler);

        return directory.GroupJoin(
                scheduler,
                room => room.Code,
                reservation => reservation.Room,
                (room, reservations) => new RoomOverview(
                    room.Code,
                    room.Seats,
                    reservations.Count(),
                    reservations.Sum(r => r.Participants),
                    TimeSpan.FromTicks(reservations.Sum(r => r.Duration.Ticks))),
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// SelectMany spłaszcza listy wyposażenia każdej sali; GroupBy tworzy indeks
    /// wyposażenie -> alfabetyczna lista kodów sal.
    /// </summary>
    public static SortedDictionary<string, List<string>> EquipmentIndex(this RoomDirectory directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        var pairs = directory.SelectMany(room => room.Equipment.Select(
            equipment => (Equipment: equipment, Room: room.Code)));

        SortedDictionary<string, List<string>> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (var group in pairs.GroupBy(pair => pair.Equipment, StringComparer.OrdinalIgnoreCase))
            result[group.Key] = group.Select(pair => pair.Room)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                .ToList();
        return result;
    }

    /// <summary>Intersect: nazwy sal występujące w obu harmonogramach.</summary>
    public static IReadOnlyList<string> SharedRooms(
        this IEnumerable<Reservation> left, IEnumerable<Reservation> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Select(r => r.Room)
            .Intersect(right.Select(r => r.Room), StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
