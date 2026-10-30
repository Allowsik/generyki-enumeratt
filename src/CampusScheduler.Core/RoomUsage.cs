namespace CampusScheduler.Core;

/// <summary>Wiersz raportu statystycznego (wynik LINQ GroupBy + Select).</summary>
public sealed record RoomUsage(string Room, int EventCount, TimeSpan TotalDuration, int Attendees)
{
    public double Hours => TotalDuration.TotalHours;
}
