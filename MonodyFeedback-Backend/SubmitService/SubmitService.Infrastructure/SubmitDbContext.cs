using MediatR;
using Microsoft.EntityFrameworkCore;
using SubmitService.Domain.Entities;
using Zack.Infrastructure.EFCore;

namespace SubmitService.Infrastructure;

public class SubmitDbContext : BaseDbContext
{
    // 只为聚合根添加DbSet，对其他实体类的操作都通过聚合根来进行
    public DbSet<Submission> Submissions { get; private set; }

    public SubmitDbContext(DbContextOptions<SubmitDbContext> options, IMediator? mediator) : base(options, mediator)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
    }
}
