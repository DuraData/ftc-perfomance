import { afterEach, describe, expect, it, vi } from 'vitest';
import { approveStrategicDocument, changePassword, closeEmployeeAssignment, commitIdpHierarchyImport, commitIdpImport, createStrategicDocumentType, createStrategicDocumentVersion, createTidVersion, enableMfa, getAuthenticationEventsPage, getAuditTrailsPage, getAuthSessions, getC88ReportsPage, getC88Workspace, getDepartmentMastersPage, getFinancialYearMastersPage, getIdpAlignmentMatrixPage, getIdpDocumentsPage, getIdpImportBatch, getIdpImportBatchesPage, getIdpPlansPage, getInternalAuditConfigurationsPage, getIpmsPerformanceDashboard, getIpmsTargetOptions, getIpmsTargetsPage, getIpmsTargetTemplatesPage, getLoginAuditLogs, getMfaStatus, getMunicipalityFinancialYearMastersPage, getNotificationPoliciesPage, getNotifications, getOfficialReportGenerationsPage, getOfficialReportJobsPage, getOfficialReportSchedulesPage, getOfficialReportTemplatesPage, getOpmsPerformanceDashboard, getOpmsSubmissionAttachmentsPage, getOpmsSubmissionsPage, getOpmsTargetOptions, getOpmsTargetsPage, getOpmsTargetTemplatesPage, getPendingNotificationDeliveries, getPerformanceRfisPage, getPerformanceTargetRevisions, getPositionMastersPage, getRatingSchemesPage, getReportingPeriodMastersPage, getReportingWindowExceptionsPage, getReportingWindowsPage, getRoleAccessMatrixPage, getSdbipLayerMastersPage, getStrategicDocumentHistory, getStrategicDocumentsPage, getStrategicDocumentTypesPage, getSubmissionStageRatingsPage, getTidConfiguration, getTidHistory, getTidRegisterPage, getUserAuthenticatorsPage, getVoteNumberMastersPage, getWardMastersPage, getWorkflowDefinitionsPage, getWorkflowQueue, getWorkingCalendarHolidaysPage, publishStrategicDocument, releaseOpmsEvidenceLegalHold, replaceOpmsSubmissionAttachment, requestOpmsEvidenceDisposal, requestPasswordReset, resetPassword, revokeAllAuthSessions, savePositionMaster, saveVoteNumberMaster, setupMfa, simulateAccess, stageIdpHierarchyImport, stageIdpKpiImport, updateTidConfiguration, withdrawOpmsSubmission, withdrawOpmsTarget } from './api';
import { getGlobalStrategicReferencesPage, getStrategicPlanningRelationshipsPage, getStrategicRiskLinksPage, getStrategicRisksPage, linkStrategicRisk, saveStrategicRisk, unlinkStrategicRisk } from './api';

describe('versioned API routes', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('transports bounded global strategic reference filters', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getGlobalStrategicReferencesPage('national-kpas', { page: 2, pageSize: 25, search: 'service', sortBy: 'name', sortDirection: 'asc' }, { active: true, enabledForMunicipality: false });

    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/v1/masters/national-kpas/page?page=2&pageSize=25&search=service&sortBy=name&sortDirection=asc&active=true&enabledForMunicipality=false'), expect.objectContaining({ credentials: 'include' }));
  });

  it('transports bounded strategic relationship filters and stable identifiers', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getStrategicPlanningRelationshipsPage(
      { page: 2, pageSize: 25, search: 'growth', sortBy: 'childName', sortDirection: 'asc' },
      { relationshipType: 'strategic-goal-objective', includeInactive: true, parentPublicId: 'parent-id', childPublicId: 'child-id' },
    );

    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/v1/strategic-planning/relationships/page?page=2&pageSize=25&search=growth&sortBy=childName&sortDirection=asc&relationshipType=strategic-goal-objective&includeInactive=true&parentPublicId=parent-id&childPublicId=child-id'), expect.objectContaining({ credentials: 'include' }));
  });

  it('transports bounded authentication identity and event filters', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getUserAuthenticatorsPage({ page: 2, pageSize: 25, search: 'person', sortBy: 'email', sortDirection: 'asc', active: false, providerCode: 'ENTRA' });
    await getAuthenticationEventsPage({ page: 3, pageSize: 25, search: 'denied', sortBy: 'occurredAt', sortDirection: 'desc', success: false, providerCode: 'ENTRA', eventType: 'ExternalSignIn' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/admin/authentication/authenticators/page?page=2&pageSize=25&search=person&sortBy=email&sortDirection=asc&active=false&providerCode=ENTRA'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/admin/authentication/events/page?page=3&pageSize=25&search=denied&sortBy=occurredAt&sortDirection=desc&success=false&providerCode=ENTRA&eventType=ExternalSignIn'), expect.objectContaining({ credentials: 'include' }));
  });

  it('transports public organization identifiers for permission simulation', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { allowed: true, reason: 'Allowed', effectivePermissions: [], matchedScopes: [], matchedAssignments: [] } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await simulateAccess({ userId: 'user-1', permissionCode: 'OPMS.Target.View', departmentId: null, departmentPublicId: 'department-public-id', unitId: null, unitPublicId: 'unit-public-id' });

    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/access/simulate'), expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ userId: 'user-1', permissionCode: 'OPMS.Target.View', departmentId: null, departmentPublicId: 'department-public-id', unitId: null, unitPublicId: 'unit-public-id' }),
    }));
  });

  it('transports bounded role access matrix search, sorting, and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getRoleAccessMatrixPage({ page: 2, pageSize: 25, search: 'reviewer', sortBy: 'code', sortDirection: 'asc' });

    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/access/role-access-matrix/page?page=2&pageSize=25&search=reviewer&sortBy=code&sortDirection=asc'), expect.objectContaining({ credentials: 'include' }));
  });

  it('transports bounded audit administration search, sorting, filters, and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getLoginAuditLogs({ page: 2, pageSize: 25, search: 'locked', sortBy: 'email', sortDirection: 'asc' }, true);
    await getAuditTrailsPage({ page: 3, pageSize: 50, search: 'approve', sortBy: 'action', sortDirection: 'desc' }, { entityName: 'OpmsSubmission' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/audit/login-logs/page?page=2&pageSize=25&search=locked&sortBy=email&sortDirection=asc&failuresOnly=true'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/audit/trails/page?page=3&pageSize=50&search=approve&sortBy=action&sortDirection=desc&entityName=OpmsSubmission'), expect.objectContaining({ credentials: 'include' }));
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

    await getIdpImportBatchesPage('plan-public-id', { page: 2, pageSize: 25, search: 'council', status: 'Committed', importType: 'KPI', sortBy: 'fileName', sortDirection: 'asc' });
    await getIdpImportBatch('batch-public-id');
    await stageIdpKpiImport('plan-public-id', { clientRequestId: 'request-id', sourceFileName: 'kpis.csv', rows: [row] });
    await commitIdpImport('batch-public-id', { rowVersion: 'AQ==', reason: 'Approved reconciliation' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/idp/plans/plan-public-id/imports/page?page=2&pageSize=25&search=council&sortBy=fileName&sortDirection=asc&status=Committed&importType=KPI'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/idp/imports/batch-public-id'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/idp/plans/plan-public-id/imports/kpis/stage'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('"clientRequestId":"request-id"') }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/idp/imports/batch-public-id/commit'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ rowVersion: 'AQ==', reason: 'Approved reconciliation' }) }));
  });

  it('transports bounded IDP plan search, sorting, and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      success: true,
      data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getIdpPlansPage({ page: 2, pageSize: 25, search: 'five year', sortBy: 'planCode', sortDirection: 'asc' });
    await getIdpAlignmentMatrixPage('plan-public-id', { page: 3, pageSize: 25, search: 'service', sortBy: 'reference', sortDirection: 'desc' }, 'Circular88');
    await getIdpDocumentsPage('plan-public-id', { page: 2, pageSize: 25, search: 'policy', sortBy: 'scanStatus', sortDirection: 'asc' }, { category: 'Policy', scanStatus: 'Clean', quarantined: false });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/idp/plans/page?page=2&pageSize=25&search=five+year&sortBy=planCode&sortDirection=asc'),
      expect.objectContaining({ credentials: 'include' }),
    );
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/idp/plans/plan-public-id/alignment-matrix/page?page=3&pageSize=25&search=service&sortBy=reference&sortDirection=desc&frameworkType=Circular88'),
      expect.objectContaining({ credentials: 'include' }),
    );
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/idp/plans/plan-public-id/documents/page?page=2&pageSize=25&search=policy&sortBy=scanStatus&sortDirection=asc&category=Policy&scanStatus=Clean&quarantined=false'),
      expect.objectContaining({ credentials: 'include' }),
    );
  });

  it('transports bounded official-report job search, sorting, and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      success: true,
      data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getOfficialReportJobsPage(2, { page: 2, pageSize: 25, search: 'failed', sortBy: 'state', sortDirection: 'asc' });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/reports/official/jobs/page?page=2&pageSize=25&search=failed&sortBy=state&sortDirection=asc&kind=2'),
      expect.objectContaining({ credentials: 'include' }),
    );
  });

  it('transports bounded official-generation search, period, sorting, and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      success: true,
      data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getOfficialReportGenerationsPage(2, 'period-1', { page: 2, pageSize: 25, search: 'annual', sortBy: 'generatedAt', sortDirection: 'asc' });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/reports/official/generations/page?page=2&pageSize=25&search=annual&sortBy=generatedAt&sortDirection=asc&kind=2&reportingPeriodPublicId=period-1'),
      expect.objectContaining({ credentials: 'include' }),
    );
  });

  it('transports bounded official-report schedule search, sorting, history, and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      success: true,
      data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getOfficialReportSchedulesPage(1, true, { page: 2, pageSize: 25, search: 'quarterly', sortBy: 'nextRunAt', sortDirection: 'asc' });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/reports/official/schedules/page?page=2&pageSize=25&search=quarterly&sortBy=nextRunAt&sortDirection=asc&kind=1&includeHistory=true'),
      expect.objectContaining({ credentials: 'include' }),
    );
  });

  it('transports bounded official-report template search, sorting, history, year, and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      success: true,
      data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getOfficialReportTemplatesPage(1, true, 'year-1', { page: 2, pageSize: 25, search: 'quarterly', sortBy: 'code', sortDirection: 'asc' });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/reports/official/templates/page?page=2&pageSize=25&search=quarterly&sortBy=code&sortDirection=asc&kind=1&includeHistory=true&municipalityFinancialYearPublicId=year-1'),
      expect.objectContaining({ credentials: 'include' }),
    );
  });

  it('transports bounded notification-policy and working-calendar filters', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getNotificationPoliciesPage({ page: 2, pageSize: 25, search: 'annual', sortBy: 'effectiveFrom', sortDirection: 'desc' }, 'year-1', 2);
    await getWorkingCalendarHolidaysPage({ page: 3, pageSize: 25, search: 'heritage', sortBy: 'date', sortDirection: 'asc' }, 'year-1');

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/notification-policies/page?page=2&pageSize=25&search=annual&sortBy=effectiveFrom&sortDirection=desc&municipalityFinancialYearPublicId=year-1&lifecycle=2'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/notification-policies/holidays/page?page=3&pageSize=25&search=heritage&sortBy=date&sortDirection=asc&municipalityFinancialYearPublicId=year-1'), expect.objectContaining({ credentials: 'include' }));
  });

  it('transports bounded target-library filters, sorting, search, and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      success: true,
      data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);
    const query = { page: 2, pageSize: 25, search: 'water', status: 'active' as const, primaryArea: 'Services', functionalArea: 'Operations', classification: 'Outcome', targetUnitType: 'percentage', version: 2, sortBy: 'templateCode', sortDirection: 'asc' as const };

    await getOpmsTargetTemplatesPage(query);
    await getIpmsTargetTemplatesPage(query);

    const expected = 'page=2&pageSize=25&search=water&sortBy=templateCode&sortDirection=asc&status=active&primaryArea=Services&functionalArea=Operations&classification=Outcome&targetUnitType=percentage&version=2';
    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining(`/v1/opms-target-library/page?${expected}`), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining(`/v1/ipms-target-library/page?${expected}`), expect.objectContaining({ credentials: 'include' }));
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
    await getStrategicDocumentTypesPage({ page: 2, pageSize: 25, search: 'plan', sortBy: 'displayOrder', sortDirection: 'asc' }, { active: true, publicId: 'type-id' });
    await getStrategicDocumentsPage({ page: 2, pageSize: 25, search: 'plan', sortBy: 'title', sortDirection: 'asc' }, { municipalityFinancialYearPublicId: 'year-id' });
    await createStrategicDocumentVersion({ municipalityFinancialYearPublicId: 'year-id', documentTypePublicId: 'type-id', title: 'Approved IDP', documentDate: '2026-07-01T00:00:00Z', displayOrder: 10, externalUrl: 'https://example.gov.za/idp.pdf', reason: 'Version reason' });
    await getStrategicDocumentHistory('family-id');
    await approveStrategicDocument('document-id', { rowVersion: 'AQ==', approvalReference: 'Council 1/2026', reason: 'Approved' });
    await publishStrategicDocument('document-id', { rowVersion: 'Ag==', publicationDate: '2026-07-02T00:00:00Z', reason: 'Published' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/strategic-documents/types'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('Controlled type') }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/strategic-documents/types/page?page=2&pageSize=25&search=plan&sortBy=displayOrder&sortDirection=asc&active=true&publicId=type-id'), expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/strategic-documents/page?page=2&pageSize=25&search=plan&sortBy=title&sortDirection=asc&municipalityFinancialYearPublicId=year-id'), expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/strategic-documents/versions'), expect.objectContaining({ method: 'POST', body: expect.any(FormData) }));
    expect(fetchMock).toHaveBeenNthCalledWith(5, expect.stringContaining('/v1/strategic-documents/families/family-id/versions'), expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(6, expect.stringContaining('/v1/strategic-documents/document-id/approve'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('Council 1/2026') }));
    expect(fetchMock).toHaveBeenNthCalledWith(7, expect.stringContaining('/v1/strategic-documents/document-id/publish'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('2026-07-02') }));
  });

  it('uses governed strategic-risk register and KPI relationship routes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getStrategicRisksPage({ page: 2, pageSize: 25, search: 'water', sortBy: 'reference', sortDirection: 'asc' }, { active: true, municipalityFinancialYearPublicId: 'year-id' });
    await saveStrategicRisk(null, { riskReference: 'SR-1', riskTitle: 'Water', isActive: true, reason: 'Approved' });
    await saveStrategicRisk('risk/id', { riskTitle: 'Water updated', isActive: false, reason: 'Review', rowVersion: 'AQ==' });
    await getStrategicRiskLinksPage({ page: 3, pageSize: 10, sortBy: 'linkedAt', sortDirection: 'desc' }, { strategicRiskPublicId: 'risk/id', includeInactive: true });
    await linkStrategicRisk({ strategicRiskPublicId: 'risk/id', targetPublicId: 'target/id', isPrimary: true, reason: 'Material exposure' });
    await unlinkStrategicRisk('link/id', { reason: 'Mitigated', rowVersion: 'Ag==' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/strategic-risks/page?page=2&pageSize=25&search=water&sortBy=reference&sortDirection=asc&municipalityFinancialYearPublicId=year-id&active=true'), expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/strategic-risks'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('SR-1') }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/strategic-risks/risk%2Fid'), expect.objectContaining({ method: 'PUT', body: expect.stringContaining('AQ==') }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/strategic-risks/links/page?page=3&pageSize=10&sortBy=linkedAt&sortDirection=desc&strategicRiskPublicId=risk%2Fid&includeInactive=true'), expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(5, expect.stringContaining('/v1/strategic-risks/links'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('target/id') }));
    expect(fetchMock).toHaveBeenNthCalledWith(6, expect.stringContaining('/v1/strategic-risks/links/link%2Fid/unlink'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('Mitigated') }));
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

    await getReportingPeriodMastersPage({ page: 1, pageSize: 100 });
    await closeEmployeeAssignment('assignment-public-id', { effectiveTo: '2026-10-02T00:00:00Z', reason: 'Employee transferred to Finance', rowVersion: 'AQ==' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/masters/reporting-periods/page?page=1&pageSize=100'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/masters/employee-assignments/assignment-public-id/close'), expect.objectContaining({
      method: 'PUT',
      body: JSON.stringify({ effectiveTo: '2026-10-02T00:00:00Z', reason: 'Employee transferred to Finance', rowVersion: 'AQ==' }),
    }));
  });

  it('transports bounded calendar-master filters, types, sorting, and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getFinancialYearMastersPage({ page: 2, pageSize: 25, search: '2026', sortBy: 'startDate', sortDirection: 'desc', active: true });
    await getMunicipalityFinancialYearMastersPage({ page: 1, pageSize: 10, sortBy: 'current', sortDirection: 'desc', active: true, current: false });
    await getReportingPeriodMastersPage({ page: 3, pageSize: 25, search: 'quarter', sortBy: 'sequence', sortDirection: 'asc', municipalityFinancialYearId: 'year-id', reportingPeriodType: 1 });
    await getSdbipLayerMastersPage({ page: 1, pageSize: 25, sortBy: 'displayOrder', sortDirection: 'asc', active: false, municipalityFinancialYearId: 'year-id' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/masters/financial-years/page?page=2&pageSize=25&search=2026&sortBy=startDate&sortDirection=desc&active=true'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/masters/municipality-financial-years/page?page=1&pageSize=10&sortBy=current&sortDirection=desc&active=true&current=false'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/masters/reporting-periods/page?page=3&pageSize=25&search=quarter&sortBy=sequence&sortDirection=asc&reportingPeriodType=1&municipalityFinancialYearId=year-id'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/masters/sdbip-layers/page?page=1&pageSize=25&sortBy=displayOrder&sortDirection=asc&active=false&municipalityFinancialYearId=year-id'), expect.objectContaining({ credentials: 'include' }));
  });

  it('uses versioned organization-master routes and carries optimistic concurrency', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getPositionMastersPage({ page: 1, pageSize: 100 });
    await savePositionMaster('position-public-id', { departmentPublicId: 'department-public-id', unitPublicId: null, code: 'CFO', name: 'Chief Financial Officer', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', effectiveTo: null, reason: 'Council approved establishment', rowVersion: 'AQ==' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/masters/positions/page?page=1&pageSize=100'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/masters/positions/position-public-id'), expect.objectContaining({ method: 'PUT', body: expect.stringContaining('"rowVersion":"AQ=="') }));
  });

  it('transports bounded organization and reference master filters and sorting', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getDepartmentMastersPage({ page: 2, pageSize: 25, search: 'finance', sortBy: 'name', sortDirection: 'asc', active: true });
    await getPositionMastersPage({ page: 1, pageSize: 10, sortBy: 'grade', sortDirection: 'desc', departmentPublicId: 'department-id', unitPublicId: 'unit-id' });
    await getWardMastersPage({ page: 3, pageSize: 25, search: 'ward 12', sortBy: 'code', sortDirection: 'asc' });
    await getVoteNumberMastersPage({ page: 1, pageSize: 25, sortBy: 'amount', sortDirection: 'desc', active: false, departmentPublicId: 'department-id', municipalityFinancialYearPublicId: 'year-id' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/masters/departments/page?page=2&pageSize=25&search=finance&sortBy=name&sortDirection=asc&active=true'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/masters/positions/page?page=1&pageSize=10&sortBy=grade&sortDirection=desc&departmentPublicId=department-id&unitPublicId=unit-id'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/masters/wards/page?page=3&pageSize=25&search=ward+12&sortBy=code&sortDirection=asc'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/masters/vote-numbers/page?page=1&pageSize=25&sortBy=amount&sortDirection=desc&departmentPublicId=department-id&active=false&municipalityFinancialYearPublicId=year-id'), expect.objectContaining({ credentials: 'include' }));
  });

  it('uses governed ward and vote-number master routes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getWardMastersPage({ page: 1, pageSize: 100 });
    await getVoteNumberMastersPage({ page: 1, pageSize: 100 });
    await saveVoteNumberMaster('vote-public-id', { departmentPublicId: 'department-public-id', municipalityFinancialYearPublicId: 'municipality-year-public-id', code: 'V01', number: '001', name: 'Operating Vote', amount: 1250, isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', reason: 'Council approved budget', rowVersion: 'AQ==' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/masters/wards/page?page=1&pageSize=100'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/masters/vote-numbers/page?page=1&pageSize=100'), expect.objectContaining({ credentials: 'include' }));
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

  it('transports bounded RFI search, state, sorting, and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      success: true,
      data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getPerformanceRfisPage(1, 'submission / 7', { page: 2, pageSize: 25, search: 'variance', status: 'overdue', sortBy: 'dueAt', sortDirection: 'asc' });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/workflow/submissions/1/submission%20%2F%207/rfis/page?page=2&pageSize=25&search=variance&sortBy=dueAt&sortDirection=asc&status=overdue'),
      expect.objectContaining({ credentials: 'include' }),
    );
  });

  it('transports bounded stage-rating sorting and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      success: true,
      data: { items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0 },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getSubmissionStageRatingsPage(1, 'submission / 7', { page: 2, pageSize: 10, sortBy: 'ratedAt', sortDirection: 'desc' });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/workflow/submissions/1/submission%20%2F%207/ratings/page?page=2&pageSize=10&sortBy=ratedAt&sortDirection=desc'),
      expect.objectContaining({ credentials: 'include' }),
    );
  });

  it('transports independent bounded workflow-governance register queries', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      success: true,
      data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 },
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getWorkflowDefinitionsPage({ page: 2, pageSize: 25, search: 'annual', sortBy: 'version', sortDirection: 'desc', submissionKind: 1, active: true });
    await getReportingWindowsPage({ page: 3, pageSize: 25, search: 'Q1', sortBy: 'opensAt', sortDirection: 'asc', submissionKind: 2, active: false });
    await getRatingSchemesPage({ page: 4, pageSize: 25, search: 'five', sortBy: 'code', sortDirection: 'asc', active: true });
    await getInternalAuditConfigurationsPage({ page: 5, pageSize: 25, search: '2026', sortBy: 'createdAt', sortDirection: 'desc', model: 1, current: true });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/workflow/definitions/page?page=2&pageSize=25&search=annual&sortBy=version&sortDirection=desc&active=true&submissionKind=1'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/workflow/reporting-windows/page?page=3&pageSize=25&search=Q1&sortBy=opensAt&sortDirection=asc&active=false&submissionKind=2'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/workflow/rating-schemes/page?page=4&pageSize=25&search=five&sortBy=code&sortDirection=asc&active=true'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/internal-audit/configurations/page?page=5&pageSize=25&search=2026&sortBy=createdAt&sortDirection=desc&current=true&model=1'), expect.objectContaining({ credentials: 'include' }));
  });

  it('transports bounded reporting-window exception filters and pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getReportingWindowExceptionsPage('window / 7', { page: 2, pageSize: 25, search: 'extension', sortBy: 'approvedAt', sortDirection: 'desc', scope: 'department' });

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/v1/workflow/reporting-windows/window%20%2F%207/exceptions/page?page=2&pageSize=25&search=extension&sortBy=approvedAt&sortDirection=desc&scope=department'),
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
      unitOfMeasureId: 8, kpiUnitOfMeasurePublicId: 'unit-public', kpiUnitOfMeasureName: 'Households', kpiUnitOfMeasureSymbol: '#', weight: 15, kpiType: 'Outcome', indicatorType: 'Quantitative',
      isRevised: false, isWithdrawn: false, targetUnitType: 'absolute_count',
      createdAt: '2026-07-01T00:00:00Z',
    };
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [dto], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    const result = await getOpmsTargetsPage({ page: 1, pageSize: 25 });
    const target = result.data?.items[0];

    expect(target).toMatchObject({
      id: 'target-live', targetName: 'Live KPI', indicatorNumber: 'LIVE-001',
      department: { id: '7', name: 'Live Water Services' },
      assignedTo: { id: 'employee-live', displayName: 'Live Owner' },
      period: { id: '42', name: 'Reporting period 42' },
      strategicGoal: { id: '3', name: 'Not supplied by API' },
      kpiUnitOfMeasurePublicId: 'unit-public', unitOfMeasure: { id: 'unit-public', name: 'Households', symbol: '#' },
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
      .mockResolvedValueOnce(new Response(JSON.stringify({ success: true, data: { items: [attachment], page: 2, pageSize: 25, totalCount: 26, totalPages: 2 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    const submissions = await getOpmsSubmissionsPage();
    const attachments = await getOpmsSubmissionAttachmentsPage('submission-live', { page: 2, pageSize: 25, search: ' evidence ', scanStatus: 'Clean', quarantined: false, active: true });

    expect(submissions.data?.items[0]).toMatchObject({
      target: { id: 'target-live', targetName: 'Live KPI' },
      submitter: { id: 'submitter-live', displayName: 'Live Submitter' },
      verifier: { id: 'verifier-live', displayName: 'Live Verifier' },
      attachments: [], comments: [], history: [],
    });
    expect(attachments.data?.items[0].uploadedBy).toMatchObject({ id: 'uploader-live', displayName: 'Live Uploader' });
    expect(fetchMock).toHaveBeenLastCalledWith(expect.stringContaining('/opms-submissions/submission-live/attachments/page?page=2&pageSize=25&search=evidence&scanStatus=Clean&quarantined=false&active=true'), expect.anything());
  });

  it('encodes bounded register paging and allow-listed sort parameters', async () => {
    const page = { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 };
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: page }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getOpmsTargetsPage({ page: 2, pageSize: 25, search: ' water ', sortBy: 'targetName', sortDirection: 'asc', departmentPublicId: 'department-1', lifecycle: 'revised' });
    await getIpmsTargetsPage({ page: 1, pageSize: 100, sortBy: 'createdAt', sortDirection: 'desc' });
    await getPendingNotificationDeliveries({ page: 3, pageSize: 10, search: ' timeout ', sortBy: 'attemptCount', sortDirection: 'desc' });
    await getNotifications({ page: 1, pageSize: 8, sortBy: 'createdAt', sortDirection: 'desc' });
    await getC88Workspace('year-1');
    await getC88ReportsPage({ page: 2, pageSize: 25, search: ' water ', sortBy: 'indicatorCode', sortDirection: 'asc' }, 'year-1');

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/opms-targets/page?page=2&pageSize=25&search=water&sortBy=targetName&sortDirection=asc&departmentPublicId=department-1&lifecycle=revised'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/ipms-targets/page?page=1&pageSize=100&sortBy=createdAt&sortDirection=desc'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/notification-operations/pending/page?page=3&pageSize=10&search=timeout&sortBy=attemptCount&sortDirection=desc'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/notifications/page?page=1&pageSize=8&sortBy=createdAt&sortDirection=desc'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(5, expect.stringContaining('/v1/c88/workspace?municipalityFinancialYearPublicId=year-1'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(6, expect.stringContaining('/v1/c88/reports/page?page=2&pageSize=25&search=water&sortBy=indicatorCode&sortDirection=asc&municipalityFinancialYearPublicId=year-1'), expect.objectContaining({ credentials: 'include' }));
  });

  it('uses bounded searchable target-option routes for production selectors', async () => {
    const page = { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 };
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: page }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getOpmsTargetOptions({ page: 2, pageSize: 25, search: 'water', sortBy: 'indicatorNumber', sortDirection: 'asc' });
    await getIpmsTargetOptions({ page: 1, pageSize: 25, relatedOpmsTargetPublicId: 'f7253f30-89a2-4fa9-b0af-1ad317222ef6' });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/opms-targets/options?page=2&pageSize=25&search=water&sortBy=indicatorNumber&sortDirection=asc'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/ipms-targets/options?page=1&pageSize=25&relatedOpmsTargetPublicId=f7253f30-89a2-4fa9-b0af-1ad317222ef6'), expect.objectContaining({ credentials: 'include' }));
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
