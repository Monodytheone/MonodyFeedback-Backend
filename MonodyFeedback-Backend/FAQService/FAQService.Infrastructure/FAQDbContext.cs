using FAQService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FAQService.Infrastructure;

public class FAQDbContext : DbContext
{
    public DbSet<Tab> Tabs { get; private set; }
    public DbSet<Page> Pages { get; private set; }
    public DbSet<QandA> QandAs { get; private set; }

    public FAQDbContext(DbContextOptions<FAQDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
    }
}
