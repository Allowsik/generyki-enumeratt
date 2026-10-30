using CampusScheduler.Core;

// Przykład uruchomienia: dotnet run --project src/CampusScheduler.Demo
// Wszystkie godziny mają ten sam offset, aby wyjście było powtarzalne.
DateTimeOffset day = new(2026, 11, 16, 0, 0, 0, TimeSpan.FromHours(1));
DateTimeOffset At(int hour, int minute = 0) => day.AddHours(hour).AddMinutes(minute);

RoomDirectory rooms = new();
rooms.Register(new RoomDefinition("A101", 45, ["Projektor", "Wi-Fi"]));
rooms.Register(new RoomDefinition("B204", 24, ["Laboratorium", "Wi-Fi"]));
rooms.Register(new RoomDefinition("C303", 80, ["Projektor", "Nagłośnienie"]));

RoomScheduler scheduler = new("North Campus", 10);
scheduler.TrySchedule("Introduction to algorithms", "A101", At(9), At(10, 30), 35);
scheduler.TrySchedule("Robotics seminar", "B204", At(9, 30), At(11), 18);
scheduler.TrySchedule("Design critique", "A101", At(11), At(12), 22);
scheduler.TrySchedule("Introduction to algorithms", "C303", At(13), At(14), 50);

Console.WriteLine(scheduler);
Console.WriteLine("\nAgenda (IComparable + sorted snapshot):");
foreach (Reservation reservation in scheduler.Sorted())
    Console.WriteLine($"  {reservation}");

Console.WriteLine("\nSale wolne 10:00–11:00 (min. 30 miejsc, projektor):");
foreach (RoomDefinition room in rooms.FindAvailable(scheduler, At(10), At(11), 30, "Projektor"))
    Console.WriteLine($"  {room}");

Console.WriteLine("\nNajpopularniejsze wydarzenia (IComparer):");
foreach (Reservation reservation in scheduler.MostPopular(2))
    Console.WriteLine($"  {reservation.EventTitle}: {reservation.Participants}");

Console.WriteLine("\nRaport sal (LINQ GroupBy, Select, Sum):");
foreach (RoomUsage usage in scheduler.UsageByRoom())
    Console.WriteLine($"  {usage.Room}: {usage.EventCount} wydarzeń, {usage.Hours:0.##}h, {usage.Attendees} zapisów");

Console.WriteLine("\nLuki w sali A101, 8:00–15:00 (yield return):");
foreach (TimeSlot slot in scheduler.FreeSlots("A101", At(8), At(15), TimeSpan.FromMinutes(30)))
    Console.WriteLine($"  {slot.Start:HH:mm}–{slot.End:HH:mm}");

Console.WriteLine("\nUnikalne tytuły (Select, Distinct, OrderBy):");
Console.WriteLine("  " + string.Join(" | ", scheduler.DistinctTitles()));
Console.WriteLine($"Uczestnikogodziny: {scheduler.ParticipantHours():0.##}");

Reservation contested = new("Special lecture", "B204", At(10), At(11), 12);
ReservationWaitlist waiting = new();
waiting.Enqueue(contested);
Console.WriteLine($"\nKolejka przed zwolnieniem terminu: {waiting.Count}");
Console.WriteLine($"Przyjęte teraz: {waiting.Process(scheduler).Count}");
Reservation robotics = scheduler.First(r => r.EventTitle == "Robotics seminar");
scheduler.Cancel(robotics);
Console.WriteLine($"Przyjęte po anulowaniu: {waiting.Process(scheduler).Count}");
Console.WriteLine($"Cofnięcie anulowania (powinien być konflikt): {scheduler.TryUndoCancellation()}");

Console.WriteLine("\nCSV (początek):");
Console.WriteLine(string.Join("\n", scheduler.ExportCsv().Split('\n').Take(3)));
