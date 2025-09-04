namespace ShopApplication.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public string Status { get; private set; } = "Pending";
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public decimal Total => _items.Sum(i => i.LineTotal);

    private Order() { } // EF

    // takes cart lines with snapshot fields
    public Order(Guid userId, IEnumerable<(Guid ProductId, string Name, decimal Price, int Quantity)> lines)
    {
        UserId = userId;
        foreach (var (productId, name, price, qty) in lines)
        {
            if (qty <= 0) continue;
            _items.Add(new OrderItem(productId, name, price, qty));
        }
        Touch();
    }

    public void MarkPaid()
    {
        Status = "Paid";
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
