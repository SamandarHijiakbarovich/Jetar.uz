using Jetar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jetar.Infrastructure.Data.Configurations;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> b)
    {
        b.ToTable("listings");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).HasMaxLength(160).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Price).HasPrecision(12, 0);
        b.Property(x => x.ServerRegion).HasMaxLength(16).IsRequired();
        b.Property(x => x.RankLevel).HasMaxLength(64);

        b.Property(x => x.GameType).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        b.Property(x => x.InGameItems)
            .HasConversion(JsonConverters.StringList)
            .Metadata.SetValueComparer(JsonConverters.StringListComparer);
        b.Property(x => x.InGameItems).HasColumnType("jsonb");

        b.Property(x => x.Images)
            .HasConversion(JsonConverters.StringList)
            .Metadata.SetValueComparer(JsonConverters.StringListComparer);
        b.Property(x => x.Images).HasColumnType("jsonb");

        b.Property(x => x.Stats)
            .HasConversion(JsonConverters.StringMap)
            .Metadata.SetValueComparer(JsonConverters.StringMapComparer);
        b.Property(x => x.Stats).HasColumnType("jsonb");

        b.HasOne(x => x.Seller)
            .WithMany(u => u.Listings)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.GameType, x.Status });
        b.HasIndex(x => new { x.Type, x.Status });
        b.HasIndex(x => x.Price);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.SellerId);
    }
}
