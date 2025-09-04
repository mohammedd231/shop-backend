namespace ShopApplication.Domain.Entities;

public class CartItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // product snapshot
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }

    public int Quantity { get; private set; }

    // computed
    public decimal LineTotal => Price * Quantity;

    private CartItem() { } // EF

    internal CartItem(Guid productId, string name, decimal price, int quantity)
    {
        ProductId = productId;
        Name = name;
        Price = price;
        Quantity = quantity;
    }

    internal void Increase(int qty) => Quantity += qty;
    internal void SetQuantity(int qty) => Quantity = qty;
}
