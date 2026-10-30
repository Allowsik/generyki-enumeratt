# Ścieżka nauki: kolekcje, generyki i LINQ

Dokument prowadzi od najprostszych działań po własne zapytania. Każdy temat ma wskazany plik z działającym przykładem oraz samodzielne ćwiczenie. Najlepiej najpierw spróbować rozwiązania, a potem zajrzeć do implementacji lub testów.

## 1. `List<T>` i indeksator

**Plik:** `CapacityCollection.cs`.

- `List<T>` przechowuje elementy w kolejności dodawania i umożliwia odczyt przez indeks.
- `_items.Count` wylicza aktualną liczbę elementów. Właściwość `Count { get; }` bez aktualizacji nie robi tego sama.
- `TryAdd` nie modyfikuje listy, gdy `IsFull == true`.

**Ćwiczenie 1:** utwórz `CapacityCollection<int>` o pojemności 3, dodaj `5, 10, 15, 20`. Sprawdź wynik czwartego `TryAdd`, `Count`, `IsFull` i indeksator `[1]`.

**Ćwiczenie 2:** zmień implementację `Remove` tak, aby wypisywała wynik i porównaj zachowanie `Remove(5)` z `RemoveAt(0)`.

## 2. Parametr typu `T` i `IComparable<T>`

**Pliki:** `CapacityCollection.cs`, `Reservation.cs`, `ReservationPopularityComparer.cs`.

- `T` oznacza typ podany przy tworzeniu obiektu, np. `CapacityCollection<int>`.
- `where T : IComparable<T>` wymaga metody `CompareTo`.
- Wynik `CompareTo` jest **ujemny, zerowy albo dodatni**. Nie musi wynosić `-1` lub `1`.
- `Comparer<T>` jest strategią zewnętrzną i może sortować inaczej niż porządek naturalny.

**Ćwiczenie 3:** dodaj do kolekcji trzy rezerwacje o różnych porach startu. Porównaj `Max()`, `Sorted()` i `Sorted(new ReservationPopularityComparer())`.

**Ćwiczenie 4:** stwórz własny `ReservationDurationComparer`, sortujący rosnąco według długości spotkania, a przy remisie po nazwie wydarzenia porządkiem `Ordinal`.

## 3. `IEnumerable<T>` oraz `yield return`

**Pliki:** `CapacityCollection.cs`, `RoomScheduler.cs`.

- `IEnumerable<T>` umożliwia enumerację przez `foreach`.
- `IEnumerator<T>` przesuwa kursor przez elementy.
- `yield return` oddaje kolejny element i pozwala wrócić do tej samej metody przy następnym wywołaniu `MoveNext()`.
- Generyczny `GetEnumerator()` wymaga również niegenerycznej implementacji `IEnumerable.GetEnumerator()`.

**Ćwiczenie 5:** zaimplementuj `EveryNth(int n)` bez użycia `Where` ani `Select`. Zwracaj elementy spod indeksów `0, n, 2*n, ...`.

**Ćwiczenie 6:** wyjaśnij, dlaczego poniższy kod uwzględnia nowy element:

```csharp
CapacityCollection<int> data = new(5);
data.TryAdd(2);
var query = data.Find(x => x % 2 == 0);
data.TryAdd(4);
Console.WriteLine(string.Join(",", query)); // 2,4
```

**Ćwiczenie 7:** sprawdź, co się stanie, gdy w środku `foreach` wykonasz `data.TryAdd(...)`. Następnie napraw implementację za pomocą `Snapshot()`.

## 4. Słowniki, zbiory, kolejka i stos

**Pliki:** `RoomDirectory.cs`, `RoomDefinition.cs`, `ReservationWaitlist.cs`, `RoomScheduler.cs`.

| Struktura | Charakterystyka | Przykład |
| --- | --- | --- |
| `List<T>` | Kolejność dodawania, indeksowanie | Rezerwacje |
| `SortedDictionary<K,V>` | Klucze w porządku sortowania | Katalog sal, agregaty |
| `SortedSet<T>` | Unikalność + porządek | Wyposażenie |
| `Queue<T>` | FIFO (pierwsze weszło, pierwsze wyszło) | Oczekujące rezerwacje |
| `Stack<T>` | LIFO (ostatnie weszło, pierwsze wyszło) | Cofanie anulowania |

**Ćwiczenie 8:** zarejestruj sale `A101`, `B204`, `a101`. Sprawdź `Register`, `Count` i `Find`.

**Ćwiczenie 9:** wstaw trzy oczekujące rezerwacje. Dwie zajmują tę samą salę i termin. Wywołaj `Process` dwukrotnie, między wywołaniami usuń przyjętą rezerwację.

**Ćwiczenie 10:** anuluj wydarzenie, spróbuj przywrócić je przez `TryUndoCancellation`. Następnie anuluj i przed przywróceniem zarezerwuj ten sam termin dla innego wydarzenia. Dlaczego `UndoCount` nie maleje?

## 5. LINQ: składnia metod i zapytań

**Pliki:** `ReservationQueries.cs`, `RoomScheduler.cs`, `DirectoryAnalytics.cs`.

Metody LINQ działają na obiektach `IEnumerable<T>`. Przykłady różnych operatorów:

```csharp
// Filtrowanie (Where)
var big = scheduler.Where(x => x.Participants >= 20);

// Projekcja (Select)
var titles = scheduler.Select(x => x.EventTitle);

// Sortowanie (OrderBy / ThenBy)
var chronological = scheduler.OrderBy(x => x.StartsAt)
    .ThenBy(x => x.EventTitle);

// Agregacja (Sum)
int participants = scheduler.Sum(x => x.Participants);

// Grupowanie (GroupBy)
var groups = scheduler.GroupBy(x => x.Room, StringComparer.OrdinalIgnoreCase);

// Materializacja (ToList / ToArray): wymusza wykonanie zapytania
List<Reservation> snapshot = big.ToList();
```

Równoważny zapis filtrowania:

```csharp
var result = from reservation in scheduler
             where reservation.Participants >= 20
             orderby reservation.StartsAt
             select reservation.EventTitle;
```

**Ćwiczenie 11:** wypisz posortowane tytuły wydarzeń w sali A101, zaczynających się po godzinie 10:00.

**Ćwiczenie 12:** zbuduj `SortedDictionary<string, double>` przechowujący sumę zarezerwowanych godzin dla każdej sali.

**Ćwiczenie 13:** porównaj wynik `scheduler.Where(...)` z `scheduler.Where(...).ToList()` po dodaniu kolejnego wydarzenia. Który rezultat się zmieni?

**Ćwiczenie 14:** użyj `SelectMany`, aby dla każdego elementu wyposażenia wypisać listę sal, w których ono występuje.

**Ćwiczenie 15:** użyj `GroupJoin`, by wypisać również sale, które nie mają żadnych rezerwacji.

## 6. Małe wyzwania projektowe

1. **Nowy komparator:** ranking wydarzeń wg `Duration`, potem `Participants`, na końcu `EventTitle`.
2. **Plan tygodniowy:** `IEnumerable<Reservation> Between(DateTimeOffset from, DateTimeOffset to)`, najlepiej leniwie.
3. **Raport:** policz dla każdego dnia liczbę wydarzeń i uczestnikogodzin.
4. **Złożoność:** rozważ, jak działa `HasConflict` przy 10, 100 i 10 000 rezerwacjach.
5. **Testy:** dopisz test dla granicy dwóch przedziałów, gdy jeden kończy się dokładnie wtedy, kiedy drugi się zaczyna.

## Proponowana kolejność plików

`Reservation` → `CapacityCollection` → `ReservationPopularityComparer` → `TimeSlot` → `RoomScheduler` → `RoomDefinition` → `RoomDirectory` → `ReservationWaitlist` → `ReservationQueries` → `DirectoryAnalytics`.

Zacznij od wykonywania małych fragmentów w `Program.cs` i uruchamiaj testy po każdym etapie. Biblioteka oferuje gotowe punkty odniesienia, ale najlepiej samodzielnie przepisać 2–3 najważniejsze metody (`Max`, `GetEnumerator`, `FreeSlots`) bez zaglądania do odpowiedzi.
