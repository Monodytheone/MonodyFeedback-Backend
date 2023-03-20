using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SubmitService.Infrastructure;

internal class SubmitDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SubmitDbContext>
{
    public SubmitDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SubmitDbContext>();
        string connStr = Environment.GetEnvironmentVariable("ConnectionStrings:MonodyFeedBackDB")!;
        optionsBuilder.UseSqlServer(connStr);
        return new SubmitDbContext(optionsBuilder.Options, null);
    }
}
