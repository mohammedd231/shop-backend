namespace ShopApplication.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // product snapshot at purchase time
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }

    // computed
    public decimal LineTotal => Price * Quantity;

    private OrderItem() { } // EF

    internal OrderItem(Guid productId, string name, decimal price, int quantity)
    {
        ProductId = productId;
        Name = name;
        Price = price;
        Quantity = quantity;
    }
}
