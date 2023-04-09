﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SubmitService.Infrastructure;

#nullable disable

namespace SubmitService.Infrastructure.Migrations
{
    [DbContext(typeof(SubmitDbContext))]
    partial class SubmitDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("SubmitService.Domain.Entities.Paragraph", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Sender")
                        .IsRequired()
                        .HasMaxLength(9)
                        .IsUnicode(false)
                        .HasColumnType("varchar(9)");

                    b.Property<int>("SequenceInSubmission")
                        .HasColumnType("int");

                    b.Property<Guid>("SubmissionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("TextContent")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("SubmissionId");

                    b.ToTable("T_Paragraphs", (string)null);
                });

            modelBuilder.Entity("SubmitService.Domain.Entities.Picture", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Bucket")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FullObjectKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("ParagraphId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Region")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)");

                    b.Property<byte>("Sequence")
                        .HasColumnType("tinyint");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("ParagraphId");

                    b.ToTable("T_Pictures", (string)null);
                });

            modelBuilder.Entity("SubmitService.Domain.Entities.Submission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("ClosingTime")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("LastInteractionTime")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("ProcessorId")
                        .IsConcurrencyToken()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("SubmissionStatus")
                        .IsRequired()
                        .HasMaxLength(16)
                        .IsUnicode(false)
                        .HasColumnType("varchar(16)");

                    b.Property<string>("SubmitterEmail")
                        .HasMaxLength(320)
                        .IsUnicode(false)
                        .HasColumnType("varchar(320)");

                    b.Property<Guid>("SubmitterId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("SubmitterName")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("SubmitterTelNumber")
                        .HasMaxLength(15)
                        .IsUnicode(false)
                        .HasColumnType("varchar(15)");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"), false);

                    b.HasIndex("LastInteractionTime");

                    SqlServerIndexBuilderExtensions.IsClustered(b.HasIndex("LastInteractionTime"));

                    b.ToTable("T_Submissions", (string)null);
                });

            modelBuilder.Entity("SubmitService.Domain.Entities.Paragraph", b =>
                {
                    b.HasOne("SubmitService.Domain.Entities.Submission", "Submission")
                        .WithMany("Paragraphs")
                        .HasForeignKey("SubmissionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Submission");
                });

            modelBuilder.Entity("SubmitService.Domain.Entities.Picture", b =>
                {
                    b.HasOne("SubmitService.Domain.Entities.Paragraph", "Paragraph")
                        .WithMany("Pictures")
                        .HasForeignKey("ParagraphId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Paragraph");
                });

            modelBuilder.Entity("SubmitService.Domain.Entities.Submission", b =>
                {
                    b.OwnsOne("SubmitService.Domain.Entities.Submission.Evaluation#SubmitService.Domain.Entities.ValueObjects.Evaluation", "Evaluation", b1 =>
                        {
                            b1.Property<Guid>("SubmissionId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<byte>("Grade")
                                .HasColumnType("tinyint");

                            b1.Property<bool>("IsSolved")
                                .HasColumnType("bit");

                            b1.HasKey("SubmissionId");

                            b1.ToTable("T_Submissions", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("SubmissionId");
                        });

                    b.Navigation("Evaluation");
                });

            modelBuilder.Entity("SubmitService.Domain.Entities.Paragraph", b =>
                {
                    b.Navigation("Pictures");
                });

            modelBuilder.Entity("SubmitService.Domain.Entities.Submission", b =>
                {
                    b.Navigation("Paragraphs");
                });
#pragma warning restore 612, 618
        }
    }
}
