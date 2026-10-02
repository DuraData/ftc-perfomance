using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;
using FTCERP.Host.Application.Services;

namespace FTCERP.Host.Infrastructure.Security;

public sealed class PoeDisposalWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PoeDisposalWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = Math.Clamp(configuration.GetValue("EvidenceDisposal:PollSeconds", 30), 5, 600);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
        do
        {
            try { await ProcessBatch(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "POE disposal batch failed"); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<int> ProcessBatch(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IEvidenceBlobStorage>();
        var requested = await context.PoeDisposalEvents.IgnoreQueryFilters()
            .Where(item => item.Action == PoeDisposalAction.Requested)
            .OrderBy(item => item.OccurredAt)
            .Take(25)
            .ToArrayAsync(cancellationToken);
        var processed = 0;
        foreach (var request in requested)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var finalized = await context.PoeDisposalEvents.IgnoreQueryFilters().AnyAsync(item => item.DisposalId == request.DisposalId && item.Action != PoeDisposalAction.Requested, cancellationToken);
            if (finalized) { await transaction.RollbackAsync(cancellationToken); continue; }
            var evidence = await context.PoeFiles.IgnoreQueryFilters().Include(item => item.Blob).Include(item => item.LegalHoldEvents).SingleOrDefaultAsync(item => item.Id == request.PoeFileId, cancellationToken);
            EvidenceStorageOperationResult result;
            if (evidence == null) result = new(false, "Evidence metadata no longer exists; physical disposal was not attempted.");
            else if (evidence.IsActive) result = new(false, "Evidence became active after disposal was requested; content was preserved.");
            else if (!evidence.RetainUntil.HasValue || evidence.RetainUntil.Value > DateTime.UtcNow) result = new(false, "The retention period is no longer eligible for disposal; content was preserved.");
            else if (PoeLegalHoldPolicy.HasAnyActiveHold(evidence.LegalHoldEvents)) result = new(false, "A legal hold became active after disposal was requested; content was preserved.");
            else if (evidence.Blob.IsContentDeleted) result = new(true, "The physical blob was already marked deleted; association disposal is idempotently complete.");
            else
            {
                var anotherPoeAssociationRequiresContent = await context.PoeFiles.IgnoreQueryFilters().AnyAsync(item => item.EvidenceBlobId == evidence.EvidenceBlobId && item.Id != evidence.Id && !item.DisposalEvents.Any(evt => evt.Action == PoeDisposalAction.Completed), cancellationToken);
                var idpAssociationRequiresContent = await context.IdpDocuments.IgnoreQueryFilters().AnyAsync(item => item.EvidenceBlobId == evidence.EvidenceBlobId, cancellationToken);
                if (anotherPoeAssociationRequiresContent || idpAssociationRequiresContent)
                    result = new(true, "The POE association was disposed while the shared physical blob was retained for another governed association.");
                else
                {
                    result = await storage.DisposeAsync(evidence.Blob.StorageKey, cancellationToken);
                    if (result.Succeeded) { evidence.Blob.IsContentDeleted = true; evidence.Blob.ContentDeletedAt = DateTime.UtcNow; }
                }
            }
            var finalEvent = new PoeDisposalEvent
            {
                DisposalId = request.DisposalId,
                MunicipalityId = request.MunicipalityId,
                PoeFileId = request.PoeFileId,
                Action = result.Succeeded ? PoeDisposalAction.Completed : PoeDisposalAction.Failed,
                Reason = request.Reason,
                ApprovalReference = request.ApprovalReference,
                ActorUserId = request.ActorUserId,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = request.CorrelationId,
                Detail = result.Detail
            };
            context.PoeDisposalEvents.Add(finalEvent);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                processed++;
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                foreach (var entry in context.ChangeTracker.Entries<PoeDisposalEvent>().Where(entry => entry.State == EntityState.Added)) entry.State = EntityState.Detached;
                logger.LogWarning(exception, "POE disposal {DisposalId} was finalized concurrently", request.DisposalId);
            }
        }
        return processed;
    }
}
