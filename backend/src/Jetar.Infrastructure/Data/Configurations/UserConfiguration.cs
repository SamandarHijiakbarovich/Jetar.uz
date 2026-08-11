using Jetar.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jetar.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);

        b.Property(x => x.Username).HasMaxLength(32).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(50).IsRequired();
        b.Property(x => x.Email).HasMaxLength(120);
        b.Property(x => x.TelegramUsername).HasMaxLength(64);
        b.Property(x => x.City).HasMaxLength(64);
        b.Property(x => x.AvatarUrl).HasMaxLength(400);
        b.Property(x => x.Rating).HasPrecision(3, 2);
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);

        b.HasIndex(x => x.Username).IsUnique();
        b.HasIndex(x => x.Phone).IsUnique();
        b.HasIndex(x => x.TelegramUsername);
    }
}
