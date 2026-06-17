using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<LoginAuditLog> LoginAuditLogs { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Unit> Units { get; set; } = null!;
    public DbSet<UserScope> UserScopes { get; set; } = null!;
    public DbSet<UserAssignment> UserAssignments { get; set; } = null!;
    public DbSet<OpmsTarget> OpmsTargets { get; set; } = null!;
    public DbSet<IpmsTarget> IpmsTargets { get; set; } = null!;
    public DbSet<OpmsTargetTemplate> OpmsTargetTemplates { get; set; } = null!;
    public DbSet<IpmsTargetTemplate> IpmsTargetTemplates { get; set; } = null!;
    public DbSet<OpmsTargetTemplateVersion> OpmsTargetTemplateVersions { get; set; } = null!;
    public DbSet<IpmsTargetTemplateVersion> IpmsTargetTemplateVersions { get; set; } = null!;
    public DbSet<OpmsSubmission> OpmsSubmissions { get; set; } = null!;
    public DbSet<IpmsSubmission> IpmsSubmissions { get; set; } = null!;
    public DbSet<PoeFile> PoeFiles { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<AuditTrail> AuditTrails { get; set; } = null!;
    public DbSet<DueDateExtension> DueDateExtensions { get; set; } = null!;
    public DbSet<ReviewComment> ReviewComments { get; set; } = null!;
    public DbSet<AuditFinding> AuditFindings { get; set; } = null!;
    public DbSet<SubmissionScore> SubmissionScores { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure RolePermission
        builder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });
        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);
        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        // Configure UserPermissionOverride
        builder.Entity<UserPermissionOverride>()
            .HasKey(upo => new { upo.UserId, upo.PermissionId });
        builder.Entity<UserPermissionOverride>()
            .HasOne(upo => upo.User)
            .WithMany(u => u.PermissionOverrides)
            .HasForeignKey(upo => upo.UserId);
        builder.Entity<UserPermissionOverride>()
            .HasOne(upo => upo.Permission)
            .WithMany(p => p.UserPermissionOverrides)
            .HasForeignKey(upo => upo.PermissionId);

        builder.Entity<Department>()
            .HasIndex(d => d.Code)
            .IsUnique();

        builder.Entity<Unit>()
            .HasIndex(u => new { u.DepartmentId, u.Code })
            .IsUnique();

        builder.Entity<Unit>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Units)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.DepartmentEntity)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.UnitEntity)
            .WithMany(unit => unit.Users)
            .HasForeignKey(u => u.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserScope>()
            .HasOne(us => us.User)
            .WithMany(u => u.Scopes)
            .HasForeignKey(us => us.UserId);

        builder.Entity<UserScope>()
            .HasOne(us => us.Department)
            .WithMany(d => d.UserScopes)
            .HasForeignKey(us => us.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserScope>()
            .HasOne(us => us.Unit)
            .WithMany(unit => unit.UserScopes)
            .HasForeignKey(us => us.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserAssignment>()
            .HasOne(ua => ua.User)
            .WithMany(u => u.Assignments)
            .HasForeignKey(ua => ua.UserId);

        builder.Entity<OpmsTargetTemplate>()
            .HasIndex(template => template.TemplateCode)
            .IsUnique();

        builder.Entity<IpmsTargetTemplate>()
            .HasIndex(template => template.TemplateCode)
            .IsUnique();

        builder.Entity<OpmsTargetTemplateVersion>()
            .HasOne(version => version.OpmsTargetTemplate)
            .WithMany(template => template.Versions)
            .HasForeignKey(version => version.OpmsTargetTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IpmsTargetTemplateVersion>()
            .HasOne(version => version.IpmsTargetTemplate)
            .WithMany(template => template.Versions)
            .HasForeignKey(version => version.IpmsTargetTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OpmsTarget>()
            .HasOne(target => target.Department)
            .WithMany()
            .HasForeignKey(target => target.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(target => target.Unit)
            .WithMany()
            .HasForeignKey(target => target.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(target => target.AssignedUser)
            .WithMany()
            .HasForeignKey(target => target.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(target => target.Department)
            .WithMany()
            .HasForeignKey(target => target.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(target => target.Unit)
            .WithMany()
            .HasForeignKey(target => target.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(target => target.AssignedUser)
            .WithMany()
            .HasForeignKey(target => target.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(target => target.RelatedOpmsTarget)
            .WithMany()
            .HasForeignKey(target => target.RelatedOpmsTargetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsSubmission>()
            .HasOne(submission => submission.OpmsTarget)
            .WithMany(target => target.Submissions)
            .HasForeignKey(submission => submission.OpmsTargetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OpmsSubmission>()
            .HasOne(submission => submission.SubmittedByUser)
            .WithMany()
            .HasForeignKey(submission => submission.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsSubmission>()
            .HasOne(submission => submission.IpmsTarget)
            .WithMany(target => target.Submissions)
            .HasForeignKey(submission => submission.IpmsTargetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IpmsSubmission>()
            .HasOne(submission => submission.SubmittedByUser)
            .WithMany()
            .HasForeignKey(submission => submission.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PoeFile>()
            .HasOne(file => file.UploadedByUser)
            .WithMany()
            .HasForeignKey(file => file.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Notification>()
            .HasOne(notification => notification.User)
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AuditTrail>()
            .HasOne(audit => audit.ChangedByUser)
            .WithMany()
            .HasForeignKey(audit => audit.ChangedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DueDateExtension>()
            .HasOne(item => item.ApprovedByUser)
            .WithMany()
            .HasForeignKey(item => item.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReviewComment>()
            .HasOne(comment => comment.CommentedByUser)
            .WithMany()
            .HasForeignKey(comment => comment.CommentedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AuditFinding>()
            .HasOne(finding => finding.CreatedByUser)
            .WithMany()
            .HasForeignKey(finding => finding.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SubmissionScore>()
            .HasOne(score => score.ScoredByUser)
            .WithMany()
            .HasForeignKey(score => score.ScoredByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
