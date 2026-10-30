# CampusScheduler 🏛️

**Przykładowy projekt edukacyjny w C# / .NET 10:** planowanie rezerwacji sal, prowadzenie katalogu pomieszczeń i analiza kalendarza wydarzeń.

Projekt służy do ćwiczenia **generyków, kolekcji, interfejsów, enumeratorów, iteratorów `yield return`, porównywania obiektów i LINQ**. Ma własny model dziedziny, demonstracyjną aplikację konsolową i zestaw testów MSTest. Nie jest systemem produkcyjnym do obsługi uczelni.

> **Idea:** jedna rezerwacja opisuje wydarzenie, salę, czas i liczbę uczestników. Harmonogram pilnuje kolizji terminów i liczby rezerwacji. Katalog sal przechowuje pojemność oraz wyposażenie, a zapytania pozwalają wybrać odpowiednie pomieszczenie i wygenerować raporty.

## Szybki start

Wymagane: **.NET SDK 10** (Windows / Linux / macOS), opcjonalnie Rider, Visual Studio lub VS Code.

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/CampusScheduler.Demo
```

Polecenia wykonuj z katalogu zawierającego `CampusScheduler.sln`. W testach nie jest potrzebne połączenie z serwerem ani baza danych. Pierwsze `restore` pobierze pakiety MSTest z NuGet.

### Przykładowe użycie

```csharp
using CampusScheduler.Core;

DateTimeOffset morning = new(2026, 11, 16, 9, 0, 0, TimeSpan.FromHours(1));

RoomScheduler schedule = new("North Campus", capacity: 20);
schedule.TrySchedule("Algorithms", "A101", morning, morning.AddHours(2), 35);
schedule.TrySchedule("Robotics", "B204", morning, morning.AddHours(1), 18);

// Foreach działa, bo CapacityCollection<T> implementuje IEnumerable<T>.
foreach (Reservation item in schedule)
    Console.WriteLine(item);

// Wynik Sorted() jest kopią; kolejność wewnętrznej kolekcji pozostaje bez zmian.
foreach (Reservation item in schedule.Sorted(new ReservationPopularityComparer()))
    Console.WriteLine(item.EventTitle);

// Zapytanie LINQ jest leniwe. Wynik powstanie dopiero przy enumeracji.
IEnumerable<Reservation> search = schedule.SearchTitle("algo");
Console.WriteLine(search.Count());
```

## Co znajdziesz w projekcie?

| Temat C# | Gdzie szukać | Zastosowanie |
| --- | --- | --- |
| `class`, walidacja, właściwości tylko do odczytu | `Reservation`, `RoomDefinition` | Niezmienne dane domenowe |
| `IComparable<T>` | `Reservation.CompareTo` | Naturalne sortowanie po terminie |
| `Comparer<T>` / `IComparer<T>` | `ReservationPopularityComparer` | Alternatywny ranking wydarzeń |
| `where T : IComparable<T>` | `CapacityCollection<T>` | Generyczna kolekcja i `Min`/`Max` |
| `IEnumerable<T>` / `IEnumerator<T>` | `CapacityCollection<T>`, `RoomDirectory`, `ReservationWaitlist` | Obsługa `foreach` |
| `yield return` / `yield break` | `Find`, `Window`, `EveryNth`, `FreeSlots` | Własne iteratory |
| `List<T>` | `CapacityCollection<T>` | Zachowanie kolejności wstawiania |
| `SortedDictionary<TKey,TValue>` | `RoomDirectory`, `EventsByRoom` | Słownik z porządkiem kluczy |
| `SortedSet<T>` | `RoomDefinition` | Unikalne i uporządkowane wyposażenie |
| `Queue<T>` | `ReservationWaitlist` | FIFO dla oczekujących |
| `Stack<T>` | `RoomScheduler.Cancel` / `TryUndoCancellation` | Cofnięcie ostatniego anulowania |
| `Where`, `Select`, `OrderBy`, `Take` | `ReservationQueries`, `RoomScheduler` | Filtrowanie, projekcja, ranking |
| `GroupBy`, `Sum`, `Distinct` | `UsageByRoom`, `DistinctTitles` | Grupowanie i agregacja |
| `SelectMany`, `GroupJoin`, `Intersect` | `DirectoryAnalytics` | Łączenie danych i operacje zbiorowe |
| Krotki | `RunningAttendance` | Kolejne wyniki z narastającą sumą |
| Rekordy | `RoomUsage`, `RoomOverview`, `TimeSlot` | Niewielkie typy danych |
| MSTest | `tests/` | Testowanie granic i wyjątków |

## Architektura

```text
CampusScheduler/
├── CampusScheduler.sln
├── src/
│   ├── CampusScheduler.Core/
│   │   ├── Reservation.cs                    # Model pojedynczego wydarzenia
│   │   ├── ReservationPopularityComparer.cs  # Strategia dodatkowego sortowania
│   │   ├── CapacityCollection.cs             # Generyki, List, IEnumerable, yield
│   │   ├── RoomScheduler.cs                  # Dziedziczenie, planowanie, konflikty
│   │   ├── TimeSlot.cs                       # Przedział czasu [start, end)
│   │   ├── RoomDefinition.cs                 # Sala i SortedSet wyposażenia
│   │   ├── RoomDirectory.cs                  # SortedDictionary i dostępność sal
│   │   ├── ReservationWaitlist.cs            # Queue: lista oczekujących
│   │   ├── ReservationQueries.cs             # Metody rozszerzające + LINQ
│   │   ├── RoomUsage.cs                      # Statystyki jednej sali
│   │   ├── RoomOverview.cs                   # Raport katalogu i harmonogramu
│   │   └── DirectoryAnalytics.cs             # GroupJoin, SelectMany, Intersect
│   └── CampusScheduler.Demo/
│       └── Program.cs                        # Przykładowa aplikacja konsolowa
├── tests/CampusScheduler.Tests/             # Testy MSTest
├── docs/
│   ├── LEARNING_PATH.md                     # Ćwiczenia i pytania kontrolne
│   ├── API_GUIDE.md                         # Metody, kontrakty i wyjątki
│   └── DESIGN_NOTES.md                      # Pułapki enumeracji i LINQ
└── .github/workflows/dotnet.yml             # CI: build + test
```

## Najważniejsze moduły

### `CapacityCollection<T>`: kolekcja od podstaw

Reużywalna klasa z ograniczeniem typu `where T : IComparable<T>` i maksymalną liczbą przechowywanych obiektów. Rozbudowuje funkcje typowej listy o własny kontrakt: `TryAdd` zwraca `false`, kiedy skończyło się miejsce. Klasa implementuje `IEnumerable<T>`, dzięki czemu działa z `foreach` i LINQ.

- **Modyfikacja:** `TryAdd`, `AddRange`, `Remove`, `RemoveAt`, `Clear`, `RemoveWhere`, `TryTakeLast`.
- **Inspekcja:** `Count`, `Capacity`, `IsFull`, `IsEmpty`, indeksator, `Contains`, `IndexOf`, `Min`, `Max`.
- **Sekwencje:** `Find`, `Window`, `EveryNth`, `Sorted`, `Snapshot`.

Metody `Find`, `Window` i `EveryNth` są **leniwe**. `Sorted` i `Snapshot` tworzą **niezależny stan elementów** w momencie wywołania (nie klonują samych obiektów referencyjnych).

### `RoomScheduler`: rezerwacje z kontrolą konfliktów

Dziedziczy po `CapacityCollection<Reservation>`, nadpisując `TryAdd` tak, aby dwie rezerwacje nie zajęły tej samej sali w nachodzących na siebie przedziałach czasu. Udostępnia m.in. `TrySchedule`, `HasConflict`, `ForRoom`, `ActiveAt`, `EventsOn`, `FreeSlots`, `MostPopular`, `BusiestRooms`, `EventsByRoom`, `AttendeesByRoom`, `Cancel`, `TryUndoCancellation` oraz eksport CSV.

Przedziały są **lewostronnie domknięte i prawostronnie otwarte `[start, end)`**. Wydarzenie kończące się o 11:00 nie blokuje wydarzenia rozpoczynającego się dokładnie o 11:00.

### `RoomDirectory`: słownik i zbiory

Katalog korzysta z `SortedDictionary<string, RoomDefinition>`, a wyposażenie sal z `SortedSet<string>`; porównania identyfikatorów i wyposażenia ignorują wielkość liter. `FindAvailable` uwzględnia liczbę miejsc, wymagane wyposażenie i konflikt z harmonogramem.

### `ReservationQueries` / `DirectoryAnalytics`: LINQ

Zestaw metod rozszerzających prezentuje oba style LINQ: składnię metod i składnię zapytań. Są tu filtry, transformacje, unikalność, agregacje, grupowanie, `GroupJoin` (pozwala pokazać również puste sale), `SelectMany` (spłaszcza wyposażenie) i `Intersect` (część wspólna zbiorów sal).

### `ReservationWaitlist`: kolejka FIFO

Zgłoszenia czekają w `Queue<Reservation>`. `Process` próbuje każdą oczekującą rezerwację **dokładnie raz w jednym przebiegu**, przyjęte usuwa z kolejki, a odrzucone zostawia w oczekiwaniu. Złożoność jednego przebiegu zależy od rozmiaru kolejki i kosztu sprawdzania konfliktów.

## Testy

W repozytorium przygotowano **74 metody testowe MSTest**. Obejmują m.in.:

- walidację argumentów i wyjątki;
- kolejność wstawiania, przepełnienie i porównywanie;
- usuwanie pierwszego wystąpienia i masową modyfikację;
- odroczone wykonanie oraz tworzenie migawki;
- kolizje czasowe, terminy stykające się końcami i wyszukiwanie luk;
- słowniki uporządkowane i zbiory bez duplikatów;
- kolejkę FIFO, stos anulowań i przypadki niemożliwego cofnięcia;
- agregaty LINQ, różne style zapytań i łączenie źródeł.

```bash
dotnet test --verbosity normal
```

> Liczba testów oznacza liczbę dodanych scenariuszy, nie deklarację ich przejścia na dowolnym komputerze. Uruchom `dotnet test` lokalnie lub sprawdź wynik GitHub Actions. W środowisku, w którym przygotowano tę wersję, SDK .NET nie było dostępne, więc nie wykonywano kompilacji.

## Czym się różnią iterator, zapytanie i snapshot?

```csharp
CapacityCollection<int> values = new(5);
values.AddRange([3, 1]);

IEnumerable<int> deferred = values.Find(x => x > 0);
IReadOnlyList<int> captured = values.Snapshot();

values.TryAdd(7);

// deferred: 3, 1, 7 (kolekcja jest odczytywana przy foreach)
// captured: 3, 1    (stan zachowany z chwili Snapshot())
Console.WriteLine(string.Join(", ", deferred));
Console.WriteLine(string.Join(", ", captured));
```

**Uwaga:** nie modyfikuj `List<T>` podczas aktywnego `foreach` po tej liście. Jeżeli trzeba równocześnie usuwać elementy, stosuj `RemoveWhere`, wcześniej utworzony `ToArray()` lub pętlę indeksową. `yield return` nie daje automatycznie bezpieczeństwa przed modyfikacją.

## Materiały do nauki

Projekt to **autorskie ćwiczenie demonstracyjne opracowane na podstawie publicznie dostępnych materiałów Programowania 3**. Kod stanowi niezależny przykład zastosowania omawianych zagadnień.

- [Kolekcje: hierarchia i struktury danych](https://csharp.mini.pw.edu.pl/pl/lectures/06.collections/collections/)
- [Enumeratory i `yield return`](https://csharp.mini.pw.edu.pl/pl/lectures/06.collections/enumerators/)
- [LINQ: filtrowanie, projekcja, grupowanie, odroczone wykonanie](https://csharp.mini.pw.edu.pl/pl/lectures/06.collections/linq/)
- [Laboratorium 04: generyki, enumeracja, kolekcje](https://csharp.mini.pw.edu.pl/pl/labs/04.collections-generics/)

Dodatkowo: [ścieżka nauki i ćwiczenia](docs/LEARNING_PATH.md), [opis API](docs/API_GUIDE.md), [decyzje projektowe](docs/DESIGN_NOTES.md).

## Ograniczenia

To projekt edukacyjny. Nie zawiera autoryzacji użytkowników, bazy danych, transakcji, bezpiecznego współdzielenia kolekcji między wątkami ani odporności na równoczesne zapisy. `Reservation` i `RoomDefinition` są niemodyfikowalne po utworzeniu, ale własne klasy kolekcji nie są thread-safe. Nazwy sal są identyfikatorami tekstowymi. `RoomDirectory` może istnieć niezależnie od `RoomScheduler` i nie blokuje dodawania rezerwacji sal, których nie ma w katalogu.

## Licencja

MIT. Zobacz [LICENSE](LICENSE).
