using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubmitService.Domain.Entities;

namespace SubmitService.Infrastructure.EntityConfigs;

internal class PictureConfig : IEntityTypeConfiguration<Picture>
{
    public void Configure(EntityTypeBuilder<Picture> builder)
    {
        builder.ToTable("T_Pictures");
        builder.HasKey(picture => picture.Id).IsClustered(false);
        builder.HasOne(picture => picture.Paragraph).WithMany(paragraph => paragraph.Pictures);
        builder.Property(picture => picture.Region).IsUnicode(false);
    }
}
