namespace CampusScheduler.Core;

/// <summary>Przykłady metod rozszerzających i operatorów LINQ to Objects.</summary>
public static class ReservationQueries
{
    /// <summary>Where: leniwe filtrowanie długich rezerwacji.</summary>
    public static IEnumerable<Reservation> LongerThan(
        this IEnumerable<Reservation> source, TimeSpan minimumDuration)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (minimumDuration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minimumDuration));
        return source.Where(r => r.Duration >= minimumDuration);
    }

    /// <summary>Where + Contains: wyszukiwanie tytułu, bez rozróżniania wielkości liter.</summary>
    public static IEnumerable<Reservation> SearchTitle(
        this IEnumerable<Reservation> source, string phrase)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(phrase))
            throw new ArgumentException("Search phrase cannot be blank.", nameof(phrase));
        return source.Where(r => r.EventTitle.Contains(phrase.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Select + Distinct + OrderBy: posortowane, unikalne nazwy wydarzeń.</summary>
    public static IReadOnlyList<string> DistinctTitles(this IEnumerable<Reservation> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(r => r.EventTitle)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(title => title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// GroupBy + Sum + OrderBy: grupowanie obiektów po nazwie sali.
    /// Wynik jest obliczony w chwili wywołania (ToList), a nie w trakcie foreach.
    /// </summary>
    public static List<RoomUsage> UsageByRoom(this IEnumerable<Reservation> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.GroupBy(r => r.Room, StringComparer.OrdinalIgnoreCase)
            .Select(group => new RoomUsage(
                group.Key,
                group.Count(),
                TimeSpan.FromTicks(group.Sum(r => r.Duration.Ticks)),
                group.Sum(r => r.Participants)))
            .OrderBy(usage => usage.Room, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Agregacja: liczba uczestnikogodzin = suma (czas w godzinach * uczestnicy).
    /// To wskaźnik obciążenia sal, nie liczba unikalnych osób.
    /// </summary>
    public static double ParticipantHours(this IEnumerable<Reservation> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Sum(r => r.Duration.TotalHours * r.Participants);
    }

    /// <summary>GroupBy + ToDictionary: liczba osób zapisanych na wydarzenia w danym dniu.</summary>
    public static SortedDictionary<DateOnly, int> AttendeesByDay(
        this IEnumerable<Reservation> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var groups = source.GroupBy(r => DateOnly.FromDateTime(r.StartsAt.Date));
        SortedDictionary<DateOnly, int> result = new();
        foreach (var group in groups)
            result[group.Key] = group.Sum(r => r.Participants);
        return result;
    }

    /// <summary>Własny iterator: narastająca liczba uczestników w kolejności wejścia.</summary>
    public static IEnumerable<(Reservation Reservation, int CumulativeAttendees)> RunningAttendance(
        this IEnumerable<Reservation> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return RunningAttendanceIterator(source);
    }

    private static IEnumerable<(Reservation Reservation, int CumulativeAttendees)> RunningAttendanceIterator(
        IEnumerable<Reservation> source)
    {
        int total = 0;
        foreach (Reservation reservation in source)
        {
            total = checked(total + reservation.Participants);
            yield return (reservation, total);
        }
    }
}
