namespace ShopApplication.Domain.Entities;

public class Cart
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }

    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public decimal Total => _items.Sum(i => i.LineTotal);

    private Cart() { } // EF

    public Cart(Guid userId)
    {
        UserId = userId;
    }

    // snapshot name & price at add time
    public void AddItem(Guid productId, string name, decimal price, int quantity)
    {
        if (quantity <= 0) return;

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is null)
            _items.Add(new CartItem(productId, name, price, quantity));
        else
            existing.Increase(quantity);
    }

    public void RemoveItem(Guid productId)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is null) return;
        _items.Remove(existing);
    }

    public void SetQuantity(Guid productId, int quantity)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is null) return;

        if (quantity <= 0) { _items.Remove(existing); return; }
        existing.SetQuantity(quantity);
    }
}
