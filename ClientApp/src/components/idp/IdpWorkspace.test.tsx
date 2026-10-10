import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { IdpAlignmentMatrixPage, IdpCommunityParticipationPage, IdpHierarchyPage, IdpPlanManagementPage, IdpPlanningDashboardPage, IdpReportsPage } from './IdpWorkspace';

const app = vi.hoisted(() => ({ pushToast: vi.fn(), setCurrentPath: vi.fn() }));
const security = vi.hoisted(() => ({ canImport: vi.fn(() => true), canReadField: vi.fn(() => false), canEditField: vi.fn(() => false) }));
const api = vi.hoisted(() => ({
  createIdpPlan: vi.fn(),
  createIdpPlanVersion: vi.fn(),
  getIdpPlansPage: vi.fn(),
  getIdpHierarchyPathsPage: vi.fn(),
  getIdpPlanVersionsPage: vi.fn(),
  getIdpDashboard: vi.fn(),
  getIdpAlignmentMatrixPage: vi.fn(),
  getIdpStakeholderEngagementsPage: vi.fn(),
  getIdpImportBatch: vi.fn(),
  getIdpImportBatchesPage: vi.fn(),
  stageIdpKpiImport: vi.fn(),
  stageIdpHierarchyImport: vi.fn(),
  commitIdpImport: vi.fn(),
  commitIdpHierarchyImport: vi.fn(),
  getIdpReport: vi.fn(),
  createIdpComment: vi.fn(),
  createIdpCommunitySession: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../security/AccessControl', () => ({ useHasAnyPermission: () => true }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));
vi.mock('../../api/api', () => api);

const predecessor = {
  publicId: '1c80989a-060c-4b22-94f0-379d54aee8a6',
  municipalityName: 'Blue Hills',
  planTitle: 'Current IDP',
  planCode: 'IDP-2026',
  startFinancialYear: 2026,
  endFinancialYear: 2031,
  status: 'Published',
  currentVersionNumber: 1,
  createdAt: '2026-07-01T00:00:00Z',
  approvedAt: '2026-07-01T00:00:00Z',
  rowVersion: 'AQID',
  planFamilyId: '7f761cbb-c853-48e5-a838-68009b05944d',
  predecessorPlanPublicId: null,
  effectiveFrom: '2026-07-01T00:00:00Z',
  effectiveTo: null,
  publishedAt: '2026-07-01T00:00:00Z',
  publicationReference: 'Council resolution 2026/17',
};

describe('IDP plan lineage workspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canImport.mockReturnValue(true);
    security.canReadField.mockReturnValue(false);
    security.canEditField.mockReturnValue(false);
    api.getIdpPlansPage.mockResolvedValue({
      success: true,
      data: { items: [predecessor], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    api.getIdpPlanVersionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getIdpHierarchyPathsPage.mockResolvedValue({
      success: true,
      data: {
        items: [{
          idpPlanPublicId: predecessor.publicId,
          outcomePublicId: 'outcome-id', outcomeCode: 'SO1', outcomeName: 'Growth',
          objectivePublicId: 'objective-id', objectiveCode: 'OBJ1', objectiveName: 'Reliable services',
          priorityPublicId: 'priority-id', priorityCode: 'PRI1', priorityName: 'Water',
          programmePublicId: 'programme-id', programmeCode: 'PRG1', programmeName: 'Water programme',
          projectPublicId: 'project-id', projectCode: 'PROJ1', projectName: 'Pipeline',
          kpiPublicId: 'kpi-id', kpiCode: 'KPI1', kpiName: 'Households served',
        }],
        page: 1, pageSize: 25, totalCount: 26, totalPages: 2,
      },
    });
    api.getIdpDashboard.mockResolvedValue({ success: true, data: null });
    api.getIdpAlignmentMatrixPage.mockResolvedValue({
      success: true,
      data: {
        items: [{ strategicOutcomeCode: 'SO1', strategicOutcomeName: 'Outcome', objectiveCode: 'OBJ1', objectiveName: 'Objective', frameworkType: 'Circular88', frameworkReferenceCode: 'C88-1', frameworkReferenceTitle: 'Service delivery' }],
        page: 1, pageSize: 25, totalCount: 26, totalPages: 2,
      },
    });
    api.getIdpStakeholderEngagementsPage.mockResolvedValue({
      success: true,
      data: {
        items: [{ publicId: 'stakeholder-id', communitySessionPublicId: 'session-id', sessionDate: '2026-09-01T00:00:00Z', venue: 'Library', stakeholderType: 'Civil Society', stakeholderName: 'Residents Association', contactPerson: null, contactEmail: null, keyInput: 'Water reliability' }],
        page: 1, pageSize: 25, totalCount: 26, totalPages: 2,
      },
    });
    api.getIdpImportBatch.mockResolvedValue({ success: true, data: null });
    api.getIdpImportBatchesPage.mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
    });
    api.createIdpPlan.mockResolvedValue({ success: true, data: predecessor });
    api.createIdpPlanVersion.mockResolvedValue({ success: true, data: {} });
    api.createIdpComment.mockResolvedValue({ success: true, data: true });
    api.createIdpCommunitySession.mockResolvedValue({ success: true, data: true });
    api.getIdpReport.mockResolvedValue({
      success: true,
      data: {
        reportName: 'ANNUAL - IDP-2026', fileName: 'IDP-2026_annual.pdf', contentType: 'application/pdf',
        contentBase64: 'JVBERi0xLjQ=', sizeInBytes: 8, sha256: 'abc123',
      },
    });
  });

  it('routes dashboard creation to the governed plan workspace without synthetic writes', async () => {
    render(<IdpPlanningDashboardPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Manage Plans and Versions' }));

    expect(app.setCurrentPath).toHaveBeenCalledWith('/idp/plans');
    expect(api.createIdpPlan).not.toHaveBeenCalled();
    expect(api.createIdpPlanVersion).not.toHaveBeenCalled();
  });

  it('loads the dashboard through the bounded searchable plan picker', async () => {
    render(<IdpPlanningDashboardPage />);

    await waitFor(() => expect(api.getIdpPlansPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: undefined, sortBy: 'createdAt', sortDirection: 'desc' }));
    await waitFor(() => expect(api.getIdpDashboard).toHaveBeenCalledWith(predecessor.publicId));
    fireEvent.change(screen.getByLabelText('Dashboard plan search'), { target: { value: 'future plan' } });
    await waitFor(() => expect(api.getIdpPlansPage).toHaveBeenLastCalledWith({ page: 1, pageSize: 25, search: 'future plan', sortBy: 'createdAt', sortDirection: 'desc' }), { timeout: 1500 });
  });

  it('downloads generated IDP reports from the governed Base64 document contract', async () => {
    const createObjectUrl = vi.fn(() => 'blob:idp-report');
    const revokeObjectUrl = vi.fn();
    Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: createObjectUrl });
    Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: revokeObjectUrl });
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    render(<IdpReportsPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Annual PDF' }));

    await waitFor(() => expect(api.getIdpReport).toHaveBeenCalledWith(predecessor.publicId, 'annual', 'pdf'));
    expect(createObjectUrl).toHaveBeenCalledWith(expect.any(Blob));
    expect(click).toHaveBeenCalled();
    expect(revokeObjectUrl).toHaveBeenCalledWith('blob:idp-report');
    expect(await screen.findByText('IDP-2026_annual.pdf')).toBeInTheDocument();
    expect(screen.getByText('abc123')).toBeInTheDocument();
    click.mockRestore();
  });

  it('fails closed against hostile annual-performance and budget dashboard payloads', async () => {
    api.getIdpDashboard.mockResolvedValue({
      success: true,
      data: {
        planPublicId: predecessor.publicId, planTitle: predecessor.planTitle, outcomes: 1, objectives: 1, projects: 1, kpis: 1,
        communitySessions: 0, risks: 0, plannedBudget: 9876543, approvedBudget: 8765432,
        actualExpenditure: 7654321, kpiAchievementRate: 91.23, topRiskTitles: [], wardParticipation: [], alignmentCount: 0,
      },
    });

    render(<IdpPlanningDashboardPage />);
    expect(await screen.findByText('No risks linked.')).toBeInTheDocument();
    expect(screen.queryByText('KPI Achievement')).not.toBeInTheDocument();
    expect(screen.queryByText('Budget Utilization')).not.toBeInTheDocument();
    expect(screen.queryByText('Budget Integration')).not.toBeInTheDocument();
    expect(screen.queryByText(/91\.23%/)).not.toBeInTheDocument();
    expect(screen.queryByText(/9,876,543|8,765,432|7,654,321/)).not.toBeInTheDocument();
    expect(security.canReadField).toHaveBeenCalledWith('IDP_INDICATOR', 'AnnualTargetValue');
    expect(security.canReadField).toHaveBeenCalledWith('IDP_PROJECT', 'BudgetSnapshotActual');
  });

  it('submits user-entered predecessor and publication metadata', async () => {
    render(<IdpPlanManagementPage />);
    await screen.findByRole('option', { name: 'IDP-2026 - Current IDP' });
    expect(screen.getByLabelText('Hierarchy/project CSV file')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'KPI definitions' }));
    expect(screen.getByLabelText('KPI CSV file')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Plan title'), { target: { value: 'Successor IDP' } });
    fireEvent.change(screen.getByLabelText('Plan code'), { target: { value: 'IDP-2031' } });
    fireEvent.change(screen.getByLabelText('Start financial year'), { target: { value: '2031' } });
    fireEvent.change(screen.getByLabelText('End financial year'), { target: { value: '2036' } });
    fireEvent.change(screen.getByLabelText('Predecessor plan'), { target: { value: predecessor.publicId } });
    fireEvent.change(screen.getByLabelText('Plan effective from'), { target: { value: '2031-07-01' } });
    fireEvent.change(screen.getByLabelText('Plan publication reference'), { target: { value: 'Council resolution 2031/42' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create Plan' }));

    await waitFor(() => expect(api.createIdpPlan).toHaveBeenCalledWith(expect.objectContaining({
      municipalityName: '',
      planTitle: 'Successor IDP',
      planCode: 'IDP-2031',
      startFinancialYear: 2031,
      endFinancialYear: 2036,
      predecessorPlanPublicId: predecessor.publicId,
      effectiveFrom: '2031-07-01T00:00:00.000Z',
      publicationReference: 'Council resolution 2031/42',
    })));
  });

  it('does not render import controls without the dynamic import capability', async () => {
    security.canImport.mockReturnValue(false);
    render(<IdpPlanManagementPage />);
    await screen.findByRole('option', { name: 'IDP-2026 - Current IDP' });
    expect(screen.queryByLabelText('KPI CSV file')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Hierarchy/project CSV file')).not.toBeInTheDocument();
    expect(api.getIdpImportBatchesPage).not.toHaveBeenCalled();
  });

  it('queries the plan register through bounded server paging and search', async () => {
    api.getIdpPlansPage.mockResolvedValue({
      success: true,
      data: { items: [predecessor], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 },
    });
    render(<IdpPlanManagementPage />);

    await screen.findByText('26 plans');
    expect(api.getIdpPlansPage).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25 }));
    fireEvent.change(screen.getByLabelText('Search IDP plans'), { target: { value: '2031' } });
    await waitFor(() => expect(api.getIdpPlansPage).toHaveBeenLastCalledWith(expect.objectContaining({
      page: 1,
      pageSize: 25,
      search: '2031',
    })));
  });

  it('loads version control through bounded server paging and search', async () => {
    api.getIdpPlanVersionsPage.mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 },
    });
    render(<IdpPlanManagementPage />);

    expect(await screen.findByText('26 versions')).toBeInTheDocument();
    expect(api.getIdpPlanVersionsPage).toHaveBeenCalledWith(predecessor.publicId, expect.objectContaining({
      page: 1, pageSize: 25, sortBy: 'versionNumber', sortDirection: 'desc',
    }));
    fireEvent.change(screen.getByLabelText('Search IDP versions'), { target: { value: 'annual review' } });
    await waitFor(() => expect(api.getIdpPlanVersionsPage).toHaveBeenLastCalledWith(predecessor.publicId, expect.objectContaining({ search: 'annual review' })), { timeout: 1500 });
  });

  it('does not render denied plan-version summary or creator data from a hostile payload', async () => {
    api.getIdpPlanVersionsPage.mockResolvedValue({
      success: true,
      data: {
        items: [{
          publicId: 'version-public-id', idpPlanPublicId: predecessor.publicId, predecessorVersionPublicId: null,
          versionNumber: 2, versionType: 'AnnualReview', versionLabel: 'Annual review', reviewYear: '2026/2027',
          summaryOfChanges: 'PROTECTED-VERSION-SUMMARY', isActive: true, createdAt: '2026-10-01T00:00:00Z',
          createdByUserPublicId: 'PROTECTED-VERSION-ACTOR-ID', createdByName: 'Protected Version Actor',
          effectiveFrom: '2026-10-01T00:00:00Z', effectiveTo: null, publishedAt: null,
          publicationReference: 'Council resolution 42', rowVersion: 'AQID',
        }],
        page: 1, pageSize: 25, totalCount: 1, totalPages: 1,
      },
    });

    render(<IdpPlanManagementPage />);

    expect(await screen.findByText('1 versions')).toBeInTheDocument();
    expect(screen.queryByText('PROTECTED-VERSION-SUMMARY')).not.toBeInTheDocument();
    expect(screen.queryByText(/Protected Version Actor|PROTECTED-VERSION-ACTOR-ID/)).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Summary of changes')).not.toBeInTheDocument();
    expect(screen.getByPlaceholderText('Label, review year, or publication')).toBeInTheDocument();
    expect(security.canReadField).toHaveBeenCalledWith('IDP_PLAN', 'VersionSummary');
    expect(security.canReadField).toHaveBeenCalledWith('IDP_PLAN', 'VersionCreator');
    expect(security.canEditField).toHaveBeenCalledWith('IDP_PLAN', 'VersionSummary');
  });

  it('pages and searches flattened hierarchy paths without loading a full graph', async () => {
    render(<IdpHierarchyPage />);

    expect(await screen.findByText('26 KPI paths')).toBeInTheDocument();
    await waitFor(() => expect(api.getIdpHierarchyPathsPage).toHaveBeenCalledWith(predecessor.publicId, expect.objectContaining({
      page: 1, pageSize: 25, sortBy: 'outcome', sortDirection: 'asc',
    })));
    expect(screen.getByText('KPI1 - Households served')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Search IDP hierarchy paths'), { target: { value: 'pipeline' } });
    await waitFor(() => expect(api.getIdpHierarchyPathsPage).toHaveBeenLastCalledWith(predecessor.publicId, expect.objectContaining({ search: 'pipeline' })), { timeout: 1500 });
    fireEvent.click(screen.getByRole('button', { name: 'Next paths' }));
    await waitFor(() => expect(api.getIdpHierarchyPathsPage).toHaveBeenLastCalledWith(predecessor.publicId, expect.objectContaining({ page: 2, search: 'pipeline' })));
    expect(api.getIdpPlanVersionsPage).toHaveBeenCalledWith(predecessor.publicId, expect.objectContaining({ pageSize: 1 }), true);
  });

  it('does not render denied hierarchy governance fields from a hostile payload', async () => {
    api.getIdpHierarchyPathsPage.mockResolvedValue({
      success: true,
      data: {
        items: [{
          idpPlanPublicId: predecessor.publicId,
          outcomePublicId: 'outcome-id', outcomeCode: 'SO1', outcomeName: 'Growth',
          objectivePublicId: 'objective-id', objectiveCode: 'OBJ1', objectiveName: 'Reliable services',
          objectiveStrategicOwnerPublicId: 'PROTECTED-OWNER-ID', objectiveStrategicOwnerName: 'PROTECTED OWNER', objectiveBudgetAllocation: 987654321,
          priorityPublicId: 'priority-id', priorityCode: 'PRI1', priorityName: 'Water',
          programmePublicId: 'programme-id', programmeCode: 'PRG1', programmeName: 'Water programme',
          programmePlannedBudget: 876543210, programmeApprovedBudget: 765432109, programmeActualExpenditure: 654321098,
          projectPublicId: 'project-id', projectCode: 'PROJ1', projectName: 'Pipeline', projectBudget: 543210987, projectFundingSource: 'PROTECTED FUNDING SOURCE',
          kpiPublicId: 'kpi-id', kpiCode: 'KPI1', kpiName: 'Households served',
        }],
        page: 1, pageSize: 25, totalCount: 1, totalPages: 1,
      },
    });

    render(<IdpHierarchyPage />);
    expect(await screen.findByText('KPI1 - Households served')).toBeInTheDocument();
    expect(screen.queryByText('Governed Details')).not.toBeInTheDocument();
    expect(screen.queryByText(/PROTECTED OWNER|PROTECTED FUNDING SOURCE/)).not.toBeInTheDocument();
    expect(screen.queryByText(/987,654,321|876,543,210|765,432,109|654,321,098|543,210,987/)).not.toBeInTheDocument();
    expect(security.canReadField).toHaveBeenCalledWith('IDP_PLAN', 'ObjectiveStrategicOwner');
    expect(security.canReadField).toHaveBeenCalledWith('IDP_PROJECT', 'ProjectFundingSource');
  });

  it('pages and filters IDP import summaries without loading reconciliation rows', async () => {
    security.canReadField.mockReturnValue(true);
    api.getIdpImportBatchesPage.mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 25, totalCount: 31, totalPages: 2 },
    });
    render(<IdpPlanManagementPage />);

    await screen.findByText('Import history · 31');
    expect(api.getIdpImportBatchesPage).toHaveBeenCalledWith(predecessor.publicId, expect.objectContaining({
      page: 1,
      pageSize: 25,
      sortBy: 'createdAt',
      sortDirection: 'desc',
    }));

    fireEvent.change(screen.getByLabelText('Search IDP import history'), { target: { value: 'council' } });
    fireEvent.change(screen.getByLabelText('Filter IDP import status'), { target: { value: 'Committed' } });
    fireEvent.change(screen.getByLabelText('Filter IDP import type'), { target: { value: 'KPI' } });
    await waitFor(() => expect(api.getIdpImportBatchesPage).toHaveBeenLastCalledWith(predecessor.publicId, expect.objectContaining({
      page: 1,
      pageSize: 25,
      search: 'council',
      status: 'Committed',
      importType: 'KPI',
    })));
  });

  it('fails closed against hostile IDP import metadata and diagnostic payloads', async () => {
    const summary = {
      publicId: 'batch-public-id', clientRequestId: 'SECRET-REQUEST', idpPlanPublicId: predecessor.publicId,
      importType: 'KPI', sourceFileName: 'SECRET-IMPORT.csv', sourceSha256: 'SECRET-HASH', status: 'Staged',
      totalRows: 1, newRows: 0, unchangedRows: 0, changedRows: 0, invalidRows: 1,
      createdByUserPublicId: 'SECRET-ACTOR-ID', createdByName: 'Secret Actor', createdAt: '2026-10-01T00:00:00Z',
      committedByUserPublicId: null, committedByName: null, committedAt: null, rowVersion: 'AQ==',
    };
    api.getIdpImportBatchesPage.mockResolvedValue({
      success: true, data: { items: [summary], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    api.getIdpImportBatch.mockResolvedValue({
      success: true,
      data: {
        ...summary,
        rows: [{ publicId: 'row-id', sourceRowNumber: 2, reference: 'ROW-1', status: 'Invalid', existingValueJson: 'SECRET-BEFORE', normalizedJson: 'SECRET-AFTER', suppliedValue: 'SECRET-SUPPLIED', errorCode: 'SECRET-CODE', errorField: 'SECRET-FIELD', errorMessage: 'SECRET-ERROR' }],
      },
    });

    render(<IdpPlanManagementPage />);
    const batchButton = await screen.findByRole('button', { name: /KPI · Staged · 1 rows/ });
    expect(screen.queryByText(/SECRET-IMPORT/)).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Search IDP import history')).not.toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'File name' })).not.toBeInTheDocument();
    fireEvent.click(batchButton);

    await waitFor(() => expect(api.getIdpImportBatch).toHaveBeenCalledWith('batch-public-id'));
    expect(await screen.findByText('ROW-1')).toBeInTheDocument();
    for (const secret of ['SECRET-REQUEST', 'SECRET-IMPORT', 'SECRET-HASH', 'Secret Actor', 'SECRET-ACTOR-ID', 'SECRET-BEFORE', 'SECRET-AFTER', 'SECRET-SUPPLIED', 'SECRET-CODE', 'SECRET-FIELD', 'SECRET-ERROR'])
      expect(screen.queryByText(new RegExp(secret))).not.toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: 'Error' })).not.toBeInTheDocument();
  });

  it('pages, searches and filters the alignment matrix by plan public ID', async () => {
    render(<IdpAlignmentMatrixPage />);

    expect(await screen.findByText('26 alignment links')).toBeInTheDocument();
    await waitFor(() => expect(api.getIdpAlignmentMatrixPage).toHaveBeenCalledWith(
      predecessor.publicId,
      expect.objectContaining({ page: 1, pageSize: 25, sortBy: 'objective', sortDirection: 'asc' }),
      undefined,
    ));
    fireEvent.change(screen.getByLabelText('Search IDP alignment matrix'), { target: { value: 'service' } });
    fireEvent.change(screen.getByLabelText('Filter IDP alignment framework'), { target: { value: 'Circular88' } });
    await waitFor(() => expect(api.getIdpAlignmentMatrixPage).toHaveBeenCalledWith(
      predecessor.publicId,
      expect.objectContaining({ page: 1, search: 'service' }),
      'Circular88',
    ));
    fireEvent.click(screen.getByRole('button', { name: 'Next alignments' }));
    await waitFor(() => expect(api.getIdpAlignmentMatrixPage).toHaveBeenCalledWith(
      predecessor.publicId,
      expect.objectContaining({ page: 2, search: 'service' }),
      'Circular88',
    ));
  });

  it('loads a bounded stakeholder register and hides denied contact fields and sort options', async () => {
    render(<IdpCommunityParticipationPage />);

    expect(await screen.findByText('26 stakeholder engagements')).toBeInTheDocument();
    expect(api.getIdpStakeholderEngagementsPage).toHaveBeenCalledWith(predecessor.publicId, expect.objectContaining({
      page: 1, pageSize: 25, sortBy: 'sessionDate', sortDirection: 'desc',
    }));
    expect(screen.getByText('Residents Association')).toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: 'Contact person' })).not.toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: 'Contact email' })).not.toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Contact person' })).not.toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Contact email' })).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search stakeholder engagements'), { target: { value: 'residents' } });
    await waitFor(() => expect(api.getIdpStakeholderEngagementsPage).toHaveBeenLastCalledWith(predecessor.publicId, expect.objectContaining({ search: 'residents' })), { timeout: 1500 });
    fireEvent.click(screen.getByRole('button', { name: 'Next stakeholders' }));
    await waitFor(() => expect(api.getIdpStakeholderEngagementsPage).toHaveBeenLastCalledWith(predecessor.publicId, expect.objectContaining({ page: 2, search: 'residents' })));
  });

  it('submits hierarchy collaboration with plan and version public IDs only', async () => {
    api.getIdpPlanVersionsPage.mockResolvedValue({
      success: true,
      data: {
        items: [{
          publicId: 'version-public-id', idpPlanPublicId: predecessor.publicId, predecessorVersionPublicId: null,
          versionNumber: 2, versionType: 'AnnualReview', versionLabel: 'Annual review', reviewYear: '2026/2027',
          summaryOfChanges: null, isActive: true, createdAt: '2026-10-01T00:00:00Z',
          effectiveFrom: '2026-10-01T00:00:00Z', effectiveTo: null, publishedAt: null,
          publicationReference: null, rowVersion: 'AQID',
        }],
        page: 1, pageSize: 1, totalCount: 1, totalPages: 1,
      },
    });

    render(<IdpHierarchyPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Add Review Comment' }));

    await waitFor(() => expect(api.createIdpComment).toHaveBeenCalledWith({
      idpPlanPublicId: predecessor.publicId,
      idpPlanVersionPublicId: 'version-public-id',
      entityName: 'IdpHierarchy',
      entityId: predecessor.publicId,
      comment: 'Hierarchy review checkpoint captured from planning workspace',
    }));
  });

  it('submits community participation with the selected plan public ID only', async () => {
    render(<IdpCommunityParticipationPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Log Public Meeting' }));

    await waitFor(() => expect(api.createIdpCommunitySession).toHaveBeenCalledWith(expect.objectContaining({
      idpPlanPublicId: predecessor.publicId,
      participationType: 'PublicMeeting',
    })));
    expect(api.createIdpCommunitySession.mock.calls[0][0]).not.toHaveProperty('idpPlanId');
  });
});
