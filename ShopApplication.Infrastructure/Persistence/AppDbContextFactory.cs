using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShopApplication.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            // keep this simple for design-time; same DB as appsettings.json
            .UseSqlite("Data Source=shop.db")
            .Options;

        return new AppDbContext(options);
    }
}
