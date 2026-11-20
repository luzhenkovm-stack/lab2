using Microsoft.EntityFrameworkCore;

namespace Lab02.Repository;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Author).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Price).HasPrecision(10, 2);
        });
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Login).IsRequired().HasMaxLength(50);
            entity.HasIndex(x => x.Login).IsUnique();
            entity.Property(x => x.PassHash).IsRequired();
        });
    }
}
