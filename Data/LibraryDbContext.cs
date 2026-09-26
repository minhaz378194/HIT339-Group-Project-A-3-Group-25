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

            // Branch Unique constraint on BranchCode
            modelBuilder.Entity<Branch>()
                .HasIndex(b => b.BranchCode)
                .IsUnique();

            // Item - Branch Relationship
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Branch)
                .WithMany(b => b.Items)
                .HasForeignKey(i => i.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // ItemTransfer Relationships
            modelBuilder.Entity<ItemTransfer>()
                .HasOne(t => t.Item)
                .WithMany()
                .HasForeignKey(t => t.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemTransfer>()
                .HasOne(t => t.FromBranch)
                .WithMany()
                .HasForeignKey(t => t.FromBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemTransfer>()
                .HasOne(t => t.ToBranch)
                .WithMany()
                .HasForeignKey(t => t.ToBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemTransfer>()
                .HasOne(t => t.RequestedByUser)
                .WithMany()
                .HasForeignKey(t => t.RequestedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

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
