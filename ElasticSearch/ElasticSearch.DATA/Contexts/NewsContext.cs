using System;
using System.Collections.Generic;
using ElasticSearch.DATA.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElasticSearch.DATA.Contexts;

public partial class NewsContext : DbContext
{
    public NewsContext()
    {
    }

    public NewsContext(DbContextOptions<NewsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<News> News { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-9RJ5BE7;Database=TEST;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<News>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NewsCategoryId).HasColumnName("newsCategoryId");
            entity.Property(e => e.NewsDesc)
                .HasMaxLength(50)
                .HasColumnName("newsDesc");
            entity.Property(e => e.NewsImage).HasColumnName("newsImage");
            entity.Property(e => e.NewsSelflink)
                .HasMaxLength(50)
                .HasColumnName("newsSelflink");
            entity.Property(e => e.NewsSound)
                .HasMaxLength(50)
                .HasColumnName("newsSound");
            entity.Property(e => e.NewsTitle)
                .HasMaxLength(50)
                .HasColumnName("newsTitle");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
