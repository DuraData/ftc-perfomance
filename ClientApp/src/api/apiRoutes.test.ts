import { afterEach, describe, expect, it, vi } from 'vitest';
import { approveStrategicDocument, changePassword, closeEmployeeAssignment, commitIdpHierarchyImport, commitIdpImport, createStrategicDocumentType, createStrategicDocumentVersion, createTidVersion, enableMfa, getAuditTrails, getAuthSessions, getC88ReportsPage, getC88Workspace, getIdpImportBatches, getIpmsPerformanceDashboard, getIpmsTargetsPage, getMfaStatus, getNotifications, getOpmsPerformanceDashboard, getOpmsSubmissionAttachments, getOpmsSubmissionsPage, getOpmsTargets, getOpmsTargetsPage, getPendingNotificationDeliveries, getPerformanceTargetRevisions, getPositionMasters, getReportingPeriodMasters, getStrategicDocumentHistory, getStrategicDocumentsPage, getTidConfiguration, getTidHistory, getTidRegisterPage, getVoteNumberMasters, getWardMasters, getWorkflowQueue, publishStrategicDocument, releaseOpmsEvidenceLegalHold, replaceOpmsSubmissionAttachment, requestOpmsEvidenceDisposal, requestPasswordReset, resetPassword, revokeAllAuthSessions, savePositionMaster, saveVoteNumberMaster, setupMfa, stageIdpHierarchyImport, stageIdpKpiImport, updateTidConfiguration, withdrawOpmsSubmission, withdrawOpmsTarget } from './api';

describe('versioned API routes', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('uses the permission-protected versioned audit route', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);
    await getAuditTrails(250, { entityName: 'OpmsTarget', entityId: 'target-public-id' });
    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/v1/audit/trails?take=250&entityName=OpmsTarget&entityId=target-public-id'), expect.objectContaining({ credentials: 'include' }));
  });

  it('transports bounded target-detail filters to submission and related-KPI pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      success: true,
      data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getOpmsSubmissionsPage({ page: 1, pageSize: 100, targetPublicId: 'target-public-id' });
    await getIpmsTargetsPage({ page: 1, pageSize: 100, relatedOpmsTargetPublicId: 'target-public-id' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/opms-submissions/page?page=1&pageSize=100&targetPublicId=target-public-id'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/ipms-targets/page?page=1&pageSize=100&relatedOpmsTargetPublicId=target-public-id'), expect.objectContaining({ credentials: 'include' }));
  });

  it('uses versioned IDP reconciliation routes and transports idempotency plus concurrency', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);
    const row = { sourceRowNumber: 2, projectCode: 'P1', kpiCode: 'K1', kpiName: 'KPI', description: 'Description', formula: 'x', baseline: 0, annualTarget: 1, fiveYearTarget: 5, dataSource: 'System', reportingFrequency: 'Quarterly', indicatorType: 'Output', circular88Linked: false, treasuryTidLinked: false };

    await getIdpImportBatches('plan-public-id');
    await stageIdpKpiImport('plan-public-id', { clientRequestId: 'request-id', sourceFileName: 'kpis.csv', rows: [row] });
    await commitIdpImport('batch-public-id', { rowVersion: 'AQ==', reason: 'Approved reconciliation' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/idp/plans/plan-public-id/imports'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/idp/plans/plan-public-id/imports/kpis/stage'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('"clientRequestId":"request-id"') }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/idp/imports/batch-public-id/commit'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ rowVersion: 'AQ==', reason: 'Approved reconciliation' }) }));
  });

  it('uses separately authorized hierarchy stage and commit routes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);
    const row = { sourceRowNumber: 2, outcomeCode: 'OUT', outcomeName: 'Outcome', outcomeDescription: 'Description', outcomeSortOrder: 1, objectiveCode: 'OBJ', objectiveName: 'Objective', objectiveDescription: 'Description', objectiveBaseline: 1, objectiveTarget: 2, objectiveDepartmentCode: null, objectiveStartDate: '2027-01-01T00:00:00.000Z', objectiveEndDate: '2030-01-01T00:00:00.000Z', objectiveBudget: 10, objectiveSortOrder: 1, priorityCode: 'PRI', priorityName: 'Priority', priorityDescription: 'Description', prioritySortOrder: 1, programmeCode: 'PROG', programmeName: 'Programme', programmeDescription: 'Description', programmeDepartmentCode: null, programmePlannedBudget: 10, programmeApprovedBudget: 10, programmeActualExpenditure: 0, projectCode: 'PROJECT', projectName: 'Project', projectDescription: 'Description', projectCategory: 'Capital', projectDepartmentCode: null, projectBudget: 10, projectFundingSource: 'Grant', projectStartDate: '2027-01-01T00:00:00.000Z', projectEndDate: '2029-01-01T00:00:00.000Z', projectStatus: 'Planned', communityNeedReference: null };

    await stageIdpHierarchyImport('plan-id', { clientRequestId: 'hierarchy-request', sourceFileName: 'hierarchy.csv', rows: [row] });
    await commitIdpHierarchyImport('batch-id', { rowVersion: 'AQ==', reason: 'Approved' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/idp/plans/plan-id/imports/hierarchy/stage'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('hierarchy-request') }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/idp/imports/batch-id/commit-hierarchy'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ rowVersion: 'AQ==', reason: 'Approved' }) }));
  });

  it('uses versioned TID configuration, register, history and lineage routes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);
    const payload = { indicatorDefinition: 'Definition', purpose: 'Purpose', dataSource: 'Source', collectionMethod: 'Collect', calculationMethod: 'Calculate', numeratorDescription: null, denominatorDescription: null, limitations: null, assumptions: null, verificationMethod: 'Verify', responsibleEmployeePublicId: null, notes: null, effectiveFrom: '2026-07-01T00:00:00Z', previousVersionRowVersion: 'AQ==', reason: 'Approved' };

    await getTidConfiguration();
    await updateTidConfiguration({ tidEnabled: true, allKpisRequired: true, rowVersion: 'AQ==', reason: 'Approved policy' });
    await getTidRegisterPage({ page: 2, pageSize: 25, search: 'water', sortBy: 'indicatorNumber', sortDirection: 'asc' });
    await getTidHistory('target-id');
    await createTidVersion('target-id', payload);

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/tids/configuration'), expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/tids/configuration'), expect.objectContaining({ method: 'PUT', body: expect.stringContaining('Approved policy') }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/tids/page?page=2&pageSize=25&search=water&sortBy=indicatorNumber&sortDirection=asc'), expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/tids/targets/target-id'), expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(5, expect.stringContaining('/v1/tids/targets/target-id/versions'), expect.objectContaining({ method: 'POST', body: JSON.stringify(payload) }));
  });

  it('uses versioned strategic-document type, version, history and lifecycle routes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await createStrategicDocumentType({ code: 'IDP', name: 'Integrated Development Plan', allowsExternalLinks: true, isActive: true, displayOrder: 10, reason: 'Controlled type' });
    await getStrategicDocumentsPage({ page: 2, pageSize: 25, search: 'plan', sortBy: 'title', sortDirection: 'asc' }, { municipalityFinancialYearPublicId: 'year-id' });
    await createStrategicDocumentVersion({ municipalityFinancialYearPublicId: 'year-id', documentTypePublicId: 'type-id', title: 'Approved IDP', documentDate: '2026-07-01T00:00:00Z', displayOrder: 10, externalUrl: 'https://example.gov.za/idp.pdf', reason: 'Version reason' });
    await getStrategicDocumentHistory('family-id');
    await approveStrategicDocument('document-id', { rowVersion: 'AQ==', approvalReference: 'Council 1/2026', reason: 'Approved' });
    await publishStrategicDocument('document-id', { rowVersion: 'Ag==', publicationDate: '2026-07-02T00:00:00Z', reason: 'Published' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/strategic-documents/types'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('Controlled type') }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/strategic-documents/page?page=2&pageSize=25&search=plan&sortBy=title&sortDirection=asc&municipalityFinancialYearPublicId=year-id'), expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/strategic-documents/versions'), expect.objectContaining({ method: 'POST', body: expect.any(FormData) }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/strategic-documents/families/family-id/versions'), expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(5, expect.stringContaining('/v1/strategic-documents/document-id/approve'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('Council 1/2026') }));
    expect(fetchMock).toHaveBeenNthCalledWith(6, expect.stringContaining('/v1/strategic-documents/document-id/publish'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('2026-07-02') }));
  });

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

  it('uses governed ward and vote-number master routes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getWardMasters();
    await getVoteNumberMasters();
    await saveVoteNumberMaster('vote-public-id', { departmentPublicId: 'department-public-id', code: 'V01', number: '001', name: 'Operating Vote', amount: 1250, isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', reason: 'Council approved budget', rowVersion: 'AQ==' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/masters/wards'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/masters/vote-numbers'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/masters/vote-numbers/vote-public-id'), expect.objectContaining({ method: 'PUT', body: expect.stringContaining('"rowVersion":"AQ=="') }));
  });

  it('uses governed session-list and revoke-all routes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getAuthSessions();
    await revokeAllAuthSessions('User requested global sign out');

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/auth/sessions'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/auth/sessions/revoke-all'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ reason: 'User requested global sign out' }) }));
  });

  it('uses versioned MFA status, setup, and enable routes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: {} }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getMfaStatus();
    await setupMfa();
    await enableMfa('123456');

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/auth/mfa/status'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/auth/mfa/setup'), expect.objectContaining({ method: 'POST' }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/auth/mfa/enable'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ code: '123456' }) }));
  });

  it('uses the versioned password-change route without exposing credentials in the URL', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: true }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await changePassword('OldPassword1!', 'NewPassword2@');

    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/v1/auth/password/change'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ currentPassword: 'OldPassword1!', newPassword: 'NewPassword2@' }) }));
  });

  it('keeps password-reset email, token, and password in versioned POST bodies', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: true }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await requestPasswordReset('person@example.test');
    await resetPassword('person@example.test', 'one-time-token', 'A-Strong-New-Password9!');

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/auth/password/forgot'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ email: 'person@example.test' }) }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/auth/password/reset'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ email: 'person@example.test', token: 'one-time-token', newPassword: 'A-Strong-New-Password9!' }) }));
    expect(String(fetchMock.mock.calls[1][0])).not.toContain('one-time-token');
  });

  it('uses the versioned OPMS target route for relational mapping responses', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getOpmsTargets();

    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/v1/opms-targets'), expect.objectContaining({ credentials: 'include' }));
  });

  it('loads OPMS and IPMS dashboard aggregates from bounded versioned routes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: {} }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getOpmsPerformanceDashboard();
    await getIpmsPerformanceDashboard();

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/performance-dashboards/opms'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/performance-dashboards/ipms'), expect.objectContaining({ credentials: 'include' }));
  });

  it('loads a bounded workflow queue through the versioned combined route', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: {} }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getWorkflowQueue('under-approval', 3, 20);

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/workflow-queues?queue=under-approval&page=3&pageSize=20'),
      expect.objectContaining({ credentials: 'include' }),
    );
  });

  it('projects OPMS API data without inheriting production fixture values', async () => {
    const dto = {
      id: 'target-live', publicId: 'target-live-public', rowVersion: 'AQ==', periodId: 42,
      departmentId: 7, departmentName: 'Live Water Services', unitId: null, unitName: null,
      assignedUserId: 'employee-live', assignedUserName: 'Live Owner', wardIds: [9],
      additionalAssigneeIds: ['employee-two'], voteNumberIds: [12], indicatorNumber: 'LIVE-001',
      nationalKpa: 'Infrastructure', municipalKpa: 'Water', strategicGoalId: 3,
      strategicObjectiveId: 4, performanceObjective: 'Deliver water', targetName: 'Live KPI',
      kpiDescription: 'Server supplied description', baseline: 10, annualTarget: 20,
      annualTargetDescription: 'Twenty', budgetSourceId: 5, budgetTypeId: 6,
      unitOfMeasureId: 8, weight: 15, kpiType: 'Outcome', indicatorType: 'Quantitative',
      isRevised: false, isWithdrawn: false, targetUnitType: 'absolute_count',
      createdAt: '2026-07-01T00:00:00Z',
    };
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [dto] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    const result = await getOpmsTargets();
    const target = result.data?.[0];

    expect(target).toMatchObject({
      id: 'target-live', targetName: 'Live KPI', indicatorNumber: 'LIVE-001',
      department: { id: '7', name: 'Live Water Services' },
      assignedTo: { id: 'employee-live', displayName: 'Live Owner' },
      period: { id: '42', name: 'Reporting period 42' },
      strategicGoal: { id: '3', name: 'Not supplied by API' },
    });
    expect(target?.submissions).toEqual([]);
    expect(target?.relatedIPMSTargets).toEqual([]);
    expect(target?.attachments).toEqual([]);
    expect(target?.additionalAssignees).toEqual([expect.objectContaining({ id: 'employee-two', displayName: 'Not supplied by API' })]);
  });

  it('projects submission actors and evidence uploaders only from API DTOs', async () => {
    const page = {
      items: [{
        id: 'submission-live', rowVersion: 'Ag==', baseState: 'SUBMITTED', opmsTargetId: 'target-live',
        targetName: 'Live KPI', quarter: 'Q1', status: 'submitted', actual: 4,
        submittedByUserId: 'submitter-live', submittedByName: 'Live Submitter',
        verifierUserId: 'verifier-live', verifierName: 'Live Verifier', createdAt: '2026-08-01T00:00:00Z',
      }],
      page: 1, pageSize: 25, totalCount: 1, totalPages: 1,
    };
    const attachment = {
      id: 'file-live', submissionKind: 'Opms', submissionId: 'submission-live', fileName: 'evidence.pdf',
      sizeInBytes: 512, uploadedByUserId: 'uploader-live', uploadedByName: 'Live Uploader',
      uploadedAt: '2026-08-01T00:00:00Z', url: '/files/file-live',
    };
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ success: true, data: page }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ success: true, data: [attachment] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    const submissions = await getOpmsSubmissionsPage();
    const attachments = await getOpmsSubmissionAttachments('submission-live');

    expect(submissions.data?.items[0]).toMatchObject({
      target: { id: 'target-live', targetName: 'Live KPI' },
      submitter: { id: 'submitter-live', displayName: 'Live Submitter' },
      verifier: { id: 'verifier-live', displayName: 'Live Verifier' },
      attachments: [], comments: [], history: [],
    });
    expect(attachments.data?.[0].uploadedBy).toMatchObject({ id: 'uploader-live', displayName: 'Live Uploader' });
  });

  it('encodes bounded register paging and allow-listed sort parameters', async () => {
    const page = { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 };
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: page }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getOpmsTargetsPage({ page: 2, pageSize: 25, search: ' water ', sortBy: 'targetName', sortDirection: 'asc' });
    await getIpmsTargetsPage({ page: 1, pageSize: 100, sortBy: 'createdAt', sortDirection: 'desc' });
    await getPendingNotificationDeliveries({ page: 3, pageSize: 10, search: ' timeout ', sortBy: 'attemptCount', sortDirection: 'desc' });
    await getNotifications({ page: 1, pageSize: 8, sortBy: 'createdAt', sortDirection: 'desc' });
    await getC88Workspace('year-1', false);
    await getC88ReportsPage({ page: 2, pageSize: 25, search: ' water ', sortBy: 'indicatorCode', sortDirection: 'asc' }, 'year-1');

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/opms-targets/page?page=2&pageSize=25&search=water&sortBy=targetName&sortDirection=asc'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/ipms-targets/page?page=1&pageSize=100&sortBy=createdAt&sortDirection=desc'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/notification-operations/pending/page?page=3&pageSize=10&search=timeout&sortBy=attemptCount&sortDirection=desc'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/notifications/page?page=1&pageSize=8&sortBy=createdAt&sortDirection=desc'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(5, expect.stringContaining('/v1/c88/workspace?municipalityFinancialYearPublicId=year-1&includeReports=false'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(6, expect.stringContaining('/v1/c88/reports/page?page=2&pageSize=25&search=water&sortBy=indicatorCode&sortDirection=asc&municipalityFinancialYearPublicId=year-1'), expect.objectContaining({ credentials: 'include' }));
  });

  it('posts reasons and concurrency tokens to governed withdrawal routes', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ success: true }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ success: true }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await withdrawOpmsTarget('target-1', { reason: 'Approved plan superseded it', rowVersion: 'AQ==' });
    await withdrawOpmsSubmission('submission-1', { reason: 'Submission entered in error', rowVersion: 'Ag==' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/opms-targets/target-1/withdraw'), expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ reason: 'Approved plan superseded it', rowVersion: 'AQ==' }),
    }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/opms-submissions/submission-1/withdraw'), expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ reason: 'Submission entered in error', rowVersion: 'Ag==' }),
    }));
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
