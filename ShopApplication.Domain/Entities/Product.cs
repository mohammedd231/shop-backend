using ShopApplication.Domain.Common;

namespace ShopApplication.Domain.Entities;

public class Product : Entity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public string? ImageUrl { get; private set; }

    public Product(string name, string description, string category, decimal price, int stock, string? imageUrl = null)
    {
        UpdateDetails(name, description, category, price, stock, imageUrl);
    }
    private Product() { }

    public void UpdateDetails(string name, string description, string category, decimal price, int stock, string? imageUrl = null)
    {
        Name = name;
        Description = description ?? string.Empty;
        Category = category;
        Price = price;
        StockQuantity = stock;
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Touch();
    }

    public void AdjustStock(int delta)
    {
        var next = StockQuantity + delta;
        if (next < 0) throw new InvalidOperationException("Insufficient stock");
        StockQuantity = next;
        Touch();
    }
}
