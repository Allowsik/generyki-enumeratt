namespace CampusScheduler.Core;

/// <summary>
/// FIFO: kolejka oczekujących rezerwacji. Po zwolnieniu terminów Process
/// próbuje zapisać elementy w kolejności zgłoszenia.
/// </summary>
public sealed class ReservationWaitlist : IEnumerable<Reservation>
{
    private readonly Queue<Reservation> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        _queue.Enqueue(reservation);
    }

    /// <summary>
    /// Próbuje każdej pozycji dokładnie raz. Nieudane trafiają na koniec kolejki,
    /// a przyjęte są zwracane w porządku przetworzenia.
    /// </summary>
    public List<Reservation> Process(RoomScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        int attempts = _queue.Count;
        List<Reservation> accepted = new();
        for (int i = 0; i < attempts; i++)
        {
            Reservation current = _queue.Dequeue();
            if (scheduler.TryAdd(current))
                accepted.Add(current);
            else
                _queue.Enqueue(current);
        }
        return accepted;
    }

    public IEnumerator<Reservation> GetEnumerator()
    {
        foreach (Reservation reservation in _queue)
            yield return reservation;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
