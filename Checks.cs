using System.Diagnostics.CodeAnalysis;

namespace Bookstore;

internal static class Checks
{
    private static int passed;

    private static void Check([DoesNotReturnIf(false)] bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        Console.WriteLine($"PASS {++passed:00}: {description}");
    }

    private static void Reject<T>(Action action, string description) where T : Exception
    {
        try { action(); }
        catch (T) { Check(true, description); return; }
        throw new InvalidOperationException($"{description}: expected {typeof(T).Name}.");
    }

    public static int Run()
    {
        passed = 0;
        try
        {
            var original = new Book("Refactoring", "111", 45.00m, 4);
            var duplicate = new Book("Different title", "111", 1.00m, 99);
            var other = new Book("Refactoring", "999", 45.00m, 4);
            Check(original.Equals(duplicate),
                "Equality ignores title, price and stock for the same ISBN");
            Check(!original.Equals(other), "Different ISBNs are unequal");
            Check(original == duplicate && original != other, "Record operators use custom equality");
            Check(((object)original).Equals(duplicate), "Object.Equals dispatches to ISBN equality");
            Check(original.GetHashCode() == duplicate.GetHashCode(),
                "Equal ISBNs produce equal hashes");
            Check(new HashSet<Book> { original, duplicate, other }.Count == 2,
                "HashSet deduplicates by ISBN");
            Check(!EqualityComparer<Book>.Default.Equals(original, null) && !original.Equals("111"),
                "Null and another type are unequal");
            var plain = new DefaultBook("Refactoring", "111", 45.00m, 4);
            Check(plain == new DefaultBook("Refactoring", "111", 45.00m, 4),
                "Default record equality accepts identical components");
            Check(plain != (plain with { Title = "Changed" }) &&
                  plain != (plain with { Isbn = "999" }) &&
                  plain != (plain with { Price = 1m }) &&
                  plain != (plain with { StockCount = 99 }),
                "Default record equality includes all four components");
            
            Check(original.CompareTo(duplicate) > 0 && duplicate.CompareTo(original) < 0,
                "Ordering uses ascending price even for equal ISBNs");
            Check(original.CompareTo(other) == 0,
                "Equal prices compare as a tie despite unequal ISBNs");
            Check(original.CompareTo(null) > 0, "A book compares greater than null");

            Check(new Book("Zero", "0", 0m, 0).StockCount == 0,
                "Zero price and zero initial stock are valid");
            Reject<ArgumentOutOfRangeException>(() => new Book("X", "x", 1m, -1),
                "Negative initial stock is rejected");
            Reject<ArgumentOutOfRangeException>(() => new Book("X", "x", 1m, int.MinValue),
                "Minimum integer initial stock is rejected");
            Reject<ArgumentException>(() => new Book(" ", "x", 1m, 0), "Blank title is rejected");
            Reject<ArgumentException>(() => new Book("X", " ", 1m, 0), "Blank ISBN is rejected");
            Reject<ArgumentOutOfRangeException>(() => new Book("X", "x", -1m, 0),
                "Negative initial price is rejected");

            foreach (int quantity in new[] { 0, -1, int.MinValue })
            {
                Reject<ArgumentOutOfRangeException>(() => original.Restock(quantity),
                    $"Restock({quantity}) rejects a nonpositive quantity");
                Reject<ArgumentOutOfRangeException>(() => original.Sell(quantity),
                    $"Sell({quantity}) rejects a nonpositive quantity");
            }
            var restocked = original.Restock(3);
            var sold = restocked.Sell(2);
            Check(restocked.StockCount == 7 && sold.StockCount == 5,
                "Normal restock and sale calculate stock");
            Check(!ReferenceEquals(original, restocked) && !ReferenceEquals(restocked, sold),
                "Each stock operation returns a new reference");
            Check(original.StockCount == 4 && restocked.StockCount == 7,
                "Earlier references retain their original stock");
            original.Sell(1);
            original.Restock(1);
            Check(original.StockCount == 4,
                "Discarding either returned record leaves the source unchanged");
            var empty = original.Sell(4);
            Check(empty.StockCount == 0, "Selling all available stock reaches zero");
            Reject<InvalidOperationException>(() => original.Sell(5), "Overselling is rejected");
            Reject<InvalidOperationException>(() => empty.Sell(1),
                "Selling from empty stock is rejected");
            var maximum = original.Restock(int.MaxValue - 4);
            Check(maximum.StockCount == int.MaxValue, "Restock can reach the maximum integer exactly");
            Reject<OverflowException>(() => maximum.Restock(1),
                "Restock beyond maximum stock is rejected");
            Reject<OverflowException>(() => original.Restock(int.MaxValue),
                "Large positive restock cannot wrap");
            Check(original.StockCount == 4 && maximum.StockCount == int.MaxValue,
                "Rejected operations leave source records unchanged");
            Check(maximum.Sell(int.MaxValue).StockCount == 0,
                "Maximum positive sale is valid when fully stocked");

            var catalog = Program.CreateCatalog();
            var before = catalog.Select(b => (b.Title, b.Isbn, b.Price, b.StockCount)).ToArray();
            var expected = new[]
            {
                ("Refactoring", "111", 45.00m, 4),
                ("Clean Code", "222", 35.50m, 2),
                ("The Pragmatic Programmer", "333", 40.00m, 6)
            };
            Check(before.SequenceEqual(expected),
                "Required catalogue has exact original order and data");

            var sorted = catalog.OrderBy(b => b).ToList();
            Check(sorted.Select(b => b.Isbn).SequenceEqual(new[] { "222", "333", "111" }),
                "Sorted view orders the required catalogue cheapest first");
            Check(catalog.Select(b => (b.Title, b.Isbn, b.Price, b.StockCount)).SequenceEqual(before),
                "Building a sorted view preserves original order and every field");

            var copy = sorted.ToList();
            copy.Add(duplicate);
            Check(copy.Count == 4 && copy.Count(b => b.Equals(duplicate)) == 2,
                "Copied catalogue contains two ISBN-equal entries");
            var naive = copy.Where(b => !b.Equals(duplicate)).ToList();
            Check(naive.Count == 2 && naive.All(b => b.Isbn != "111"),
                "Equality filtering removes both the original and duplicate (4 to 2)");
            var corrected = copy.Where(b => !ReferenceEquals(b, duplicate)).ToList();
            Check(corrected.Count == 3 && corrected.Any(b => ReferenceEquals(b, catalog[0])) &&
                  !corrected.Any(b => ReferenceEquals(b, duplicate)),
                "Reference filtering removes only the inserted object (4 to 3)");
            Check(corrected.Single(b => b.Isbn == "111").Price == 45.00m &&
                  corrected.Single(b => b.Isbn == "111").StockCount == 4 &&
                  catalog.Select(b => (b.Title, b.Isbn, b.Price, b.StockCount)).SequenceEqual(before),
                "Duplicate filtering preserves the original price, stock and catalogue");
            Console.WriteLine($"All {passed} checks passed.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL after {passed} passes: {error}");
            return 1;
        }
    }
}
