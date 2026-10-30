namespace CampusScheduler.Core;

/// <summary>Single room booking with immutable, validated data.</summary>
public sealed class Reservation : IComparable<Reservation>
{
    public string EventTitle { get; }
    public string Room { get; }
    public DateTimeOffset StartsAt { get; }
    public DateTimeOffset EndsAt { get; }
    public int Participants { get; }
    public TimeSpan Duration => EndsAt - StartsAt;

    public Reservation(
        string eventTitle,
        string room,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int participants)
    {
        if (string.IsNullOrWhiteSpace(eventTitle))
            throw new ArgumentException("Event title is required.", nameof(eventTitle));
        if (string.IsNullOrWhiteSpace(room))
            throw new ArgumentException("Room name is required.", nameof(room));
        if (endsAt <= startsAt)
            throw new ArgumentException("End time must be later than start time.", nameof(endsAt));
        if (participants <= 0)
            throw new ArgumentOutOfRangeException(nameof(participants));

        EventTitle = eventTitle.Trim();
        Room = room.Trim();
        StartsAt = startsAt;
        EndsAt = endsAt;
        Participants = participants;
    }

    /// <summary>Natural order: start time, then room and event title.</summary>
    public int CompareTo(Reservation? other)
    {
        if (other is null) return 1;

        int byStart = StartsAt.CompareTo(other.StartsAt);
        if (byStart != 0) return byStart;

        int byRoom = StringComparer.OrdinalIgnoreCase.Compare(Room, other.Room);
        if (byRoom != 0) return byRoom;

        int byTitle = StringComparer.Ordinal.Compare(EventTitle, other.EventTitle);
        if (byTitle != 0) return byTitle;

        int byEnd = EndsAt.CompareTo(other.EndsAt);
        return byEnd != 0 ? byEnd : Participants.CompareTo(other.Participants);
    }

    public override string ToString() =>
        $"{EventTitle} @ {Room} | {StartsAt:yyyy-MM-dd HH:mm zzz} – {EndsAt:yyyy-MM-dd HH:mm zzz} | {Participants} participants";
}
