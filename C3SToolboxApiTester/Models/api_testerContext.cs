using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace C3SToolboxApiTester.Models
{
    public partial class api_testerContext : DbContext
    {
        public api_testerContext()
        {
        }

        public api_testerContext(DbContextOptions<api_testerContext> options)
            : base(options)
        {
        }

        public virtual DbSet<C3sApiMetadatum> C3sApiMetadata { get; set; }
        public virtual DbSet<C3sApiTestlog> C3sApiTestlogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                ## REDACTED
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<C3sApiMetadatum>(entity =>
            {
                entity.HasKey(e => e.NCode);

                entity.ToTable("c3s_api_metadata");

                entity.Property(e => e.NCode).HasColumnName("n_code");

                entity.Property(e => e.Active)
                    .HasColumnName("active")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.C3sIdentifier)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("c3s_identifier");

                entity.Property(e => e.EcdeUrl)
                    .HasMaxLength(1000)
                    .IsUnicode(false)
                    .HasColumnName("ecde_url");

                entity.Property(e => e.InputDate)
                    .HasColumnType("datetime")
                    .HasColumnName("input_date")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.LastUpdate)
                    .HasColumnType("datetime")
                    .HasColumnName("last_update")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Title)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("title");

                entity.Property(e => e.UpdateCount)
                    .HasColumnName("update_count")
                    .HasDefaultValueSql("((0))");
            });

            modelBuilder.Entity<C3sApiTestlog>(entity =>
            {
                entity.HasKey(e => e.NCode);

                entity.ToTable("c3s_api_testlog");

                entity.Property(e => e.NCode).HasColumnName("n_code");

                entity.Property(e => e.Active)
                    .HasColumnName("active")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.C3sIndicatorId)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("c3s_indicator_id");

                entity.Property(e => e.C3sIndicatorType)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("c3s_indicator_type");

                entity.Property(e => e.C3sRequestType)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("c3s_request_type");

                entity.Property(e => e.InputDate)
                    .HasColumnType("datetime")
                    .HasColumnName("input_date")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.LastUpdate)
                    .HasColumnType("datetime")
                    .HasColumnName("last_update")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.RequestContent)
                    .IsUnicode(false)
                    .HasColumnName("request_content");

                entity.Property(e => e.RequestLogHash)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("request_log_hash");

                entity.Property(e => e.RequestMethod)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("request_method");

                entity.Property(e => e.RequestUrl)
                    .IsUnicode(false)
                    .HasColumnName("request_url");

                entity.Property(e => e.ResponseContent)
                    .IsUnicode(false)
                    .HasColumnName("response_content");

                entity.Property(e => e.ResponseDuration).HasColumnName("response_duration");

                entity.Property(e => e.ResponseStatus).HasColumnName("response_status");

                entity.Property(e => e.UpdateCount)
                    .HasColumnName("update_count")
                    .HasDefaultValueSql("((0))");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
