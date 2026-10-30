# Decyzje projektowe i pułapki

## 1. Nie wszystkie porównania są tym samym

`CompareTo` określa *kolejność*, a `EqualityComparer<T>.Default` określa *równość*. W `Reservation` dwie odrębne instancje z identycznymi danymi mogą dać `CompareTo == 0`, ale nie są automatycznie równe według `Equals`, bo klasa nie nadpisuje tej metody.

Dlatego `CapacityCollection<T>.Remove` deleguje do `List<T>.Remove`, co ma semantykę równości, nie sortowania. Alternatywny `ReservationPopularityComparer` zmienia wyłącznie porządek elementów przekazywanych do sortowania.

## 2. Dlaczego `CapacityCollection<T>` nie implementuje `ICollection<T>`?

Interfejs `ICollection<T>` deklaruje `void Add(T item)`, a nasz kontrakt ma `bool TryAdd(T item)` i ograniczenie pojemności. Istnieją poprawne wzorce adaptacji do `ICollection<T>`, ale komplikowałyby ten przykład. Dziedziczymy więc tylko kontrakt enumerowalności (`IEnumerable<T>`), a własne operacje są jawne.

## 3. Kolejność dodania a kolejność sortowania

- `List<T>` zachowuje kolejność wstawiania.
- `Sorted()` kopiuje listę i sortuje kopię.
- `RoomDirectory` używa `SortedDictionary`, więc enumeracja sal ma kolejność alfabetyczną niezależnie od momentu dodania.
- `SortedSet` usuwa duplikaty wyposażenia i narzuca porządek.

`Sort()` zastosowane bezpośrednio do prywatnej listy zepsułoby wymaganą kolejność enumeracji kolekcji.

## 4. Kiedy powstaje wynik zapytania?

W `Find(predicate)` walidacja predykatu jest wykonywana natychmiast, ale właściwe przeglądanie elementów jest odłożone do `FindIterator`.

W `FreeSlots` wywołanie zwraca iterator; dopiero jego enumeracja pobiera i sortuje rezerwacje za pomocą `ToArray`. Taka kopia pozwala bezpiecznie przechodzić po terminach w ustalonej kolejności. Sam iterator nie jest jednak gwarancją odporności na równoczesną modyfikację wątków.

Metody zwracające `ToList` / `ToArray`, a także `Sorted` i `Snapshot`, materializują wyniki od razu.

## 5. Dlaczego `Collection was modified`?

Enumerator listy wykrywa zmiany w strukturze `List<T>` podczas aktywnego `foreach`. Zwykle następuje wtedy `InvalidOperationException`.

Zamiast:

```csharp
foreach (var item in collection)
    if (ShouldDelete(item)) collection.Remove(item); // błędne podejście
```

Użyj `RemoveWhere`, indeksowania od końca lub wcześniej pobranej niezależnej kopii sekwencji.

## 6. Terminy jako przedziały półotwarte

Dwa przedziały `[aStart, aEnd)` i `[bStart, bEnd)` zachodzą na siebie wtedy i tylko wtedy, gdy:

```csharp
aStart < bEnd && bStart < aEnd
```

Dlatego `[09:00, 10:00)` i `[10:00, 11:00)` są rozłączne, ale `[09:00, 10:01)` i `[10:00, 11:00)` już nie.

`DateTimeOffset` pozwala zachować informację o przesunięciu strefowym. Demo stosuje stałe `+01:00`; przy obsłudze prawdziwych grafików wielomiesięcznych trzeba rozważyć `TimeZoneInfo` oraz przejścia na czas letni.

## 7. Stos anulowań to operacja warunkowa

`Cancel` usuwa obiekt i odkłada go na `Stack<Reservation>`. Jeśli od momentu anulowania pojawiła się sprzeczna rezerwacja albo zabrakło miejsca, `TryUndoCancellation()` zwraca `false`, **nie usuwając** wpisu ze stosu.

`Remove`, `RemoveAt`, `Clear` i `RemoveFinished` są zwykłymi operacjami na kolekcji i nie zapisują historii. Ten przykład nie jest pełnym systemem audytu.

## 8. Dlaczego FIFO nie oznacza automatycznego przyjęcia?

`ReservationWaitlist.Process` przechodzi po początkowej liczbie oczekujących. Każdemu zgłoszeniu daje jedną próbę. Nawet jeśli pierwsza rezerwacja koliduje, kolejna może pasować do innej sali, więc również jest sprawdzana. Te, które nie mogą zostać przyjęte, pozostają w kolejce.

## 9. Złożoność: skrócony przegląd

Niech `n` to liczba rezerwacji, `m` liczba sal.

| Operacja | Przybliżona złożoność | Dlaczego |
| --- | --- | --- |
| `TryAdd` kolekcji bez reguł | Amortyzowane O(1) | `List.Add` |
| `Contains`, `IndexOf`, `Remove` | O(n) | Liniowe wyszukiwanie |
| `Min`, `Max`, `Find` (pełne przejście) | O(n) | Jedna pętla |
| `Sorted()` | O(n log n) | Sortowanie kopii |
| `RoomScheduler.HasConflict()` | O(n) | Przegląd istniejących wpisów |
| `RoomDirectory.Find()` | O(log m) | Drzewiasta struktura `SortedDictionary` |
| `RoomDirectory.FindAvailable()` | O(m·n) w najgorszym przypadku | Dla każdej sali sprawdzana dostępność |
| `FreeSlots()` | O(n log n) | Filtrowanie i sortowanie |
| `ReservationWaitlist.Process()` | O(q·n) | Maks. q prób przy n rezerwacjach |

To wartości orientacyjne bez kosztów porównywania napisów, alokacji i szczegółów implementacji bibliotek. Na potrzeby ćwiczeń są wystarczające; do produkcji można wykorzystać indeks terminów dla każdej sali.

## 10. Co dalej?

Dodatkowe pomysły na samodzielne rozwijanie: wyszukiwarka wolnych sal dla serii tygodniowych spotkań, import i eksport JSON, eksport kalendarza ICS, trwałe przechowywanie danych, osobne identyfikatory rezerwacji, wyraźne wyniki błędów zamiast `bool`, testy wydajności i interfejs terminalowy.
