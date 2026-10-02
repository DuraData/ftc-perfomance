import { afterEach, describe, expect, it, vi } from 'vitest';
import { closeEmployeeAssignment, getAuthSessions, getPerformanceTargetRevisions, getPositionMasters, getReportingPeriodMasters, releaseOpmsEvidenceLegalHold, replaceOpmsSubmissionAttachment, requestOpmsEvidenceDisposal, revokeAllAuthSessions, savePositionMaster } from './api';

describe('versioned API routes', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('loads performance target revisions from the controller route', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }));
    vi.stubGlobal('fetch', fetchMock);

    await getPerformanceTargetRevisions('target-value-public-id');

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/performance-period-targets/target-value-public-id/revisions'),
      expect.objectContaining({ credentials: 'include' }),
    );
  });

  it('uses the versioned tenant-master routes for periods and governed assignment closure', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }));
    vi.stubGlobal('fetch', fetchMock);

    await getReportingPeriodMasters();
    await closeEmployeeAssignment('assignment-public-id', { effectiveTo: '2026-10-02T00:00:00Z', reason: 'Employee transferred to Finance', rowVersion: 'AQ==' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/masters/reporting-periods'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/masters/employee-assignments/assignment-public-id/close'), expect.objectContaining({
      method: 'PUT',
      body: JSON.stringify({ effectiveTo: '2026-10-02T00:00:00Z', reason: 'Employee transferred to Finance', rowVersion: 'AQ==' }),
    }));
  });

  it('uses versioned organization-master routes and carries optimistic concurrency', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getPositionMasters();
    await savePositionMaster('position-public-id', { departmentPublicId: 'department-public-id', unitPublicId: null, code: 'CFO', name: 'Chief Financial Officer', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', effectiveTo: null, reason: 'Council approved establishment', rowVersion: 'AQ==' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/masters/positions'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/masters/positions/position-public-id'), expect.objectContaining({ method: 'PUT', body: expect.stringContaining('"rowVersion":"AQ=="') }));
  });

  it('uses governed session-list and revoke-all routes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getAuthSessions();
    await revokeAllAuthSessions('User requested global sign out');

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/auth/sessions'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/auth/sessions/revoke-all'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ reason: 'User requested global sign out' }) }));
  });

  it('posts both concurrency tokens to the governed POE replacement route', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { id: 'new-id', publicId: 'new-public', submissionKind: 'Opms', submissionId: 'submission-1', fileName: 'new.pdf', sizeInBytes: 100, uploadedByUserId: 'owner', uploadedAt: '2026-10-02T00:00:00Z', url: '', rowVersion: 'AAAAAAAAAAM=' } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await replaceOpmsSubmissionAttachment('submission-1', 'old-id', { replacementEvidencePublicId: 'new-public', reason: 'Corrected signed version', supersededRowVersion: 'AAAAAAAAAAE=', replacementRowVersion: 'AAAAAAAAAAI=' });

    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/opms-submissions/submission-1/attachments/old-id/replace'), expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ replacementEvidencePublicId: 'new-public', reason: 'Corrected signed version', supersededRowVersion: 'AAAAAAAAAAE=', replacementRowVersion: 'AAAAAAAAAAI=' }),
    }));
  });

  it('posts a reason to the legal-hold release route', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { id: 'file-1', submissionKind: 'Opms', submissionId: 'submission-1', fileName: 'proof.pdf', sizeInBytes: 100, uploadedByUserId: 'owner', uploadedAt: '2026-10-02T00:00:00Z', url: '' } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await releaseOpmsEvidenceLegalHold('submission-1', 'file-1', '9a98920c-78c5-4f38-9b3d-aa243d68d272', { reason: 'Matter concluded' });

    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/opms-submissions/submission-1/attachments/file-1/legal-holds/9a98920c-78c5-4f38-9b3d-aa243d68d272/release'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ reason: 'Matter concluded' }) }));
  });

  it('posts approval evidence and concurrency token to the disposal route', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { id: 'file-1', submissionKind: 'Opms', submissionId: 'submission-1', fileName: 'proof.pdf', sizeInBytes: 100, uploadedByUserId: 'owner', uploadedAt: '2026-10-02T00:00:00Z', url: '', isActive: false } }), { status: 202, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await requestOpmsEvidenceDisposal('submission-1', 'file-1', { approvalReference: 'COUNCIL-2026-42', reason: 'Retention period completed', rowVersion: 'AAAAAAAAAAE=' });

    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/opms-submissions/submission-1/attachments/file-1/disposals'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ approvalReference: 'COUNCIL-2026-42', reason: 'Retention period completed', rowVersion: 'AAAAAAAAAAE=' }) }));
  });
});
