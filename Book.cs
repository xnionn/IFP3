namespace Bookstore;


public sealed record Book : IComparable<Book>
{
    #region Task1 - Immutable state and ISBN equality

    public string Title { get; }
    public string Isbn { get; }
    public decimal Price { get; }
    public int StockCount { get; private init; }

    public Book(string title, string isbn, decimal price, int stockCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(isbn);
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ArgumentOutOfRangeException.ThrowIfNegative(stockCount);
        Title = title;
        Isbn = isbn;
        Price = price;
        StockCount = stockCount;
    }

    public bool Equals(Book? other) =>
        other is not null && StringComparer.Ordinal.Equals(Isbn, other.Isbn);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Isbn);

    #endregion

    #region Task2 - Price ordering and controlled stock changes

    public int CompareTo(Book? other) => other is null ? 1 : Price.CompareTo(other.Price);

    public Book Restock(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        return this with { StockCount = checked(StockCount + quantity) };
    }

    public Book Sell(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (quantity > StockCount)
            throw new InvalidOperationException(
                $"Cannot sell {quantity} copies; only {StockCount} are in stock.");
        return this with { StockCount = StockCount - quantity };
    }

    #endregion
}
