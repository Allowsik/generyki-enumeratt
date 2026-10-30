using System.Text;

namespace CampusScheduler.Core;

/// <summary>A capacity-limited room reservation planner with conflict detection.</summary>
public sealed class RoomScheduler : CapacityCollection<Reservation>
{
    private readonly Stack<Reservation> _cancelled = new();

    public string CampusName { get; }
    public int UndoCount => _cancelled.Count;
    public int TotalAttendees => this.Sum(r => r.Participants);

    public RoomScheduler(string campusName, int capacity) : base(capacity)
    {
        if (string.IsNullOrWhiteSpace(campusName))
            throw new ArgumentException("Campus name is required.", nameof(campusName));

        CampusName = campusName.Trim();
    }

    /// <summary>A room cannot host overlapping reservations. Adjoining slots are allowed.</summary>
    public override bool TryAdd(Reservation item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (IsFull || HasConflict(item)) return false;
        return base.TryAdd(item);
    }

    public bool TrySchedule(
        string eventTitle,
        string room,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int participants)
    {
        Reservation reservation = new(eventTitle, room, startsAt, endsAt, participants);
        return TryAdd(reservation);
    }

    public bool HasConflict(Reservation candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        foreach (Reservation existing in this)
        {
            if (string.Equals(existing.Room, candidate.Room, StringComparison.OrdinalIgnoreCase)
                && candidate.StartsAt < existing.EndsAt
                && existing.StartsAt < candidate.EndsAt)
                return true;
        }
        return false;
    }

    public TimeSpan TotalBookedTime
    {
        get
        {
            TimeSpan total = TimeSpan.Zero;
            foreach (Reservation reservation in this)
                total += reservation.Duration;
            return total;
        }
    }

    /// <summary>Deferred query, evaluated when the caller iterates it.</summary>
    public IEnumerable<Reservation> ForRoom(string room)
    {
        if (string.IsNullOrWhiteSpace(room))
            throw new ArgumentException("Room name is required.", nameof(room));

        foreach (Reservation reservation in this)
        {
            if (string.Equals(reservation.Room, room, StringComparison.OrdinalIgnoreCase))
                yield return reservation;
        }
    }

    public IEnumerable<Reservation> ActiveAt(DateTimeOffset moment)
    {
        foreach (Reservation reservation in this)
        {
            if (reservation.StartsAt <= moment && moment < reservation.EndsAt)
                yield return reservation;
        }
    }

    /// <summary>True, gdy w danej sali nie ma nakładających się terminów.</summary>
    public bool IsRoomFree(string room, DateTimeOffset start, DateTimeOffset end)
    {
        if (string.IsNullOrWhiteSpace(room))
            throw new ArgumentException("Room name is required.", nameof(room));
        if (end <= start)
            throw new ArgumentException("End must be later than start.", nameof(end));

        return !this.Any(existing =>
            string.Equals(existing.Room, room.Trim(), StringComparison.OrdinalIgnoreCase)
            && start < existing.EndsAt && existing.StartsAt < end);
    }

    /// <summary>Leniwe zapytanie LINQ w składni zapytaniowej (query syntax).</summary>
    public IEnumerable<Reservation> EventsOn(DateOnly day) =>
        from reservation in this
        where DateOnly.FromDateTime(reservation.StartsAt.Date) == day
        orderby reservation.StartsAt, reservation.EventTitle
        select reservation;

    /// <summary>Suma uczestników dla każdej sali, klucze alfabetycznie.</summary>
    public SortedDictionary<string, int> AttendeesByRoom()
    {
        SortedDictionary<string, int> attendees = new(StringComparer.OrdinalIgnoreCase);
        foreach (Reservation reservation in this)
        {
            attendees.TryGetValue(reservation.Room, out int current);
            attendees[reservation.Room] = checked(current + reservation.Participants);
        }
        return attendees;
    }

    public SortedDictionary<string, int> EventsByRoom()
    {
        SortedDictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);
        foreach (Reservation reservation in this)
        {
            counts.TryGetValue(reservation.Room, out int count);
            counts[reservation.Room] = count + 1;
        }
        return counts;
    }

    /// <summary>Removes reservations that finished at or before the given instant.</summary>
    public int RemoveFinished(DateTimeOffset moment) =>
        RemoveWhere(reservation => reservation.EndsAt <= moment);

    /// <summary>Popular events first; does not change the scheduler.</summary>
    public List<Reservation> MostPopular(int limit)
    {
        if (limit < 0)
            throw new ArgumentOutOfRangeException(nameof(limit));

        List<Reservation> result = new();
        foreach (Reservation reservation in Sorted(new ReservationPopularityComparer()))
        {
            if (result.Count >= limit) break;
            result.Add(reservation);
        }
        return result;
    }


    /// <summary>
    /// Zwraca luki w terminarzu sali we wskazanym oknie. Wyniki powstają leniwie;
    /// posortowana lista rezerwacji jest tworzona dopiero przy enumeracji.
    /// </summary>
    public IEnumerable<TimeSlot> FreeSlots(string room, DateTimeOffset start,
        DateTimeOffset end, TimeSpan minimumDuration)
    {
        if (string.IsNullOrWhiteSpace(room))
            throw new ArgumentException("Room name is required.", nameof(room));
        if (end <= start)
            throw new ArgumentException("End must be later than start.", nameof(end));
        if (minimumDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minimumDuration));
        return FreeSlotsIterator(room.Trim(), start, end, minimumDuration);
    }

    private IEnumerable<TimeSlot> FreeSlotsIterator(string room, DateTimeOffset start,
        DateTimeOffset end, TimeSpan minimumDuration)
    {
        // ToArray uniezależnia iterację od zmian w wewnętrznej liście.
        Reservation[] events = ForRoom(room)
            .Where(r => r.EndsAt > start && r.StartsAt < end)
            .OrderBy(r => r.StartsAt)
            .ToArray();

        DateTimeOffset cursor = start;
        foreach (Reservation item in events)
        {
            DateTimeOffset busyStart = item.StartsAt < start ? start : item.StartsAt;
            DateTimeOffset busyEnd = item.EndsAt > end ? end : item.EndsAt;
            if (busyStart - cursor >= minimumDuration)
                yield return new TimeSlot(cursor, busyStart);
            if (busyEnd > cursor) cursor = busyEnd;
        }
        if (end - cursor >= minimumDuration)
            yield return new TimeSlot(cursor, end);
    }

    /// <summary>
    /// Anuluje konkretny obiekt rezerwacji i zapisuje go na stosie do cofnięcia.
    /// Normalne Remove i RemoveFinished nie trafiają na stos historii.
    /// </summary>
    public bool Cancel(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        if (!Remove(reservation)) return false;
        _cancelled.Push(reservation);
        return true;
    }

    /// <summary>
    /// Próbuje przywrócić ostatnią anulowaną rezerwację. Nie usuwa wpisu
    /// ze stosu, jeśli pojawił się konflikt albo pojemnik jest pełny.
    /// </summary>
    public bool TryUndoCancellation()
    {
        if (_cancelled.Count == 0) return false;
        if (!TryAdd(_cancelled.Peek())) return false;
        _cancelled.Pop();
        return true;
    }

    /// <summary>Ranking sal według liczby minut zajęć (GroupBy/OrderBy).</summary>
    public IReadOnlyList<RoomUsage> BusiestRooms(int limit)
    {
        if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
        return this.UsageByRoom()
            .OrderByDescending(usage => usage.TotalDuration)
            .ThenBy(usage => usage.Room, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    public string ExportCsv()
    {
        StringBuilder csv = new("Event,Room,Start,End,Participants\n");
        foreach (Reservation reservation in Sorted())
        {
            csv.Append(EscapeCsv(reservation.EventTitle)).Append(',')
                .Append(EscapeCsv(reservation.Room)).Append(',')
                .Append(reservation.StartsAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                .Append(reservation.EndsAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                .Append(reservation.Participants).Append('\n');
        }
        return csv.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
            return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    public override string ToString() =>
        $"{CampusName}: {Count}/{Capacity} reservations, {TotalBookedTime.TotalMinutes:0} booked minutes";
}
