using Microsoft.EntityFrameworkCore;
using FiscalDocAPI.Domain.Entities;

namespace FiscalDocAPI.Infrastructure.Persistence;

public class FiscalDocContext : DbContext
{
    public FiscalDocContext(DbContextOptions<FiscalDocContext> options) : base(options)
    {
    }

    public DbSet<FiscalDocument> FiscalDocuments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FiscalDocument>(entity =>
        {
            entity.ToTable("FiscalDocuments");
            
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.DocumentKey)
                .IsUnique()
                .HasDatabaseName("IX_FiscalDocuments_DocumentKey");
            
            entity.HasIndex(e => e.XmlHash)
                .HasDatabaseName("IX_FiscalDocuments_XmlHash");
            
            entity.HasIndex(e => e.EmitterCnpj)
                .HasDatabaseName("IX_FiscalDocuments_EmitterCnpj");
            
            entity.HasIndex(e => e.EmitterUF)
                .HasDatabaseName("IX_FiscalDocuments_EmitterUF");
            
            entity.HasIndex(e => e.IssueDate)
                .HasDatabaseName("IX_FiscalDocuments_IssueDate");
            
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_FiscalDocuments_CreatedAt");

            entity.Property(e => e.TotalValue)
                .HasPrecision(18, 2);
        });
    }
}
