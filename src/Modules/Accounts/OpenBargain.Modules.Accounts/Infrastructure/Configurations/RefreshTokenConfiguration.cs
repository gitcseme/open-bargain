using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OpenBargain.Modules.Accounts.Infrastructure.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> token)
    {
        token.ToTable("RefreshTokens");
        token.Property(t => t.TokenHash).HasMaxLength(64);
        token.HasIndex(t => t.TokenHash).IsUnique();
        token.HasOne<AccountUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
