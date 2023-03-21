using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubmitService.Domain.Entities;

namespace SubmitService.Infrastructure.EntityConfigs;

internal class SubmissionConfig : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("T_Submissions");
        builder.HasKey(submission => submission.Id).IsClustered(false);  // 取消Guid主键的聚集索引
        builder.Property(submission => submission.ProcessorId).IsConcurrencyToken();  // 将处理者Id设为并发令牌
        builder.Property(submission => submission.SubmitterName).HasMaxLength(20);
        builder.Property(submission => submission.SubmissionStatus)
            .HasConversion<string>()  // 枚举映射为字符串
            .HasMaxLength(16)
            .IsUnicode(false);
        builder.Property(submission => submission.SubmitterTelNumber).HasMaxLength(15).IsUnicode(false);
        builder.Property(submission => submission.SubmitterEmail).HasMaxLength(320).IsUnicode(false);

        // 值对象
        //builder.OwnsOne(submission => submission.Evaluation, ownedNavigationBuilder =>
        //{
        //    ownedNavigationBuilder.Property(evaluation => evaluation.IsSolved).IsRequired(false);
        //    ownedNavigationBuilder.Property(evaluation => evaluation.Grade).IsRequired(false);
        //});  
        builder.OwnsOne(submission => submission.Evaluation);  // 约定会把映射出的两个字段设为可空
        builder.HasIndex(submission => submission.LastInteractionTime).IsClustered();  // 聚集索引，使Submission在表中按照最后交互时间排序，便于分配时获取提交时间最早的几个
    }
}
