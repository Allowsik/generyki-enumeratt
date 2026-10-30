namespace CampusScheduler.Core;

/// <summary>Przedział czasu [Start, End), z wyłącznym prawym końcem.</summary>
public readonly record struct TimeSlot
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
    public TimeSpan Duration => End - Start;

    public TimeSlot(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new ArgumentException("End must be later than start.", nameof(end));
        Start = start;
        End = end;
    }

    public bool Overlaps(TimeSlot other) => Start < other.End && other.Start < End;
    public bool Contains(DateTimeOffset instant) => Start <= instant && instant < End;
}
