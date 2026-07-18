using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KromicFlow.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KromicFlowDbContext>
{
    public KromicFlowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KromicFlowDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=kromicflow;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);
        return new KromicFlowDbContext(optionsBuilder.Options);
    }
}
