using FAQService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FAQService.Infrastructure.EntityConfigs;

internal class TabConfig : IEntityTypeConfiguration<Tab>
{
    public void Configure(EntityTypeBuilder<Tab> builder)
    {
        builder.ToTable("T_Tabs");
        builder.HasKey(t => t.Id).IsClustered(false);
    }
}
