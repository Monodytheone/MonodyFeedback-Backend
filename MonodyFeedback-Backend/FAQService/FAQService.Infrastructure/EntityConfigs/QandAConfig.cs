using FAQService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FAQService.Infrastructure.EntityConfigs;

internal class QandAConfig : IEntityTypeConfiguration<QandA>
{
    public void Configure(EntityTypeBuilder<QandA> builder)
    {
        builder.ToTable("T_QandAs");
        builder.HasKey(x => x.Id).IsClustered(false);
        builder.HasOne(q => q.Page).WithMany(page => page.QandAs);
    }
}
