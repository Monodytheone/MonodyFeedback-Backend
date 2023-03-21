using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubmitService.Domain.Entities;

namespace SubmitService.Infrastructure.EntityConfigs;

internal class ParagraphConfig : IEntityTypeConfiguration<Paragraph>
{
    public void Configure(EntityTypeBuilder<Paragraph> builder)
    {
        builder.ToTable("T_Paragraphs");
        builder.HasKey(x => x.Id).IsClustered(false);
        builder.HasOne(paragraph => paragraph.Submission).WithMany(submission => submission.Paragraphs);// 一对多
        builder.Property(paragraph => paragraph.Sender)
            .HasConversion<string>().HasMaxLength(9).IsUnicode(false);
    }
}
