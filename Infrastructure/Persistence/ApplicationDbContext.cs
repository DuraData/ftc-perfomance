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
    public DbSet<Period> Periods { get; set; } = null!;
    public DbSet<StrategicGoal> StrategicGoals { get; set; } = null!;
    public DbSet<StrategicObjective> StrategicObjectives { get; set; } = null!;
    public DbSet<BudgetSource> BudgetSources { get; set; } = null!;
    public DbSet<BudgetType> BudgetTypes { get; set; } = null!;
    public DbSet<UnitOfMeasure> UnitOfMeasures { get; set; } = null!;
    public DbSet<Ward> Wards { get; set; } = null!;
    public DbSet<VoteNumber> VoteNumbers { get; set; } = null!;
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
    public DbSet<IdpPlan> IdpPlans { get; set; } = null!;
    public DbSet<IdpPlanVersion> IdpPlanVersions { get; set; } = null!;
    public DbSet<IdpChangeLog> IdpChangeLogs { get; set; } = null!;
    public DbSet<IdpStrategicOutcome> IdpStrategicOutcomes { get; set; } = null!;
    public DbSet<IdpStrategicObjective> IdpStrategicObjectives { get; set; } = null!;
    public DbSet<IdpDevelopmentPriority> IdpDevelopmentPriorities { get; set; } = null!;
    public DbSet<IdpProgramme> IdpProgrammes { get; set; } = null!;
    public DbSet<IdpProject> IdpProjects { get; set; } = null!;
    public DbSet<IdpKpi> IdpKpis { get; set; } = null!;
    public DbSet<IdpAnnualTarget> IdpAnnualTargets { get; set; } = null!;
    public DbSet<IdpAlignmentLink> IdpAlignmentLinks { get; set; } = null!;
    public DbSet<IdpCommunitySession> IdpCommunitySessions { get; set; } = null!;
    public DbSet<IdpCommunityNeed> IdpCommunityNeeds { get; set; } = null!;
    public DbSet<IdpWardInput> IdpWardInputs { get; set; } = null!;
    public DbSet<IdpStakeholderEngagement> IdpStakeholderEngagements { get; set; } = null!;
    public DbSet<IdpRiskLink> IdpRiskLinks { get; set; } = null!;
    public DbSet<IdpBudgetSnapshot> IdpBudgetSnapshots { get; set; } = null!;
    public DbSet<IdpDocument> IdpDocuments { get; set; } = null!;
    public DbSet<IdpCollaborationComment> IdpCollaborationComments { get; set; } = null!;
    public DbSet<IdpTaskAssignment> IdpTaskAssignments { get; set; } = null!;

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

        builder.Entity<OpmsTarget>()
            .Property(target => target.Baseline)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.AnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Weight)
            .HasPrecision(18, 2);

        // Add precision for new OpmsTarget decimal fields
        builder.Entity<OpmsTarget>()
            .Property(target => target.Q1Target)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q1Budget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q2Target)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q2Budget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.MidTermTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.MidTermBudget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q3Target)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q3Budget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q3RevisedTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q4Target)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q4Budget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.Q4RevisedTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.RevisedAnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .Property(target => target.RevisedAnnualBudget)
            .HasPrecision(18, 2);

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

        builder.Entity<IpmsTarget>()
            .Property(target => target.AnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Weight)
            .HasPrecision(18, 2);

        // Add precision for new IpmsTarget decimal fields
        builder.Entity<IpmsTarget>()
            .Property(target => target.Baseline)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q1Target)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q1Budget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q2Target)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q2Budget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.MidTermTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.MidTermBudget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q3Target)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q3Budget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q3RevisedTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q4Target)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q4Budget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.Q4RevisedTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.RevisedAnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTarget>()
            .Property(target => target.RevisedAnnualBudget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTargetTemplate>()
            .Property(template => template.Baseline)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTargetTemplate>()
            .Property(template => template.AnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTargetTemplate>()
            .Property(template => template.Weight)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTargetTemplate>()
            .Property(template => template.AnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<IpmsTargetTemplate>()
            .Property(template => template.Weight)
            .HasPrecision(18, 2);

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

        builder.Entity<OpmsSubmission>()
            .Property(submission => submission.Actual)
            .HasPrecision(18, 2);

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

        builder.Entity<IpmsSubmission>()
            .Property(submission => submission.Actual)
            .HasPrecision(18, 2);

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

        builder.Entity<Period>()
            .HasIndex(p => p.Code)
            .IsUnique();

        builder.Entity<StrategicGoal>()
            .HasIndex(sg => sg.Code)
            .IsUnique();

        builder.Entity<StrategicObjective>()
            .HasIndex(so => so.Code)
            .IsUnique();

        builder.Entity<StrategicObjective>()
            .HasOne(so => so.StrategicGoal)
            .WithMany()
            .HasForeignKey(so => so.StrategicGoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BudgetSource>()
            .HasIndex(bs => bs.Code)
            .IsUnique();

        builder.Entity<BudgetType>()
            .HasIndex(bt => bt.Code)
            .IsUnique();

        builder.Entity<UnitOfMeasure>()
            .HasIndex(uom => uom.Code)
            .IsUnique();

        builder.Entity<Ward>()
            .HasIndex(w => w.Code)
            .IsUnique();

        builder.Entity<VoteNumber>()
            .HasIndex(vn => vn.Code)
            .IsUnique();

        builder.Entity<VoteNumber>()
            .HasOne(vn => vn.Department)
            .WithMany()
            .HasForeignKey(vn => vn.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<VoteNumber>()
            .Property(vn => vn.Amount)
            .HasPrecision(18, 2);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.Period)
            .WithMany()
            .HasForeignKey(ot => ot.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.StrategicGoal)
            .WithMany()
            .HasForeignKey(ot => ot.StrategicGoalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.StrategicObjective)
            .WithMany()
            .HasForeignKey(ot => ot.StrategicObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.BudgetSource)
            .WithMany()
            .HasForeignKey(ot => ot.BudgetSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.BudgetType)
            .WithMany()
            .HasForeignKey(ot => ot.BudgetTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpmsTarget>()
            .HasOne(ot => ot.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(ot => ot.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.Period)
            .WithMany()
            .HasForeignKey(it => it.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.StrategicGoal)
            .WithMany()
            .HasForeignKey(it => it.StrategicGoalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.StrategicObjective)
            .WithMany()
            .HasForeignKey(it => it.StrategicObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.BudgetSource)
            .WithMany()
            .HasForeignKey(it => it.BudgetSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.BudgetType)
            .WithMany()
            .HasForeignKey(it => it.BudgetTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IpmsTarget>()
            .HasOne(it => it.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(it => it.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SubmissionScore>()
            .Property(score => score.Score)
            .HasPrecision(18, 2);

        builder.Entity<IdpPlan>()
            .HasIndex(plan => plan.PlanCode)
            .IsUnique();

        builder.Entity<IdpPlan>()
            .HasOne(plan => plan.CreatedByUser)
            .WithMany()
            .HasForeignKey(plan => plan.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpPlan>()
            .HasOne(plan => plan.ApprovedByUser)
            .WithMany()
            .HasForeignKey(plan => plan.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpPlanVersion>()
            .HasIndex(version => new { version.IdpPlanId, version.VersionNumber })
            .IsUnique();

        builder.Entity<IdpPlanVersion>()
            .HasOne(version => version.IdpPlan)
            .WithMany(plan => plan.Versions)
            .HasForeignKey(version => version.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpPlanVersion>()
            .HasOne(version => version.CreatedByUser)
            .WithMany()
            .HasForeignKey(version => version.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpChangeLog>()
            .HasOne(changeLog => changeLog.IdpPlanVersion)
            .WithMany(version => version.ChangeLogs)
            .HasForeignKey(changeLog => changeLog.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpChangeLog>()
            .HasOne(changeLog => changeLog.ChangedByUser)
            .WithMany()
            .HasForeignKey(changeLog => changeLog.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpStrategicOutcome>()
            .HasIndex(outcome => new { outcome.IdpPlanId, outcome.Code })
            .IsUnique();

        builder.Entity<IdpStrategicOutcome>()
            .HasOne(outcome => outcome.IdpPlan)
            .WithMany(plan => plan.StrategicOutcomes)
            .HasForeignKey(outcome => outcome.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpStrategicObjective>()
            .HasIndex(objective => new { objective.IdpStrategicOutcomeId, objective.Code })
            .IsUnique();

        builder.Entity<IdpStrategicObjective>()
            .HasOne(objective => objective.IdpStrategicOutcome)
            .WithMany(outcome => outcome.StrategicObjectives)
            .HasForeignKey(objective => objective.IdpStrategicOutcomeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpStrategicObjective>()
            .HasOne(objective => objective.ResponsibleDepartment)
            .WithMany()
            .HasForeignKey(objective => objective.ResponsibleDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpStrategicObjective>()
            .HasOne(objective => objective.StrategicOwnerUser)
            .WithMany()
            .HasForeignKey(objective => objective.StrategicOwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpStrategicObjective>()
            .Property(objective => objective.BaselineValue)
            .HasPrecision(18, 2);

        builder.Entity<IdpStrategicObjective>()
            .Property(objective => objective.TargetValue)
            .HasPrecision(18, 2);

        builder.Entity<IdpStrategicObjective>()
            .Property(objective => objective.BudgetAllocation)
            .HasPrecision(18, 2);

        builder.Entity<IdpDevelopmentPriority>()
            .HasOne(priority => priority.IdpStrategicObjective)
            .WithMany(objective => objective.DevelopmentPriorities)
            .HasForeignKey(priority => priority.IdpStrategicObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpProgramme>()
            .HasIndex(programme => new { programme.IdpDevelopmentPriorityId, programme.ProgrammeCode })
            .IsUnique();

        builder.Entity<IdpProgramme>()
            .HasOne(programme => programme.IdpDevelopmentPriority)
            .WithMany(priority => priority.Programmes)
            .HasForeignKey(programme => programme.IdpDevelopmentPriorityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpProgramme>()
            .HasOne(programme => programme.ResponsibleDepartment)
            .WithMany()
            .HasForeignKey(programme => programme.ResponsibleDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpProgramme>()
            .Property(programme => programme.PlannedBudget)
            .HasPrecision(18, 2);

        builder.Entity<IdpProgramme>()
            .Property(programme => programme.ApprovedBudget)
            .HasPrecision(18, 2);

        builder.Entity<IdpProgramme>()
            .Property(programme => programme.ActualExpenditure)
            .HasPrecision(18, 2);

        builder.Entity<IdpProject>()
            .HasIndex(project => new { project.IdpProgrammeId, project.ProjectCode })
            .IsUnique();

        builder.Entity<IdpProject>()
            .HasOne(project => project.IdpProgramme)
            .WithMany(programme => programme.Projects)
            .HasForeignKey(project => project.IdpProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpProject>()
            .HasOne(project => project.Department)
            .WithMany()
            .HasForeignKey(project => project.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpProject>()
            .Property(project => project.Budget)
            .HasPrecision(18, 2);

        builder.Entity<IdpKpi>()
            .HasIndex(kpi => new { kpi.IdpProjectId, kpi.KpiCode })
            .IsUnique();

        builder.Entity<IdpKpi>()
            .HasOne(kpi => kpi.IdpProject)
            .WithMany(project => project.Kpis)
            .HasForeignKey(kpi => kpi.IdpProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpKpi>()
            .HasOne(kpi => kpi.ResponsibleDepartment)
            .WithMany()
            .HasForeignKey(kpi => kpi.ResponsibleDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpKpi>()
            .Property(kpi => kpi.Baseline)
            .HasPrecision(18, 2);

        builder.Entity<IdpKpi>()
            .Property(kpi => kpi.AnnualTarget)
            .HasPrecision(18, 2);

        builder.Entity<IdpKpi>()
            .Property(kpi => kpi.FiveYearTarget)
            .HasPrecision(18, 2);

        builder.Entity<IdpAnnualTarget>()
            .HasIndex(annualTarget => new { annualTarget.IdpKpiId, annualTarget.FinancialYear })
            .IsUnique();

        builder.Entity<IdpAnnualTarget>()
            .HasOne(annualTarget => annualTarget.IdpKpi)
            .WithMany(kpi => kpi.AnnualTargets)
            .HasForeignKey(annualTarget => annualTarget.IdpKpiId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpAnnualTarget>()
            .Property(annualTarget => annualTarget.TargetValue)
            .HasPrecision(18, 2);

        builder.Entity<IdpAnnualTarget>()
            .Property(annualTarget => annualTarget.ActualValue)
            .HasPrecision(18, 2);

        builder.Entity<IdpAlignmentLink>()
            .HasOne(link => link.IdpStrategicObjective)
            .WithMany(objective => objective.AlignmentLinks)
            .HasForeignKey(link => link.IdpStrategicObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpCommunitySession>()
            .HasOne(session => session.IdpPlan)
            .WithMany(plan => plan.CommunitySessions)
            .HasForeignKey(session => session.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpCommunitySession>()
            .HasOne(session => session.Ward)
            .WithMany()
            .HasForeignKey(session => session.WardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpCommunityNeed>()
            .HasOne(need => need.IdpCommunitySession)
            .WithMany(session => session.CommunityNeeds)
            .HasForeignKey(need => need.IdpCommunitySessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpWardInput>()
            .HasIndex(wardInput => new { wardInput.IdpPlanId, wardInput.WardId })
            .IsUnique();

        builder.Entity<IdpWardInput>()
            .HasOne(wardInput => wardInput.IdpPlan)
            .WithMany()
            .HasForeignKey(wardInput => wardInput.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpWardInput>()
            .HasOne(wardInput => wardInput.Ward)
            .WithMany()
            .HasForeignKey(wardInput => wardInput.WardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpStakeholderEngagement>()
            .HasOne(engagement => engagement.IdpCommunitySession)
            .WithMany(session => session.StakeholderEngagements)
            .HasForeignKey(engagement => engagement.IdpCommunitySessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpRiskLink>()
            .HasOne(riskLink => riskLink.IdpStrategicObjective)
            .WithMany(objective => objective.RiskLinks)
            .HasForeignKey(riskLink => riskLink.IdpStrategicObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpRiskLink>()
            .HasOne(riskLink => riskLink.IdpProject)
            .WithMany(project => project.RiskLinks)
            .HasForeignKey(riskLink => riskLink.IdpProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpRiskLink>()
            .HasOne(riskLink => riskLink.IdpKpi)
            .WithMany(kpi => kpi.RiskLinks)
            .HasForeignKey(riskLink => riskLink.IdpKpiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpBudgetSnapshot>()
            .HasOne(snapshot => snapshot.IdpStrategicObjective)
            .WithMany(objective => objective.BudgetSnapshots)
            .HasForeignKey(snapshot => snapshot.IdpStrategicObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpBudgetSnapshot>()
            .HasOne(snapshot => snapshot.IdpProject)
            .WithMany(project => project.BudgetSnapshots)
            .HasForeignKey(snapshot => snapshot.IdpProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpBudgetSnapshot>()
            .Property(snapshot => snapshot.PlannedBudget)
            .HasPrecision(18, 2);

        builder.Entity<IdpBudgetSnapshot>()
            .Property(snapshot => snapshot.ApprovedBudget)
            .HasPrecision(18, 2);

        builder.Entity<IdpBudgetSnapshot>()
            .Property(snapshot => snapshot.ActualExpenditure)
            .HasPrecision(18, 2);

        builder.Entity<IdpDocument>()
            .HasOne(document => document.IdpPlan)
            .WithMany(plan => plan.Documents)
            .HasForeignKey(document => document.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpDocument>()
            .HasOne(document => document.IdpPlanVersion)
            .WithMany()
            .HasForeignKey(document => document.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpDocument>()
            .HasOne(document => document.UploadedByUser)
            .WithMany()
            .HasForeignKey(document => document.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpCollaborationComment>()
            .HasOne(comment => comment.IdpPlan)
            .WithMany()
            .HasForeignKey(comment => comment.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpCollaborationComment>()
            .HasOne(comment => comment.IdpPlanVersion)
            .WithMany()
            .HasForeignKey(comment => comment.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpCollaborationComment>()
            .HasOne(comment => comment.CommentedByUser)
            .WithMany()
            .HasForeignKey(comment => comment.CommentedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpTaskAssignment>()
            .HasOne(task => task.IdpPlan)
            .WithMany()
            .HasForeignKey(task => task.IdpPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdpTaskAssignment>()
            .HasOne(task => task.IdpPlanVersion)
            .WithMany()
            .HasForeignKey(task => task.IdpPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpTaskAssignment>()
            .HasOne(task => task.AssignedToUser)
            .WithMany()
            .HasForeignKey(task => task.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IdpTaskAssignment>()
            .HasOne(task => task.AssignedByUser)
            .WithMany()
            .HasForeignKey(task => task.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
