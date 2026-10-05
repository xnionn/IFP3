using System.Globalization;

namespace Bookstore;

internal static class Program
{
    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (args.Contains("--check"))
            return Checks.Run();

        List<Book> catalog = CreateCatalog();

        #region Task1 - Immutability and equality demonstration

        Console.WriteLine("TASK 1: immutable records and ISBN equality");
        Book featured = catalog[0];
        Book display = featured;
        Console.WriteLine($"Initially shared instance: {ReferenceEquals(featured, display)}");
        display = display.Sell(4);
        Console.WriteLine($"After display.Sell(4): featured={featured.StockCount}, " +
            $"display={display.StockCount}");
        Console.WriteLine($"Still shared instance: {ReferenceEquals(featured, display)}");
        featured.Sell(1);
        Console.WriteLine($"Discarded Sell(1): featured stock={featured.StockCount}");

        Console.WriteLine("Prediction: default record equality compares " +
            "all four stored properties.");
        var defaultA = new DefaultBook("Refactoring", "111", 45.00m, 4);
        var defaultB = new DefaultBook("Refactoring", "111", 99.00m, 9);
        var defaultCopy = new DefaultBook("Refactoring", "111", 45.00m, 4);
        Console.WriteLine("Default equality, same ISBN but changed price/stock: " +
            (defaultA == defaultB));
        Console.WriteLine($"Default equality, identical values: {defaultA == defaultCopy}");

        var duplicate = new Book("Refactoring", "111", 99.00m, 9);
        Console.WriteLine($"Custom equality, same ISBN: {featured.Equals(duplicate)}");
        Console.WriteLine($"Custom equality, different ISBN: {featured.Equals(catalog[1])}");
        Console.WriteLine("Equal ISBN gives equal hash: " +
            (featured.GetHashCode() == duplicate.GetHashCode()));

        #endregion

        #region Task2 - Price ordering and controlled stock changes

        Console.WriteLine("\nTASK 2: price ordering and controlled stock changes");
        List<Book> sorted = catalog.OrderBy(book => book).ToList();
        Console.WriteLine("Original order: " +
            string.Join(", ", catalog.Select(book => book.Isbn)));

        Book before = catalog[0];
        Book restocked = before.Restock(3);
        Console.WriteLine($"Restock(3): before={before.StockCount}, " +
            $"after={restocked.StockCount}, " +
            $"new instance={!ReferenceEquals(before, restocked)}");
        Book sold = restocked.Sell(2);
        Console.WriteLine($"Sell(2): before={restocked.StockCount}, after={sold.StockCount}, " +
            $"new instance={!ReferenceEquals(restocked, sold)}");
        try
        {
            sold.Sell(6);
        }
        catch (InvalidOperationException exception)
        {
            Console.WriteLine($"Oversell rejected: {exception.Message}");
        }
        Console.WriteLine($"After rejection: stock={sold.StockCount}");

        #endregion

        #region Task3 - Catalogue report and duplicate handling

        Console.WriteLine("\nTASK 3: sorted catalogue report and duplicate exclusion");
        PrintCatalog(sorted);
        var withDuplicate = new List<Book>(sorted) { duplicate };
        int matches = withDuplicate.Count(book => book.Equals(duplicate));
        Console.WriteLine($"After adding ISBN 111: count={withDuplicate.Count}; " +
            $"equal matches={matches}");

        List<Book> naive = withDuplicate.Where(book => !book.Equals(duplicate)).ToList();
        Console.WriteLine($"Where(!Equals): count={naive.Count}; ISBNs=" +
            string.Join(", ", naive.Select(book => book.Isbn)));
        List<Book> corrected = withDuplicate
            .Where(book => !ReferenceEquals(book, duplicate)).ToList();
        Console.WriteLine($"Where(!ReferenceEquals): count={corrected.Count}; " +
            $"removed={withDuplicate.Count - corrected.Count}");
        Book preserved = corrected.Single(book => book.Isbn == "111");
        Console.WriteLine($"Original ISBN 111 preserved: price={preserved.Price:F2}, " +
            $"stock={preserved.StockCount}, " +
            $"same instance={ReferenceEquals(featured, preserved)}");
        Console.WriteLine("Original catalogue unchanged: " +
            string.Join(", ", catalog.Select(book => $"{book.Isbn}:{book.StockCount}")));

        #endregion

        return 0;
    }

    // Shared source data for Task1, Task2 and Task3.
    internal static List<Book> CreateCatalog() => new()
    {
        new Book("Refactoring", "111", 45.00m, 4),
        new Book("Clean Code", "222", 35.50m, 2),
        new Book("The Pragmatic Programmer", "333", 40.00m, 6),
    };

    // Task3: print the catalogue sorted using Task2's comparison rule.
    private static void PrintCatalog(IEnumerable<Book> books)
    {
        foreach (Book book in books)
            Console.WriteLine($"{book.Isbn} | {book.Title} | {book.Price:F2} | " +
                $"stock={book.StockCount}");
    }
}
