# API: kontrakty i zachowanie metod

Poniższa ściąga podaje rezultat, sposób wykonania i typowe wyjątki. Zachowanie dokładnie tej wersji kodu jest ważniejsze niż analogie z innymi interfejsami .NET.

## `Reservation`

| Element | Opis |
| --- | --- |
| `EventTitle`, `Room` | Niepuste, przycinane (`Trim`) nazwy |
| `StartsAt`, `EndsAt` | `DateTimeOffset`, wymagane `EndsAt > StartsAt` |
| `Participants` | Liczba całkowita > 0 |
| `Duration` | `EndsAt - StartsAt` |
| `CompareTo(other)` | Naturalny porządek: start, sala, tytuł, koniec, uczestnicy; `null` jest mniejszy |
| `ToString()` | Czytelny zapis wydarzenia, sali, czasu i liczby uczestników |

`ReservationPopularityComparer`: malejąco po uczestnikach, następnie po czasie trwania, następnie alfabetycznie po tytule i na końcu według porządku naturalnego.

## `CapacityCollection<T>`

| API | Zwraca | Uwagi |
| --- | --- | --- |
| `CapacityCollection(capacity)` | obiekt | `capacity <= 0`: `ArgumentOutOfRangeException` |
| `Capacity`, `Count`, `IsFull`, `IsEmpty` | właściwości | Tylko do odczytu |
| `this[index]` | `T` | Błędny indeks: wyjątek z `List<T>` |
| `TryAdd(item)` | `bool` | `false`, gdy pełna; `null` odrzucone |
| `AddRange(items)` | `int` | Liczba rzeczywiście dodanych; kopiuje wejściową sekwencję przed dodawaniem |
| `Remove(item)` | `bool` | Pierwszy element równy według `EqualityComparer<T>.Default` |
| `RemoveAt(index)` | `void` | Usuwa spod indeksu |
| `Clear()` | `void` | Opróżnia kolekcję |
| `Contains`, `IndexOf` | `bool`, `int` | Porównanie przez równość, a nie `CompareTo` |
| `TryTakeLast(out item)` | `bool` | Usuwa ostatni element; dla pustej false |
| `Max()`, `Min()` | `T` | Wymaga `CompareTo`; pusty: `InvalidOperationException` |
| `Sorted(comparer?)` | `IReadOnlyList<T>` | Nowa, posortowana kopia, bez zmiany oryginału |
| `Find(predicate)` | `IEnumerable<T>` | Leniwy iterator |
| `Window(skip, take)` | `IEnumerable<T>` | Leniwy wycinek, parametry nieujemne |
| `EveryNth(step)` | `IEnumerable<T>` | Leniwa próbka, `step > 0` |
| `Snapshot()` | `IReadOnlyList<T>` | Kopia referencji/wartości w momencie wywołania |
| `RemoveWhere(predicate)` | `int` | Bezpiecznie usuwa dopasowania, zwraca liczbę usuniętych |
| `GetEnumerator()` | `IEnumerator<T>` | Zachowuje kolejność dodania, `yield return` |

Ważne: `AddRange` używa wirtualnego `TryAdd`, więc klasy pochodne mogą dołożyć dodatkowe reguły akceptacji. W przypadku niepoprawnego elementu w środku `AddRange` wcześniejsze zaakceptowane elementy pozostają w kolekcji; nie jest to transakcja.

## `RoomScheduler`

| API | Znaczenie |
| --- | --- |
| `TrySchedule(...)` | Tworzy i próbuje dodać rezerwację, odrzuca konflikt/przepełnienie |
| `TryAdd(reservation)` | Nadpisane, waliduje również kolizje |
| `HasConflict(candidate)` | Czy wskazana rezerwacja koliduje z istniejącą |
| `IsRoomFree(room,start,end)` | Czy cały termin jest wolny |
| `ForRoom(room)` | Leniwy strumień rezerwacji jednej sali |
| `ActiveAt(moment)` | Leniwy strumień aktywnych rezerwacji |
| `EventsOn(day)` | LINQ query syntax: wydarzenia dnia, sortowane przy wykonaniu |
| `TotalBookedTime` | Suma czasów wszystkich rezerwacji |
| `TotalAttendees` | Suma zadeklarowanych uczestników (nie deduplikuje osób) |
| `EventsByRoom()` | `SortedDictionary<string,int>`, liczba wydarzeń |
| `AttendeesByRoom()` | `SortedDictionary<string,int>`, suma uczestników |
| `FreeSlots(...)` | Leniwy generator przedziałów wolnych o minimalnej długości |
| `MostPopular(limit)` | Lista wybranych przez porównywanie, oryginał niezmieniony |
| `BusiestRooms(limit)` | Raport najdłużej wykorzystywanych sal |
| `RemoveFinished(moment)` | Masowe usunięcie zakończonych rezerwacji |
| `Cancel(reservation)` | Usunięcie i zapis na stosie |
| `TryUndoCancellation()` | Przywrócenie ostatnio anulowanej, gdy warunki pozwalają |
| `ExportCsv()` | CSV z kodowaniem pól i dat ISO 8601 |

## Pozostałe typy

- **`TimeSlot`**: niemodyfikowalny przedział; `Duration`, `Overlaps`, `Contains`; granice `[Start, End)`.
- **`RoomDefinition`**: kod, miejsca, niemodyfikowalny widok posortowanego wyposażenia, `HasEquipment`.
- **`RoomDirectory`**: `Register`, `Remove`, `Find`, `FindAvailable`; iteracja w porządku klucza.
- **`ReservationWaitlist`**: `Enqueue` i `Process(scheduler)`; kolejka FIFO.
- **`ReservationQueries`**: `LongerThan`, `SearchTitle`, `DistinctTitles`, `UsageByRoom`, `ParticipantHours`, `AttendeesByDay`, `RunningAttendance`.
- **`DirectoryAnalytics`**: `Overview`, `EquipmentIndex`, `SharedRooms`.

## Kiedy wyniki się aktualizują?

| Metoda | Rodzaj wyniku | Obserwuje dodanie po wywołaniu? |
| --- | --- | --- |
| `Find` | iterator `yield` | Tak, **przed rozpoczęciem** enumeracji |
| `ForRoom`, `ActiveAt` | iterator `yield` | Tak, przed enumeracją |
| `EventsOn` | LINQ `IEnumerable` | Tak, przed enumeracją |
| `FreeSlots` | iterator `yield` + migawka przy rozpoczęciu | Tak, przed enumeracją |
| `RoomDirectory.FindAvailable` | iterator `yield` | Tak, przed enumeracją |
| `Sorted` | lista wynikowa | Nie |
| `Snapshot` | kopia | Nie |
| `UsageByRoom` | `List<RoomUsage>` | Nie |
| `DirectoryAnalytics.Overview` | `List<RoomOverview>` | Nie |
| `DistinctTitles` | tablica | Nie |

Nie należy oczekiwać deterministycznego wyniku po modyfikowaniu źródła **w środku jego aktywnej enumeracji**.
