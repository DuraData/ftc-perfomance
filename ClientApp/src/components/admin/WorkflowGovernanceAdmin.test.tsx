import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { WorkflowGovernanceAdminPage } from './WorkflowGovernanceAdmin';

const api = vi.hoisted(() => ({
  getWorkflowDefinitions: vi.fn(),
  getReportingWindows: vi.fn(),
  getReportingPeriodMastersPage: vi.fn(),
  getMunicipalityFinancialYearMastersPage: vi.fn(),
  getRatingSchemes: vi.fn(),
  createWorkflowDefinition: vi.fn(),
  compareWorkflowDefinitions: vi.fn(),
  retireWorkflowDefinition: vi.fn(),
  createReportingWindow: vi.fn(),
  getReportingWindowExceptions: vi.fn(),
  createReportingWindowException: vi.fn(),
  getUsersPage: vi.fn(),
  getDepartments: vi.fn(),
  getUnits: vi.fn(),
  createRatingScheme: vi.fn(),
  getInternalAuditConfigurations: vi.fn(),
  saveInternalAuditConfiguration: vi.fn(),
  getNotificationPolicies: vi.fn(),
  getWorkingCalendarHolidays: vi.fn(),
  createNotificationPolicy: vi.fn(),
  activateNotificationPolicy: vi.fn(),
  setNotificationPolicyDeliveryState: vi.fn(),
  copyNotificationPolicy: vi.fn(),
  previewNotificationPolicy: vi.fn(),
  runDueNotificationPolicies: vi.fn(),
  addWorkingCalendarHoliday: vi.fn(),
  testNotificationPolicy: vi.fn(),
  getTargetNormalizationPreview: vi.fn(),
  executeTargetNormalization: vi.fn(),
}));

vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ permissions: ['OPMS_KPI.NORMALIZE_LEGACY'], pushToast: vi.fn() }) }));

describe('WorkflowGovernanceAdminPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getWorkflowDefinitions.mockResolvedValue({ success: true, data: [] });
    api.getReportingWindows.mockResolvedValue({ success: true, data: [] });
    api.getReportingPeriodMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'period-1', municipalityFinancialYearPublicId: 'year-1', code: 'Q1', name: 'Quarter 1', periodType: 1, sequence: 1, startDate: '2026-07-01', endDate: '2026-09-30', isActive: true, rowVersion: '' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getMunicipalityFinancialYearMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'year-1', financialYearPublicId: 'fy-1', code: '2026/27', name: '2026/27', isCurrent: true, isActive: true, effectiveFrom: '2026-07-01', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getRatingSchemes.mockResolvedValue({ success: true, data: [{ publicId: 'scheme-1', code: 'FIVE_POINT', name: 'Five point scale', isActive: true, rowVersion: 'AQ==', values: [] }] });
    api.getInternalAuditConfigurations.mockResolvedValue({ success: true, data: [] });
    api.getNotificationPolicies.mockResolvedValue({ success: true, data: [] });
    api.getWorkingCalendarHolidays.mockResolvedValue({ success: true, data: [] });
    api.getTargetNormalizationPreview.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 } });
    api.getReportingWindowExceptions.mockResolvedValue({ success: true, data: [] });
    api.getUsersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 } });
    api.getDepartments.mockResolvedValue({ success: true, data: [] });
    api.getUnits.mockResolvedValue({ success: true, data: [] });
    api.compareWorkflowDefinitions.mockResolvedValue({ success: false, message: 'not configured' });
    api.retireWorkflowDefinition.mockResolvedValue({ success: true, data: null });
  });

  it('opens municipality notification policy and working-calendar administration', async () => {
    render(<WorkflowGovernanceAdminPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Notifications' }));

    await waitFor(() => expect(api.getNotificationPolicies).toHaveBeenCalledOnce());
    expect(screen.getByText('New notification policy draft')).toBeInTheDocument();
    expect(screen.getByText('Working-calendar holiday')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Run due now' })).toBeInTheDocument();
  });

  it('loads authoritative configuration and switches governance tabs', async () => {
    render(<WorkflowGovernanceAdminPage />);

    await waitFor(() => expect(api.getMunicipalityFinancialYearMastersPage).toHaveBeenCalled());
    expect(screen.getByText('New workflow version')).toBeInTheDocument();
    expect(screen.getAllByRole('option', { name: /FIVE_POINT · Five point scale/ })).toHaveLength(2);

    fireEvent.click(screen.getByRole('button', { name: 'Windows' }));
    expect(screen.getByText('Open a reporting window')).toBeInTheDocument();
    expect(await screen.findByRole('option', { name: /Q1 · Quarter 1/ })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Ratings' }));
    expect(screen.getByText('Create rating scheme')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Internal Audit' }));
    expect(screen.getByText('Select Internal Audit model')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Data Cutover' }));
    await waitFor(() => expect(api.getTargetNormalizationPreview).toHaveBeenCalledWith(1, 1, 50));
    expect(screen.getByText('Normalized target cutover')).toBeInTheDocument();
  });

  it('opens scoped reporting-window exception administration', async () => {
    api.getReportingWindows.mockResolvedValue({ success: true, data: [{ publicId: 'window-1', reportingPeriodPublicId: 'period-1', periodCode: 'Q1', submissionKind: 1, opensAt: '2026-07-01T00:00:00Z', closesAt: '2026-07-31T00:00:00Z', isActive: true, rowVersion: 'AQ==' }] });
    api.getDepartments.mockResolvedValue({ success: true, data: [{ id: 7, publicId: 'department-1', code: 'FIN', name: 'Finance' }] });
    render(<WorkflowGovernanceAdminPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Windows' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Manage exceptions' }));

    await waitFor(() => expect(api.getReportingWindowExceptions).toHaveBeenCalledWith('window-1'));
    expect(screen.getByText('Scoped exceptions · Q1 OPMS')).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'FIN · Finance' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Approve exception' })).toBeInTheDocument();
  });

  it('compares versions and renders authoritative stage differences', async () => {
    const stages = [{ publicId: 'stage-submit', code: 'SUBMIT', name: 'Submit', sequence: 1, requiredActionCode: 'OPMS_SUBMISSION.SUBMIT', requiredPermissionCode: 'OPMS_SUBMISSION.SUBMIT', isOptional: false, allowBypass: false, requireDifferentActorFromSubmitter: false, requireDifferentActorFromPreviousStage: false, isTerminal: true, requiresRating: false }];
    const version1 = { publicId: 'workflow-v1', municipalityFinancialYearPublicId: 'year-1', submissionKind: 1, code: 'DEFAULT', name: 'Default', version: 1, isActive: false, effectiveFrom: '2026-07-01T00:00:00Z', effectiveTo: '2026-08-01T00:00:00Z', rowVersion: 'AQ==', stages };
    const version2 = { ...version1, publicId: 'workflow-v2', version: 2, isActive: true, effectiveFrom: '2026-08-01T00:00:00Z', effectiveTo: null, rowVersion: 'Ag==', stages: [{ ...stages[0], publicId: 'stage-submit-v2', name: 'Capture and submit' }] };
    api.getWorkflowDefinitions.mockResolvedValue({ success: true, data: [version2, version1] });
    api.compareWorkflowDefinitions.mockResolvedValue({ success: true, data: { from: version1, to: version2, stageDifferences: [{ change: 'Modified', stageCode: 'SUBMIT', fromSequence: 1, toSequence: 1, changedFields: ['Name'] }] } });
    render(<WorkflowGovernanceAdminPage />);

    const compareButtons = await screen.findAllByRole('button', { name: 'Compare to previous' });
    fireEvent.click(compareButtons[0]);
    await waitFor(() => expect(api.compareWorkflowDefinitions).toHaveBeenCalledWith('workflow-v1', 'workflow-v2'));
    expect(screen.getByText('Changed: Name')).toBeInTheDocument();
  });

  it('retires an active version with its reason and concurrency token', async () => {
    const stages = [{ publicId: 'stage-submit', code: 'SUBMIT', name: 'Submit', sequence: 1, requiredActionCode: 'OPMS_SUBMISSION.SUBMIT', requiredPermissionCode: 'OPMS_SUBMISSION.SUBMIT', isOptional: false, allowBypass: false, requireDifferentActorFromSubmitter: false, requireDifferentActorFromPreviousStage: false, isTerminal: true, requiresRating: false }];
    const version = { publicId: 'workflow-v2', municipalityFinancialYearPublicId: 'year-1', submissionKind: 1, code: 'DEFAULT', name: 'Default', version: 2, isActive: true, effectiveFrom: '2026-08-01T00:00:00Z', effectiveTo: null, rowVersion: 'Ag==', stages };
    api.getWorkflowDefinitions.mockResolvedValue({ success: true, data: [version] });
    render(<WorkflowGovernanceAdminPage />);

    fireEvent.change(await screen.findByLabelText(/Retirement reason for DEFAULT v2/), { target: { value: 'Replaced after annual governance review' } });
    fireEvent.click(screen.getByRole('button', { name: 'Retire version' }));
    await waitFor(() => expect(api.retireWorkflowDefinition).toHaveBeenCalledWith('workflow-v2', expect.objectContaining({ reason: 'Replaced after annual governance review', rowVersion: 'Ag==' })));
  });
});
