# Assignment 3 Week 3

Immutable bookstore records, ISBN equality, price ordering and checked stock changes.

## Разметка Task1, Task2, Task3

Распределение соответствует разделам отчёта (`work/build_report.py`). В `Book.cs`
и `Program.cs` блоки помечены сворачиваемыми `#region Task1`, `Task2`, `Task3`;
в остальных исходниках и проверках используются комментарии с номером задания.

| Задание | Что относится к заданию | Где смотреть |
| --- | --- | --- |
| **Task1** | Изменяемый исходный класс и общие ссылки; неизменяемый `record Book`; равенство по ISBN и согласованный хеш; сравнение со стандартным равенством record | `baseline/Program.cs`, `Book.cs` (свойства, конструктор, `Equals`, `GetHashCode`), `DefaultBook.cs`, блок Task1 в `Program.cs` |
| **Task2** | `IComparable<Book>`, сравнение по цене, отдельный отсортированный список; `Restock` и `Sell`, проверки количества и возврат нового объекта | `Book.cs` (`CompareTo`, `Restock`, `Sell`), блок Task2 в `Program.cs` |
| **Task3** | Вывод отсортированного каталога; добавление дубликата; сравнение фильтров `Equals` и `ReferenceEquals`, сохранение исходной книги | Блок Task3 и `PrintCatalog` в `Program.cs` |

`CreateCatalog` и инфраструктура `Checks.cs` общие для заданий. Блоки проверок
в `Checks.cs` подписаны отдельно. Демонстрация Task1 использует `Sell` из Task2,
чтобы показать сохранение исходного объекта; Task3 использует сортировку из Task2.
`verify.ps1` проверяет сборку, все задания и запрет внешнего изменения состояния
(Task1 / Task2). `baseline/OriginalSnippet.txt` сохраняет исходный фрагмент Task1.
`work/`, `evidence/`, `output/` содержат материалы отчёта, результаты и готовые
артефакты; `bin/` и `obj/` — файлы сборки, а не отдельные задания.

## Requirements

- .NET 10 SDK (tested with 10.0.401 on Windows).
- PowerShell for `verify.ps1`.
- No external NuGet packages.

## Build and run

Open PowerShell in this directory and run:

```powershell
dotnet --version
dotnet build .\IFP3.csproj -c Release
dotnet run --project .\baseline\Baseline.csproj -c Release
dotnet run --project .\IFP3.csproj -c Release --no-build
dotnet run --project .\IFP3.csproj -c Release --no-build -- --check
powershell -NoProfile -ExecutionPolicy Bypass -File .\verify.ps1
```

The baseline prints `0`. The application demonstrates all three tasks. The check
runner reports 43 passing runtime checks. The script also builds a valid external
caller, then confirms six forbidden mutations fail to compile with `CS0200`.
Those deliberate compiler failures are expected; the verification script exits 0.

## Files

- `Book.cs`: immutable record and its equality, ordering and stock operations.
- `DefaultBook.cs`: ordinary generated record equality for comparison.
- `Program.cs`: catalogue, demonstrations, and `--check` entry point.
- `Checks.cs`: runtime assertions for normal, boundary and invalid cases.
- `verify.ps1`: build and runtime checks plus external compile-time access probes.
- `baseline/OriginalSnippet.txt`: supplied starter with its comment removed.
- `baseline/Program.cs`: runnable `Main` wrapper around the starter statements.
- `evidence/`: captured build, demo, baseline, runtime and compiler evidence.

The literal starter places its class before top-level statements, which is not a
valid standalone console-project layout (CS8803). Its executable code is retained in
the text file. The separate baseline project adds a `Main` wrapper and indentation
to make the same declarations and operations runnable. Source comments and regions
identify which assignment task each implementation block belongs to.

## Results

- Supplied catalogue order remains `111, 222, 333`, with original field values.
- Price-sorted view: `222, 333, 111`.
- Mutable alias writes stock `0`; immutable replacement preserves source stock `4`.
- Same ISBN is equal even if title, price or stock differs.
- Four-entry duplicate experiment: equality filter leaves 2; identity filter leaves 3.
- Restock and sale: stock `4 -> 7 -> 5`; selling 6 is rejected.
- Restock overflow throws `OverflowException`; invalid quantities cannot change stock.

The Word report contains explanations, complete text code listings, expected and
actual results, references, and oral-defence preparation. Add the student name and
academic group to its blank identity fields before submission if they are still blank.
