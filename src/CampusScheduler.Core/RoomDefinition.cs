namespace CampusScheduler.Core;

/// <summary>Metadane sali: kod, liczba miejsc i uporządkowany zbiór wyposażenia.</summary>
public sealed class RoomDefinition
{
    private readonly SortedSet<string> _equipment;

    public string Code { get; }
    public int Seats { get; }
    public IReadOnlyList<string> Equipment { get; }

    public RoomDefinition(string code, int seats, IEnumerable<string>? equipment = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Room code is required.", nameof(code));
        if (seats <= 0)
            throw new ArgumentOutOfRangeException(nameof(seats));

        Code = code.Trim();
        Seats = seats;
        _equipment = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        if (equipment is not null)
            foreach (string item in equipment)
            {
                if (string.IsNullOrWhiteSpace(item))
                    throw new ArgumentException("Equipment names cannot be blank.", nameof(equipment));
                _equipment.Add(item.Trim());
            }

        Equipment = Array.AsReadOnly(_equipment.ToArray());
    }

    public bool HasEquipment(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Equipment name is required.", nameof(name));
        return _equipment.Contains(name.Trim());
    }

    public override string ToString() => $"{Code} ({Seats} seats)";
}
