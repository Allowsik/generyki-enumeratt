namespace CampusScheduler.Core;

/// <summary>Metadane sali połączone z bieżącym harmonogramem (LINQ GroupJoin).</summary>
public sealed record RoomOverview(
    string Code, int Seats, int ReservationCount, int Attendees, TimeSpan BookedTime);
