using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityService.Infrastructure;

internal class IdDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdDbContext>
{
    public IdDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<IdDbContext>();
        string connStr = Environment.GetEnvironmentVariable("ConnectionStrings:MonodyFeedBackDB")!;
        builder.UseSqlServer(connStr);
        return new IdDbContext(builder.Options);
    }
}
