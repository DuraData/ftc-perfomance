using FTCERP.Host.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.Infrastructure.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<IpmsTargetOpmsLink> IpmsTargetOpmsLinks { get; set; } = null!;

    private void ConfigureIpmsOpmsTargetLinks(ModelBuilder builder)
    {
        var entity = builder.Entity<IpmsTargetOpmsLink>();
        entity.ToTable("IPMS_TargetOpmsLinks", table => table.HasCheckConstraint("CK_IPMS_TargetOpmsLinks_Lifecycle",
            "([IsActive] = 1 AND [UnlinkedAt] IS NULL AND [UnlinkedByUserId] IS NULL AND [UnlinkReason] IS NULL) OR ([IsActive] = 0 AND [UnlinkedAt] IS NOT NULL AND [UnlinkReason] IS NOT NULL)"));
        entity.HasIndex(item => item.PublicId).IsUnique();
        entity.HasIndex(item => item.IpmsTargetId).IsUnique().HasFilter("[IsActive] = 1");
        entity.HasIndex(item => new { item.OpmsTargetId, item.IsActive });
        entity.Property(item => item.UnlinkReason).HasMaxLength(2000);
        ConfigureRowVersion(entity.Property(item => item.RowVersion));
        entity.HasOne(item => item.Municipality).WithMany().HasForeignKey(item => item.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(item => item.IpmsTarget).WithMany(item => item.RelatedOpmsTargetLinks).HasForeignKey(item => item.IpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(item => item.OpmsTarget).WithMany(item => item.RelatedIpmsTargetLinks).HasForeignKey(item => item.OpmsTargetId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(item => item.LinkedByUser).WithMany().HasForeignKey(item => item.LinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(item => item.UnlinkedByUser).WithMany().HasForeignKey(item => item.UnlinkedByUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasQueryFilter(item => TenantFilterBypass || item.MunicipalityId == CurrentMunicipalityIdOrSentinel);
    }
}
