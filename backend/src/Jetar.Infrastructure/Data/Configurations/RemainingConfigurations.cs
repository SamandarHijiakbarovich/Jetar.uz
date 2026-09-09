using Jetar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jetar.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.HasKey(x => x.Id);

        b.Property(x => x.Amount).HasPrecision(12, 0);
        b.Property(x => x.Method).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.ProviderPaymentId).HasMaxLength(128);
        b.Property(x => x.CheckoutUrl).HasMaxLength(600);
        b.Property(x => x.FailureReason).HasMaxLength(500);

        b.HasOne(x => x.Transaction)
            .WithMany(t => t.Payments)
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.ProviderPaymentId);
        b.HasIndex(x => x.Status);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("messages");
        b.HasKey(x => x.Id);

        b.Property(x => x.Text).HasMaxLength(2000).IsRequired();
        b.Property(x => x.AttachmentUrl).HasMaxLength(600);

        b.HasOne(x => x.Transaction)
            .WithMany(t => t.Messages)
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Sender)
            .WithMany()
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Receiver)
            .WithMany()
            .HasForeignKey(x => x.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.TransactionId, x.CreatedAt });
    }
}

public class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> b)
    {
        b.ToTable("ratings");
        b.HasKey(x => x.Id);

        b.Property(x => x.Comment).HasMaxLength(1000);

        // Transaction endi ixtiyoriy — to'g'ridan-to'g'ri baholarda null bo'ladi.
        b.HasOne(x => x.Transaction)
            .WithMany(t => t.Ratings)
            .HasForeignKey(x => x.TransactionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.FromUser)
            .WithMany()
            .HasForeignKey(x => x.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ToUser)
            .WithMany()
            .HasForeignKey(x => x.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bir bitim ichida har tomon faqat bir marta baho beradi.
        b.HasIndex(x => new { x.TransactionId, x.FromUserId }).IsUnique();
        // To'g'ridan-to'g'ri baho (bitimsiz): bir foydalanuvchi bir sotuvchiga bir marta.
        b.HasIndex(x => new { x.FromUserId, x.ToUserId })
            .IsUnique()
            .HasFilter("\"TransactionId\" IS NULL");
        b.HasIndex(x => x.ToUserId);
    }
}

public class BoostRequestConfiguration : IEntityTypeConfiguration<BoostRequest>
{
    public void Configure(EntityTypeBuilder<BoostRequest> b)
    {
        b.ToTable("boost_requests");
        b.HasKey(x => x.Id);

        b.Property(x => x.Amount).HasPrecision(12, 0);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.ScreenshotUrl).HasMaxLength(600);
        b.Property(x => x.ReviewNote).HasMaxLength(1000);

        b.HasOne(x => x.Listing)
            .WithMany()
            .HasForeignKey(x => x.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => new { x.ListingId, x.Status });
    }
}

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> b)
    {
        b.ToTable("disputes");
        b.HasKey(x => x.Id);

        b.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        b.Property(x => x.ResolutionNote).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);

        b.Property(x => x.EvidenceUrls)
            .HasConversion(JsonConverters.StringList)
            .Metadata.SetValueComparer(JsonConverters.StringListComparer);
        b.Property(x => x.EvidenceUrls).HasColumnType("jsonb");

        b.HasOne(x => x.OpenedBy)
            .WithMany()
            .HasForeignKey(x => x.OpenedById)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ResolvedBy)
            .WithMany()
            .HasForeignKey(x => x.ResolvedById)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.TransactionId).IsUnique();
    }
}
