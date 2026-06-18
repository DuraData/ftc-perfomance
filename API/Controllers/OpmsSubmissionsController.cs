using FTCERP.Host.API.Requests;
using FTCERP.Host.API.Responses;
using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FTCERP.Host.API.Controllers;

[ApiController]
[Route("api/opms-submissions")]
[Authorize]
public class OpmsSubmissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessControlService _accessControlService;
    private readonly IWorkflowGovernanceService _workflowGovernanceService;
    private readonly IWebHostEnvironment _environment;

    public OpmsSubmissionsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAccessControlService accessControlService,
        IWorkflowGovernanceService workflowGovernanceService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _accessControlService = accessControlService;
        _workflowGovernanceService = workflowGovernanceService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<OpmsSubmissionResponse[]>>> GetSubmissions()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsSubmissionResponse[]>(false, null, "User not found"));

        var items = await _context.OpmsSubmissions
            .AsNoTracking()
            .Include(item => item.OpmsTarget).ThenInclude(target => target.Department)
            .Include(item => item.OpmsTarget).ThenInclude(target => target.Unit)
            .Include(item => item.SubmittedByUser)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        var visible = new List<OpmsSubmissionResponse>();
        foreach (var item in items)
        {
            var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Submissions.View", BuildScope(item));
            if (decision.Allowed)
            {
                visible.Add(item.ToResponse());
            }
        }

        return Ok(new ApiResponse<OpmsSubmissionResponse[]>(true, visible.ToArray()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> GetSubmission(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsSubmissionResponse>(false, null, "User not found"));

        var item = await FindSubmissionAsync(id);
        if (item == null) return NotFound(new ApiResponse<OpmsSubmissionResponse>(false, null, "OPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Submissions.View", BuildScope(item));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsSubmissionResponse>(false, null, decision.Reason));

        return Ok(new ApiResponse<OpmsSubmissionResponse>(true, item.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> CreateSubmission([FromBody] SaveOpmsSubmissionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsSubmissionResponse>(false, null, "User not found"));

        var target = await _context.OpmsTargets.FirstOrDefaultAsync(item => item.Id == request.OpmsTargetId);
        if (target == null) return NotFound(new ApiResponse<OpmsSubmissionResponse>(false, null, "OPMS target not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Submissions.Create", BuildScope(target));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsSubmissionResponse>(false, null, decision.Reason));

        var entity = new OpmsSubmission
        {
            OpmsTargetId = request.OpmsTargetId,
            Quarter = request.Quarter.Trim(),
            Status = "draft",
            Actual = request.Actual,
            ActualDescription = request.ActualDescription?.Trim(),
            ActualExpenditure = request.ActualExpenditure,
            Variance = request.Variance,
            VarianceReason = request.VarianceReason?.Trim(),
            CorrectiveMeasure = request.CorrectiveMeasure?.Trim(),
            SubmitterScore = request.SubmitterScore,
            DueDate = request.DueDate,
            ExtendedDueDate = request.ExtendedDueDate,
            SubmittedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.OpmsSubmissions.Add(entity);
        await _context.SaveChangesAsync();
        var created = await FindSubmissionAsync(entity.Id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsSubmission", created.Id, "Create", null, created.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<OpmsSubmissionResponse>(true, created.ToResponse()));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> UpdateSubmission(string id, [FromBody] SaveOpmsSubmissionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsSubmissionResponse>(false, null, "User not found"));

        var entity = await _context.OpmsSubmissions.Include(item => item.OpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<OpmsSubmissionResponse>(false, null, "OPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Submissions.Edit", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsSubmissionResponse>(false, null, decision.Reason));

        var before = await FindSubmissionAsync(id);
        entity.Quarter = request.Quarter.Trim();
        entity.Actual = request.Actual;
        entity.ActualDescription = request.ActualDescription?.Trim();
        entity.ActualExpenditure = request.ActualExpenditure;
        entity.Variance = request.Variance;
        entity.VarianceReason = request.VarianceReason?.Trim();
        entity.CorrectiveMeasure = request.CorrectiveMeasure?.Trim();
        entity.SubmitterScore = request.SubmitterScore;
        entity.DueDate = request.DueDate;
        entity.ExtendedDueDate = request.ExtendedDueDate;
        await _context.SaveChangesAsync();

        var after = await FindSubmissionAsync(id) ?? entity;
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsSubmission", id, "Edit", before?.ToResponse(), after.ToResponse(), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<OpmsSubmissionResponse>(true, after.ToResponse()));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSubmission(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));

        var entity = await _context.OpmsSubmissions.Include(item => item.OpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<bool>(false, false, "OPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Submissions.Delete", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<bool>(false, false, decision.Reason));

        var before = await FindSubmissionAsync(id);
        _context.OpmsSubmissions.Remove(entity);
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsSubmission", id, "Delete", before?.ToResponse(), null, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpGet("{id}/attachments")]
    public async Task<ActionResult<ApiResponse<PoeFileResponse[]>>> GetAttachments(string id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PoeFileResponse[]>(false, null, "User not found"));

        var submission = await _context.OpmsSubmissions
            .AsNoTracking()
            .Include(item => item.OpmsTarget)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<PoeFileResponse[]>(false, null, "OPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Submissions.View", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoeFileResponse[]>(false, null, decision.Reason));

        var files = await _context.PoeFiles
            .AsNoTracking()
            .Include(item => item.UploadedByUser)
            .Where(item => item.SubmissionKind == SubmissionKind.Opms && item.SubmissionId == id)
            .OrderByDescending(item => item.UploadedAt)
            .ToListAsync();

        return Ok(new ApiResponse<PoeFileResponse[]>(true, files.Select(item => item.ToResponse(HttpContext)).ToArray()));
    }

    [HttpPost("{id}/attachments")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<ActionResult<ApiResponse<PoeFileResponse>>> UploadAttachment(string id, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new ApiResponse<PoeFileResponse>(false, null, "File is required"));

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<PoeFileResponse>(false, null, "User not found"));

        var submission = await _context.OpmsSubmissions
            .Include(item => item.OpmsTarget)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<PoeFileResponse>(false, null, "OPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.POE.Upload", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoeFileResponse>(false, null, decision.Reason));

        var relativeDirectory = Path.Combine("uploads", "poe", "opms", id);
        var absoluteDirectory = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var safeFileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var relativePath = Path.Combine(relativeDirectory, safeFileName);
        var absolutePath = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), relativePath);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream);
        }

        var entity = new PoeFile
        {
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = id,
            FileName = file.FileName,
            StoragePath = relativePath,
            ContentType = file.ContentType,
            SizeInBytes = file.Length,
            UploadedByUserId = user.Id,
            UploadedAt = DateTime.UtcNow
        };

        _context.PoeFiles.Add(entity);
        await _context.SaveChangesAsync();

        var created = await _context.PoeFiles.Include(item => item.UploadedByUser).FirstAsync(item => item.Id == entity.Id);
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsSubmissionAttachment", entity.Id, "Upload", null, created.ToResponse(HttpContext), user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await _workflowGovernanceService.CreateWorkflowNotificationsAsync(GetRelevantUserIds(submission), NotificationType.Submission, "OPMS evidence uploaded", $"A POE file was uploaded for OPMS submission '{id}'.", "OpmsSubmission", id);
        return Ok(new ApiResponse<PoeFileResponse>(true, created.ToResponse(HttpContext)));
    }

    [HttpDelete("{id}/attachments/{attachmentId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAttachment(string id, string attachmentId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<bool>(false, false, "User not found"));

        var submission = await _context.OpmsSubmissions
            .Include(item => item.OpmsTarget)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (submission == null) return NotFound(new ApiResponse<bool>(false, false, "OPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.POE.Upload", BuildScope(submission));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<bool>(false, false, decision.Reason));

        var file = await _context.PoeFiles.Include(item => item.UploadedByUser).FirstOrDefaultAsync(item =>
            item.Id == attachmentId &&
            item.SubmissionKind == SubmissionKind.Opms &&
            item.SubmissionId == id);
        if (file == null) return NotFound(new ApiResponse<bool>(false, false, "Attachment not found"));

        var before = file.ToResponse(HttpContext);
        var absolutePath = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), file.StoragePath);
        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }

        _context.PoeFiles.Remove(file);
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsSubmissionAttachment", attachmentId, "Delete", before, null, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        return Ok(new ApiResponse<bool>(true, true));
    }

    [HttpPost("{id}/submit")]
    public Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> Submit(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "OPMS.Submissions.Submit", "submitted", "Submit", NotificationType.Submission, request);

    [HttpPost("{id}/verify")]
    public Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> Verify(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "OPMS.Submissions.Verify", "verified", "Verify", NotificationType.Approval, request);

    [HttpPost("{id}/verify-reject")]
    public Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> VerifyReject(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "OPMS.Submissions.VerifyReject", "verify_rejected", "VerifyReject", NotificationType.VerifyRejection, request);

    [HttpPost("{id}/approve")]
    public Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> Approve(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "OPMS.Submissions.Approve", "approved", "Approve", NotificationType.Approval, request);

    [HttpPost("{id}/reject")]
    public Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> Reject(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "OPMS.Submissions.Reject", "rejected", "Reject", NotificationType.Rejection, request);

    [HttpPost("{id}/review")]
    public Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> Review(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "OPMS.Submissions.Review", "reviewed", "Review", NotificationType.Rfi, request);

    [HttpPost("{id}/audit")]
    public Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> Audit(string id, [FromBody] SubmissionWorkflowActionRequest request) =>
        ApplyWorkflowAction(id, "OPMS.Submissions.Audit", "audited", "Audit", NotificationType.InternalAuditRfi, request);

    [HttpPost("{id}/score")]
    public async Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> Score(string id, [FromBody] SubmissionWorkflowActionRequest request)
    {
        if (request.Score == null) return BadRequest(new ApiResponse<OpmsSubmissionResponse>(false, null, "Score is required"));

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsSubmissionResponse>(false, null, "User not found"));

        var entity = await _context.OpmsSubmissions.Include(item => item.OpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<OpmsSubmissionResponse>(false, null, "OPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Submissions.Score", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsSubmissionResponse>(false, null, decision.Reason));

        _context.SubmissionScores.Add(new SubmissionScore
        {
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = id,
            Score = request.Score.Value,
            Notes = request.Comment,
            ScoredByUserId = user.Id,
            ScoredAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsSubmission", id, "Score", null, new { request.Score, request.Comment }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));

        var response = await FindSubmissionAsync(id) ?? entity;
        return Ok(new ApiResponse<OpmsSubmissionResponse>(true, response.ToResponse()));
    }

    [HttpPost("{id}/extend-due-date")]
    public async Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> ExtendDueDate(string id, [FromBody] DueDateExtensionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsSubmissionResponse>(false, null, "User not found"));

        var entity = await _context.OpmsSubmissions.Include(item => item.OpmsTarget).FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<OpmsSubmissionResponse>(false, null, "OPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, "OPMS.Submissions.ExtendDueDate", BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsSubmissionResponse>(false, null, decision.Reason));

        var previousDueDate = entity.DueDate ?? DateTime.UtcNow;
        entity.DueDate = request.ExtendedDueDate;
        _context.DueDateExtensions.Add(new DueDateExtension
        {
            SubmissionKind = SubmissionKind.Opms,
            SubmissionId = id,
            OriginalDueDate = previousDueDate,
            ExtendedDueDate = request.ExtendedDueDate,
            Reason = request.Reason,
            ApprovedByUserId = user.Id,
            ApprovedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsSubmission", id, "ExtendDueDate", new { DueDate = previousDueDate }, new { DueDate = request.ExtendedDueDate, request.Reason }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await _workflowGovernanceService.CreateWorkflowNotificationsAsync(GetRelevantUserIds(entity), NotificationType.DueDateExtension, "OPMS due date extended", $"The due date for OPMS submission '{entity.Id}' was extended.", "OpmsSubmission", id);

        var response = await FindSubmissionAsync(id) ?? entity;
        return Ok(new ApiResponse<OpmsSubmissionResponse>(true, response.ToResponse()));
    }

    private async Task<ActionResult<ApiResponse<OpmsSubmissionResponse>>> ApplyWorkflowAction(string id, string permissionCode, string status, string action, NotificationType notificationType, SubmissionWorkflowActionRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new ApiResponse<OpmsSubmissionResponse>(false, null, "User not found"));

        var entity = await _context.OpmsSubmissions
            .Include(item => item.OpmsTarget)
            .Include(item => item.SubmittedByUser)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (entity == null) return NotFound(new ApiResponse<OpmsSubmissionResponse>(false, null, "OPMS submission not found"));

        var decision = await _accessControlService.CheckPermissionAsync(user, permissionCode, BuildScope(entity));
        if (!decision.Allowed) return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<OpmsSubmissionResponse>(false, null, decision.Reason));

        var before = await FindSubmissionAsync(id);
        entity.Status = status;
        if (string.Equals(status, "submitted", StringComparison.OrdinalIgnoreCase))
        {
            entity.SubmittedAt = DateTime.UtcNow;
            entity.SubmittedByUserId = user.Id;
        }
        else if (string.Equals(status, "verified", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "verify_rejected", StringComparison.OrdinalIgnoreCase))
        {
            entity.VerifierUserId = user.Id;
            entity.VerifiedAt = DateTime.UtcNow;
            entity.VerifierComments = request.Comment;
        }
        else if (string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            entity.ApproverUserId = user.Id;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApproverComments = request.Comment;
        }
        else if (string.Equals(status, "audited", StringComparison.OrdinalIgnoreCase))
        {
            entity.AuditorUserId = user.Id;
            entity.AuditedAt = DateTime.UtcNow;
            entity.AuditorComments = request.Comment;
        }
        else if (string.Equals(status, "reviewed", StringComparison.OrdinalIgnoreCase))
        {
            entity.PmsOfficerUserId = user.Id;
            entity.PmsReviewedAt = DateTime.UtcNow;
            entity.PmsComments = request.Comment;
        }
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            _context.ReviewComments.Add(new ReviewComment
            {
                SubmissionKind = SubmissionKind.Opms,
                SubmissionId = id,
                Comment = request.Comment,
                CommentedByUserId = user.Id,
                CommentedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        await _workflowGovernanceService.WriteAuditTrailAsync("OpmsSubmission", id, action, before?.ToResponse(), new { Status = status, request.Comment }, user.Id, PerformanceApiSupport.GetIpAddress(HttpContext));
        await _workflowGovernanceService.CreateWorkflowNotificationsAsync(GetRelevantUserIds(entity), notificationType, $"OPMS submission {action}", $"OPMS submission '{entity.Id}' was marked as {status}.", "OpmsSubmission", id);

        var after = await FindSubmissionAsync(id) ?? entity;
        return Ok(new ApiResponse<OpmsSubmissionResponse>(true, after.ToResponse()));
    }

    private IEnumerable<string> GetRelevantUserIds(OpmsSubmission submission)
    {
        return new[] { submission.SubmittedByUserId, submission.OpmsTarget.AssignedUserId }.Where(id => !string.IsNullOrWhiteSpace(id))!;
    }

    private Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = PerformanceApiSupport.GetCurrentUserId(User);
        return string.IsNullOrWhiteSpace(userId) ? Task.FromResult<ApplicationUser?>(null) : _userManager.FindByIdAsync(userId);
    }

    private Task<OpmsSubmission?> FindSubmissionAsync(string id)
    {
        return _context.OpmsSubmissions
            .Include(item => item.OpmsTarget).ThenInclude(target => target.Department)
            .Include(item => item.OpmsTarget).ThenInclude(target => target.Unit)
            .Include(item => item.SubmittedByUser)
            .Include(item => item.VerifierUser)
            .Include(item => item.ApproverUser)
            .Include(item => item.PmsOfficerUser)
            .Include(item => item.AuditorUser)
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    private static AccessScopeContext BuildScope(OpmsSubmission submission) => BuildScope(submission.OpmsTarget);

    private static AccessScopeContext BuildScope(OpmsTarget target) =>
        new(target.DepartmentId, target.UnitId, target.Id, null);
}
