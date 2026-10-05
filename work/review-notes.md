# Assignment 3 technical review notes

Verified against the supplied assignment and official Microsoft documentation on 27 September 2026. These notes describe required behavior; actual results must be taken from the submitted application's execution.

## Starter preservation and execution

The supplied text places `class Book` before executable top-level statements. In an ordinary modern C# source file, types must follow top-level statements, so the literal layout triggers CS8803. Preserve the exact supplied snippet in a text artifact. To execute its behavior without changing its declarations, dataset, or operations, retain `class Book` at file scope and put the executable block starting `List<Book> catalog = new()` through the final comment inside `static void Main()` in a separate `Program` class. Add `using System;` and `using System.Collections.Generic;` if implicit usings are not enabled. Indentation and this entry-point wrapper are the only adaptation. A report should state this clearly rather than claim that the raw layout compiled unchanged. [1]

`Book display = featured;` copies the reference; it does not construct a second Book. Both variables, and `catalog[0]`, point to one object. The exact modifying statement is `display.StockCount = 0;`. A class is a reference type, and its public setters make its state mutable after construction. Reading through `featured` therefore observes zero. Expected original catalogue: Refactoring / 111 / 45.00 / 4; Clean Code / 222 / 35.50 / 2; The Pragmatic Programmer / 333 / 40.00 / 6, in this order.

## Record, equality, and immutability

A `record class` remains a reference type; public mutable setters would preserve the aliasing bug. The actual remedy is an immutable public surface, a validated constructor, and stock methods returning new instances. Use `public int StockCount { get; private init; }`, with get-only properties for the remaining data and a sealed record to avoid inheritance complications. A `with` expression makes a shallow copy. Here string values are immutable and `decimal`/`int` are value types, so there is no nested mutable state to share. [3, 12]

Predict then experimentally check default record equality using a small uncustomized probe record: default equality checks the runtime record type and all instance fields, including the compiler-generated backing fields for auto-properties. For these four properties it therefore includes Title, Isbn, Price, and StockCount. It does not inherently recognize ISBN as a business identifier. Supply `Equals(Book? other)` and a matching `GetHashCode()` using the same ordinal ISBN rule. A record's compiler-generated object equality and equality operators route through its typed equality. Do not separately declare `Equals(object?)` or `==` on the record; those members are synthesized. [2]

For the intended domain, same ISBN and different title/price/stock must be equal; different ISBN must be unequal, even when the other data match. Same ISBN implies the same hash code; a hash collision does not imply equality. Do not print exact hash values as portable expected results. The record already supports `IEquatable<Book>` through its generated typed equality contract. [2, 4]

Public `init` prevents later assignment to an existing object's property, but permits callers to set properties while constructing or copying. With a plain public auto-init stock property, `book with { StockCount = -1 }` could bypass the stock methods, and even a positive manual assignment would bypass the checked business operation. Private init blocks those callers. A constructor must reject initial negative stock as well; restricting later updates alone is insufficient. [3]

## Ordering and checked state transitions

Implement `CompareTo(Book? other)` as `other is null ? 1 : Price.CompareTo(other.Price)`. Return-value sign means lower, equal, or higher position in the price order; the exact nonzero number is irrelevant. A non-null instance compares above null. [5]

Prefer `catalog.OrderBy(book => book).ToList()`: the default comparer invokes `IComparable<Book>`, so the output genuinely exercises the interface. Ordering by `book.Price` is valid for display but would not demonstrate that Book.CompareTo is used. `OrderBy` is stable, and `ToList` materializes the result. The original list stays in its supplied order; callers holding that list see no reorder. Sorting a new List copy would also satisfy the assignment. Sorting the original list would be visible to all callers sharing it. [6, 7]

Equality and ordering intentionally answer different questions here: identical ISBNs with different prices can be equal yet have a nonzero comparison; different ISBNs with equal prices can compare as zero yet be unequal. This follows the assignment's two separate rules. Do not add ISBN-first equality shortcuts in CompareTo, which can break price-order consistency. Do not use a price-comparer SortedSet as an ISBN-deduplication tool: comparison ties could collapse different books. Microsoft explicitly documents that equality can be distinct from ordering. [4, 8]

For Restock/Sell, first require `quantity > 0`, then reject an oversale, and prevent Restock integer overflow. `checked(StockCount + quantity)` gives an OverflowException before a new object is returned; an explicit capacity check also works with a clear exception. Once validation succeeds, return `this with { StockCount = ... }`. No operation changes the receiver. If the caller discards `book.Sell(1)`, no stock change is retained. To retain it, assign the result to a variable or intentionally replace an entry in a new catalogue. Existing aliases continue to see their previous immutable instance. [9]

Invariant argument (analysis of the design): the constructor starts within `[0, int.MaxValue]`. Positive restock either stays in that range or throws before constructing a result. A positive sale is allowed only when quantity is no greater than stock; subtraction is consequently nonnegative and cannot overflow. The only writable stock initializer is inside the type, and both exposed transitions validate before copying. An empty external `with { }` produces a valid identical copy, not an invalid state or stock change.

## Duplicate filtering and evidence

Use a copy of the sorted list, add a new Book object whose ISBN duplicates one original, and demonstrate duplicate detection using equality (for example, `Contains(duplicate)` against the list before adding it). With original count 3 and one appended object, input count is 4. `Where(b => !b.Equals(duplicate)).ToList()` excludes every object with that ISBN: both the original and duplicate disappear, leaving count 2. It is operating correctly under the ISBN equality definition but is the wrong predicate for removing one object. [10]

Recompute from the four-element copy with `Where(b => !ReferenceEquals(b, duplicate)).ToList()`. This examines object identity and excludes only that object; count becomes 3, a drop of exactly one. Verify the retained original is the same reference and has its original price and stock; show all three original catalogue values/order unchanged. `ReferenceEquals` cannot be overridden. This one-object conclusion assumes the inserted duplicate object occurs exactly once, as in the required scenario; repeated references to the same object would all be filtered. [11]

Normal stock evidence should show before and after each operation, separate retained old/new instances, the untouched original, and a clear oversale message. Code/output must demonstrate both equal and unequal pairs and the naive and corrected filter survivor lists. Avoid `List<T>.Remove`, which uses value equality and could target the wrong equal object.

## Complexity (analysis of this implementation)

Let n be the number of entries and k the maximum ISBN length. Price comparison, reference comparison, and each stock transition use O(1) time; a successful transition allocates one fixed-size Book, O(1) auxiliary space. Title/ISBN strings are shared safely rather than copied. ISBN equality and hashing are O(k) worst case, so linear duplicate detection and equality-based filtering take O(nk). Reference-based filtering takes O(n). Materialized filtering/copying uses O(n) result storage. Full price sorting uses O(n log n) comparisons and O(n) storage for the sorted view/buffers. Printing is O(total text output), commonly abbreviated O(n) only when field sizes are bounded. With the supplied fixed three-character ISBNs, k is constant.

## Focused checks

Check zero initial stock; negative initial stock rejection; zero/negative quantity rejection for both operations; exact-stock sale to zero; one-above-stock sale rejection; restock at int.MaxValue rejection; successful boundary restock to int.MaxValue; original unchanged after successful and rejected operations; null equality false; CompareTo(null) positive; equal ISBN/equal hash; different ISBN unequal; equal-price/different-ISBN comparison zero; original order/value preservation; counts 4 -> 2 for naive filter and 4 -> 3 for identity filter. A compile-negative check for external `with { StockCount = -1 }` can demonstrate access control, provided it is isolated from the normal build and documented as an expected failure.

## Verified official references

1. Microsoft Learn, Top-level statements: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/top-level-statements
2. Microsoft Learn, Records (language reference): https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record
3. Microsoft Learn, The init keyword: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init
4. Microsoft Learn, IEquatable<T>: https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1?view=net-10.0
5. Microsoft Learn, IComparable<T>.CompareTo: https://learn.microsoft.com/en-us/dotnet/api/system.icomparable-1.compareto
6. Microsoft Learn, Enumerable.OrderBy: https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.orderby?view=net-10.0
7. Microsoft Learn, Comparer<T>.Default: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.comparer-1.default?view=net-10.0
8. Microsoft Learn, SortedSet<T>: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedset-1?view=net-10.0
9. Microsoft Learn, checked and unchecked statements: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked
10. Microsoft Learn, Enumerable.Where: https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.where?view=net-10.0
11. Microsoft Learn, Object.ReferenceEquals: https://learn.microsoft.com/en-us/dotnet/api/system.object.referenceequals?view=net-10.0
12. Microsoft Learn, C# record types (fundamentals): https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records

Use small in-text reference markers with this reference list in the DOCX. The theoretical explanations are paraphrases; avoid extensive quotations.

## Independent source review outcome

Reviewed `Book.cs`, `Program.cs`, `DefaultBook.cs`, `Checks.cs`, `IFP3.csproj`, and `baseline/Program.cs` after implementation became available. No correctness blocker was found. The source uses a sealed record, matching ordinal ISBN equality/hash, private stock init, validated construction, checked restock addition, oversale validation, price comparison with a null branch, and a materialized stable sorted view. The two filtering predicates show the expected 4-to-2 and 4-to-3 behaviors. Catalogue creation exactly matches the supplied order and all fields. The verification code covers default equality for each component, interface behavior, aliases, constructor and quantity boundaries, integer limits, and catalogue preservation. This was a static review; run results and compile-negative evidence belong to the separate verification output. Long console-format lines may need wrapping in the code listing for readable DOCX layout without changing behavior.
