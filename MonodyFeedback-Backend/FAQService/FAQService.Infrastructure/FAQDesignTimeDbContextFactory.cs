using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FAQService.Infrastructure;

internal class FAQDesignTimeDbContextFactory : IDesignTimeDbContextFactory<FAQDbContext>
{
    public FAQDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FAQDbContext>();
        string connStr = Environment.GetEnvironmentVariable("ConnectionStrings:MonodyFeedBackDB")!;
        optionsBuilder.UseSqlServer(connStr);
        return new FAQDbContext(optionsBuilder.Options);
    }
}
