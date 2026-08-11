using Jetar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jetar.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("transactions");
        b.HasKey(x => x.Id);

        // Inson o'qiy oladigan raqam — bazada avtomatik o'sadi (#8842).
        b.Property(x => x.Number).ValueGeneratedOnAdd();
        b.HasIndex(x => x.Number).IsUnique();

        b.Property(x => x.Amount).HasPrecision(12, 0);
        b.Property(x => x.CommissionAmount).HasPrecision(12, 0);
        b.Property(x => x.SellerPayout).HasPrecision(12, 0);

        b.Property(x => x.EscrowCode).HasMaxLength(32).IsRequired();
        b.HasIndex(x => x.EscrowCode).IsUnique();

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.CancelReason).HasMaxLength(500);

        b.HasOne(x => x.Listing)
            .WithMany(l => l.Transactions)
            .HasForeignKey(x => x.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Buyer)
            .WithMany(u => u.Purchases)
            .HasForeignKey(x => x.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Seller)
            .WithMany(u => u.Sales)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Dispute)
            .WithOne(d => d.Transaction!)
            .HasForeignKey<Dispute>(d => d.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.BuyerId);
        b.HasIndex(x => x.SellerId);
        b.HasIndex(x => x.AutoReleaseAt);
    }
}
