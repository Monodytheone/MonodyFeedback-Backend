using FAQService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FAQService.Infrastructure.EntityConfigs;

internal class PageConfig : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("T_Pages");
        builder.HasKey(page => page.Id).IsClustered(false);
        builder.HasOne(page => page.Tab).WithMany(tab => tab.Pages);

    }
}
