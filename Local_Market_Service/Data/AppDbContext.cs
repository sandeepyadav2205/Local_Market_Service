using Local_Market_Service.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Local_Market_Service.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions options) : base(options){}

        public DbSet<Booking> Booking {  get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<ProviderDocument> ProviderDocuments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Cart> Carts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // ApplicationUser -> Provider (1 : 1)
            // =====================================================
            modelBuilder.Entity<Provider>()
                .HasOne(p => p.ApplicationUser)
                .WithOne(u => u.Provider)
                .HasForeignKey<Provider>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // ApplicationUser -> Customer (1 : 1)
            // =====================================================
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.ApplicationUser)
                .WithOne(u => u.Customer)
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // Category -> Service (1 : Many)
            // =====================================================
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // Provider -> Service (1 : Many)
            // =====================================================
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Provider)
                .WithMany(p => p.Services)
                .HasForeignKey(s => s.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // Customer -> Booking (1 : Many)
            // =====================================================
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // Provider -> Booking (1 : Many)
            // =====================================================
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Provider)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // Service -> Booking (1 : Many)
            // =====================================================
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Service)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // Booking -> Payment (1 : 1)
            // =====================================================
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithOne(b => b.Payment)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // Booking -> Review (1 : Many)
            // =====================================================
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Booking)
                .WithMany(b => b.Reviews)
                .HasForeignKey(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // Provider -> Review (1 : Many)
            // =====================================================
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Provider)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.NoAction);

            // =====================================================
            // Customer -> Review (1 : Many)
            // =====================================================
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            // =====================================================
            // Provider -> ProviderDocument (1 : 1)
            // =====================================================
            modelBuilder.Entity<ProviderDocument>()
                .HasOne(d => d.Provider)
                .WithOne(p => p.ProviderDocument)
                .HasForeignKey<ProviderDocument>(d => d.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // Customer -> Cart (1 : Many)
            // =====================================================
            modelBuilder.Entity<Cart>()
    .HasOne(c => c.Customer)
    .WithMany(c => c.Carts)
    .HasForeignKey(c => c.CustomerId)
    .OnDelete(DeleteBehavior.NoAction);

            // =====================================================
            // Service -> Cart (1 : Many)
            // =====================================================
            modelBuilder.Entity<Cart>()
    .HasOne(c => c.Service)
    .WithMany(s => s.Carts)
    .HasForeignKey(c => c.ServiceId)
    .OnDelete(DeleteBehavior.NoAction);

            // =====================================================
            // UNIQUE INDEXES
            // =====================================================

            modelBuilder.Entity<Provider>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.BookingId)
                .IsUnique();

            modelBuilder.Entity<ProviderDocument>()
                .HasIndex(d => d.ProviderId)
                .IsUnique();
        }

    }
}
