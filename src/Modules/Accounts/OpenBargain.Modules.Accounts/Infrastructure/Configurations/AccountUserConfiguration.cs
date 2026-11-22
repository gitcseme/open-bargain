using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OpenBargain.Modules.Accounts.Infrastructure.Configurations;

// Applied after Identity's own configuration, so these settings win.
internal sealed class AccountUserConfiguration : IEntityTypeConfiguration<AccountUser>
{
    public void Configure(EntityTypeBuilder<AccountUser> user)
    {
        user.ToTable("Users");
        user.Property(u => u.DisplayName).HasMaxLength(40);
        user.Property(u => u.AvatarUrl).HasMaxLength(2048);
        user.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
    }
}
