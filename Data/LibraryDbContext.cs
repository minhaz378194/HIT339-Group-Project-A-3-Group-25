using Microsoft.EntityFrameworkCore;
using LibraTrack.Models;

namespace LibraTrack.Data
{
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
        {
        }

        public DbSet<Item> Items { get; set; } = null!;
        public DbSet<Book> Books { get; set; } = null!;
        public DbSet<Music> MusicAlbums { get; set; } = null!;
        public DbSet<Toy> Toys { get; set; } = null!;

        public DbSet<Borrower> Borrowers { get; set; } = null!;
        public DbSet<Loan> Loans { get; set; } = null!;
        public DbSet<UserAccount> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table-Per-Hierarchy (TPH) Inheritance Configuration for Item and derived classes
            modelBuilder.Entity<Item>()
                .ToTable("Items")
                .HasDiscriminator<string>("ItemTypeDiscriminator")
                .HasValue<Book>("Book")
                .HasValue<Music>("Music")
                .HasValue<Toy>("Toy");

            // Unique constraint on LibraryCode
            modelBuilder.Entity<Item>()
                .HasIndex(i => i.LibraryCode)
                .IsUnique();

            // Unique constraint on BorrowerCardNumber
            modelBuilder.Entity<Borrower>()
                .HasIndex(b => b.BorrowerCardNumber)
                .IsUnique();

            // Unique constraint on UserAccount Username
            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Loan - Item Relationship
            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Item)
                .WithMany(i => i.Loans)
                .HasForeignKey(l => l.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Loan - Borrower Relationship
            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Borrower)
                .WithMany(b => b.Loans)
                .HasForeignKey(l => l.BorrowerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
