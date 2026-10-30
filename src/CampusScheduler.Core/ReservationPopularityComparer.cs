namespace CampusScheduler.Core;

/// <summary>Popular events first; longer events break ties, then titles.</summary>
public sealed class ReservationPopularityComparer : Comparer<Reservation>
{
    public override int Compare(Reservation? x, Reservation? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int byParticipants = y.Participants.CompareTo(x.Participants);
        if (byParticipants != 0) return byParticipants;

        int byDuration = y.Duration.CompareTo(x.Duration);
        if (byDuration != 0) return byDuration;

        int byTitle = StringComparer.Ordinal.Compare(x.EventTitle, y.EventTitle);
        return byTitle != 0 ? byTitle : x.CompareTo(y);
    }
}
