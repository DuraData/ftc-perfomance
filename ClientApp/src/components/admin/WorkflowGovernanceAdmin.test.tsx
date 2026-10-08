import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { WorkflowGovernanceAdminPage } from './WorkflowGovernanceAdmin';

const api = vi.hoisted(() => ({
  getWorkflowDefinitionsPage: vi.fn(),
  getReportingWindowsPage: vi.fn(),
  getReportingPeriodMastersPage: vi.fn(),
  getMunicipalityFinancialYearMastersPage: vi.fn(),
  getRatingSchemesPage: vi.fn(),
  createWorkflowDefinition: vi.fn(),
  compareWorkflowDefinitions: vi.fn(),
  retireWorkflowDefinition: vi.fn(),
  createReportingWindow: vi.fn(),
  getReportingWindowExceptionsPage: vi.fn(),
  createReportingWindowException: vi.fn(),
  getUsersPage: vi.fn(),
  getDepartmentMastersPage: vi.fn(),
  getUnitMastersPage: vi.fn(),
  getPositionMastersPage: vi.fn(),
  getWardMastersPage: vi.fn(),
  getVoteNumberMastersPage: vi.fn(),
  createRatingScheme: vi.fn(),
  getInternalAuditConfigurationsPage: vi.fn(),
  saveInternalAuditConfiguration: vi.fn(),
  getNotificationPoliciesPage: vi.fn(),
  getWorkingCalendarHolidaysPage: vi.fn(),
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
const security = vi.hoisted(() => ({ permissions: new Set<string>() }));

vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ permissions: ['OPMS_KPI.NORMALIZE_LEGACY'], pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({
  canRead: (resource: string) => security.permissions.has(`${resource}.READ`),
  canCreate: (resource: string) => security.permissions.has(`${resource}.CREATE`),
  canUpdate: (resource: string) => security.permissions.has(`${resource}.UPDATE`),
  canDelete: (resource: string) => security.permissions.has(`${resource}.DELETE`),
  canExport: (resource: string) => security.permissions.has(`${resource}.EXPORT`),
  canImport: (resource: string) => security.permissions.has(`${resource}.IMPORT`),
  canExecute: (action: string) => security.permissions.has(action),
  canReadField: (resource: string, member: string) => security.permissions.has(`${resource}.${member}.READ`),
  canEditField: (resource: string, member: string) => security.permissions.has(`${resource}.${member}.UPDATE`),
}) }));

describe('WorkflowGovernanceAdminPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.permissions = new Set([
      'NOTIFICATION_POLICY.READ', 'NOTIFICATION_POLICY.CREATE', 'NOTIFICATION_POLICY.ACTIVATE',
      'NOTIFICATION_POLICY.COPY', 'NOTIFICATION_POLICY.SET_DELIVERY_STATE', 'NOTIFICATION_POLICY.PREVIEW',
      'NOTIFICATION_POLICY.TEST', 'NOTIFICATION_POLICY.RUN_DUE',
      'NOTIFICATION_POLICY.RecipientValues.READ', 'NOTIFICATION_POLICY.RecipientValues.UPDATE',
      'NOTIFICATION_POLICY.TitleTemplate.READ', 'NOTIFICATION_POLICY.TitleTemplate.UPDATE',
      'NOTIFICATION_POLICY.MessageTemplate.READ', 'NOTIFICATION_POLICY.MessageTemplate.UPDATE',
      'OPMS_WORKFLOW.WindowExceptionScope.READ', 'OPMS_WORKFLOW.WindowExceptionScope.UPDATE',
      'OPMS_WORKFLOW.WindowExceptionReason.READ', 'OPMS_WORKFLOW.WindowExceptionReason.UPDATE',
      'OPMS_WORKFLOW.WindowExceptionApprovedBy.READ',
      'IPMS_WORKFLOW.WindowExceptionScope.READ', 'IPMS_WORKFLOW.WindowExceptionScope.UPDATE',
      'IPMS_WORKFLOW.WindowExceptionReason.READ', 'IPMS_WORKFLOW.WindowExceptionReason.UPDATE',
      'IPMS_WORKFLOW.WindowExceptionApprovedBy.READ',
    ]);
    api.getWorkflowDefinitionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getReportingWindowsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getReportingPeriodMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'period-1', municipalityFinancialYearPublicId: 'year-1', code: 'Q1', name: 'Quarter 1', periodType: 1, sequence: 1, startDate: '2026-07-01', endDate: '2026-09-30', isActive: true, rowVersion: '' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getMunicipalityFinancialYearMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'year-1', financialYearPublicId: 'fy-1', code: '2026/27', name: '2026/27', isCurrent: true, isActive: true, effectiveFrom: '2026-07-01', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getRatingSchemesPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'scheme-1', code: 'FIVE_POINT', name: 'Five point scale', isActive: true, rowVersion: 'AQ==', values: [] }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getInternalAuditConfigurationsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getNotificationPoliciesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getWorkingCalendarHolidaysPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getTargetNormalizationPreview.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 } });
    api.getReportingWindowExceptionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getUsersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getUnitMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.compareWorkflowDefinitions.mockResolvedValue({ success: false, message: 'not configured' });
    api.retireWorkflowDefinition.mockResolvedValue({ success: true, data: null });
  });

  it('opens municipality notification policy and working-calendar administration', async () => {
    render(<WorkflowGovernanceAdminPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Notifications' }));

    await waitFor(() => expect(api.getNotificationPoliciesPage).toHaveBeenCalledOnce());
    expect(screen.getByText('New notification policy draft')).toBeInTheDocument();
    expect(screen.getByText('Working-calendar holiday')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Run due now' })).toBeInTheDocument();
  });

  it('applies dynamic notification-policy member and action permissions without recompilation', async () => {
    api.getNotificationPoliciesPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'policy-sensitive', familyId: 'family-sensitive', version: 1, code: 'SENSITIVE', name: 'Sensitive policy',
      municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27', scope: 1, source: 1, submissionKind: 1,
      lifecycle: 1, isMandatory: true, deliveryPaused: false, channels: ['IN_APP'], titleTemplate: 'HOSTILE-TITLE',
      messageTemplate: 'HOSTILE-MESSAGE', effectiveFrom: '2026-07-01T00:00:00Z',
      rules: [{ publicId: 'rule-1', code: 'DUE', workingDayOffset: 0, recipientKind: 3, recipientValues: ['HOSTILE-RECIPIENT'], isActive: true }], rowVersion: 'AQ=='
    }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    security.permissions = new Set(['NOTIFICATION_POLICY.READ']);
    const rendered = render(<WorkflowGovernanceAdminPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Notifications' }));
    expect(await screen.findByText('Sensitive policy')).toBeInTheDocument();
    expect(screen.queryByText('HOSTILE-TITLE')).not.toBeInTheDocument();
    expect(screen.queryByText('HOSTILE-MESSAGE')).not.toBeInTheDocument();
    expect(screen.queryByText('HOSTILE-RECIPIENT')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Title template')).toBeDisabled();
    expect(screen.getByLabelText('Message template')).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Create draft' })).toBeDisabled();
    for (const action of ['Run due now', 'Preview', 'Queue test', 'Activate', 'Copy to selected FY'])
      expect(screen.queryByRole('button', { name: action })).not.toBeInTheDocument();

    security.permissions = new Set([
      'NOTIFICATION_POLICY.READ', 'NOTIFICATION_POLICY.CREATE', 'NOTIFICATION_POLICY.ACTIVATE',
      'NOTIFICATION_POLICY.COPY', 'NOTIFICATION_POLICY.PREVIEW', 'NOTIFICATION_POLICY.TEST', 'NOTIFICATION_POLICY.RUN_DUE',
      'NOTIFICATION_POLICY.RecipientValues.UPDATE', 'NOTIFICATION_POLICY.TitleTemplate.READ',
      'NOTIFICATION_POLICY.TitleTemplate.UPDATE', 'NOTIFICATION_POLICY.MessageTemplate.READ',
      'NOTIFICATION_POLICY.MessageTemplate.UPDATE',
    ]);
    rendered.rerender(<WorkflowGovernanceAdminPage />);
    expect(screen.getByLabelText('Title template')).not.toBeDisabled();
    expect(screen.getByLabelText('Message template')).not.toBeDisabled();
    expect(screen.getByRole('button', { name: 'Create draft' })).not.toBeDisabled();
    for (const action of ['Run due now', 'Preview', 'Queue test', 'Activate', 'Copy to selected FY'])
      expect(screen.getByRole('button', { name: action })).toBeInTheDocument();
  });

  it('pages and searches notification policies and working-calendar holidays independently', async () => {
    api.getNotificationPoliciesPage.mockImplementation(async ({ page = 1, search = '' }) => ({ success: true, data: { items: [], page, pageSize: 25, totalCount: search ? 1 : 26, totalPages: search ? 1 : 2 } }));
    api.getWorkingCalendarHolidaysPage.mockImplementation(async ({ page = 1, search = '' }) => ({ success: true, data: { items: [], page, pageSize: 25, totalCount: search ? 1 : 27, totalPages: search ? 1 : 2 } }));
    render(<WorkflowGovernanceAdminPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Notifications' }));
    expect(await screen.findByText('26 policies · Page 1 of 2')).toBeInTheDocument();
    expect(await screen.findByText('27 holidays · Page 1 of 2')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Next policies' }));
    await waitFor(() => expect(api.getNotificationPoliciesPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc' }), undefined, undefined));
    fireEvent.change(screen.getByLabelText('Search notification policies'), { target: { value: 'annual' } });
    await waitFor(() => expect(api.getNotificationPoliciesPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, search: 'annual' }), undefined, undefined));

    fireEvent.click(screen.getByRole('button', { name: 'Next holidays' }));
    await waitFor(() => expect(api.getWorkingCalendarHolidaysPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, pageSize: 25, sortBy: 'date', sortDirection: 'asc' })));
    fireEvent.change(screen.getByLabelText('Search configured holidays'), { target: { value: 'heritage' } });
    await waitFor(() => expect(api.getWorkingCalendarHolidaysPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, search: 'heritage' })));
  });

  it('loads authoritative configuration and switches governance tabs', async () => {
    render(<WorkflowGovernanceAdminPage />);

    await waitFor(() => expect(api.getMunicipalityFinancialYearMastersPage).toHaveBeenCalled());
    expect(screen.getByText('New workflow version')).toBeInTheDocument();
    expect(screen.getAllByRole('option', { name: /FIVE_POINT · Five point scale/ })).toHaveLength(2);

    fireEvent.click(screen.getByRole('button', { name: 'Windows' }));
    expect(screen.getByText('Open a reporting window')).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: /Governance reason/ })).toBeInTheDocument();
    expect(await screen.findByRole('option', { name: /Q1 · Quarter 1/ })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Ratings' }));
    expect(screen.getByText('Create rating scheme')).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: /Governance reason/ })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Internal Audit' }));
    expect(screen.getByText('Select Internal Audit model')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Data Cutover' }));
    await waitFor(() => expect(api.getTargetNormalizationPreview).toHaveBeenCalledWith(1, 1, 50));
    expect(screen.getByText('Normalized target cutover')).toBeInTheDocument();
  });

  it('pages and searches workflow versions through the bounded register contract', async () => {
    api.getWorkflowDefinitionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    render(<WorkflowGovernanceAdminPage />);

    expect(await screen.findByText('Page 1 of 2 · 26 versions')).toBeInTheDocument();
    fireEvent.click(screen.getAllByRole('button', { name: 'Next' }).find(button => !button.hasAttribute('disabled'))!);
    await waitFor(() => expect(api.getWorkflowDefinitionsPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, pageSize: 25, sortBy: 'effectiveFrom', sortDirection: 'desc' })));

    fireEvent.change(screen.getByLabelText('Search workflow versions'), { target: { value: 'annual' } });
    await waitFor(() => expect(api.getWorkflowDefinitionsPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, search: 'annual' })));
  });

  it('opens scoped reporting-window exception administration', async () => {
    api.getReportingWindowsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'window-1', reportingPeriodPublicId: 'period-1', periodCode: 'Q1', submissionKind: 1, opensAt: '2026-07-01T00:00:00Z', closesAt: '2026-07-31T00:00:00Z', isActive: true, rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'department-1', code: 'FIN', name: 'Finance', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    render(<WorkflowGovernanceAdminPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Windows' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Manage exceptions' }));

    await waitFor(() => expect(api.getReportingWindowExceptionsPage).toHaveBeenCalledWith('window-1', { page: 1, pageSize: 25, sortBy: 'approvedAt', sortDirection: 'desc' }));
    expect(screen.getByText('Scoped exceptions · Q1 OPMS')).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'FIN · Finance' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Approve exception' })).toBeInTheDocument();
  });

  it('fails closed against hostile reporting-window exception identities and reasons', async () => {
    security.permissions = new Set();
    api.getReportingWindowsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'window-1', reportingPeriodPublicId: 'period-1', periodCode: 'Q1', submissionKind: 1, opensAt: '2026-07-01T00:00:00Z', closesAt: '2026-07-31T00:00:00Z', isActive: true, rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getReportingWindowExceptionsPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'exception-1', scopeType: 'User', scopePublicId: 'hostile-user-public', scopeName: 'Hostile Scoped User',
      extendedClosesAt: '2026-08-07T00:00:00Z', reason: 'Hostile confidential reason',
      approvedByUserPublicId: 'hostile-approver-public', approvedByName: 'Hostile Approver',
      approvedAt: '2026-07-30T00:00:00Z', rowVersion: 'AQ==',
    }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    render(<WorkflowGovernanceAdminPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Windows' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Manage exceptions' }));

    expect(await screen.findByText('User scope protected')).toBeInTheDocument();
    expect(screen.queryByText('Hostile Scoped User')).not.toBeInTheDocument();
    expect(screen.queryByText('Hostile confidential reason')).not.toBeInTheDocument();
    expect(screen.queryByText(/Hostile Approver|hostile-approver-public/)).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Search scoped exceptions')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Approve exception' })).not.toBeInTheDocument();
    expect(api.getUsersPage).not.toHaveBeenCalled();
  });

  it('pages and searches scoped reporting-window exceptions', async () => {
    api.getReportingWindowsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'window-1', reportingPeriodPublicId: 'period-1', periodCode: 'Q1', submissionKind: 1, opensAt: '2026-07-01T00:00:00Z', closesAt: '2026-07-31T00:00:00Z', isActive: true, rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getReportingWindowExceptionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    render(<WorkflowGovernanceAdminPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Windows' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Manage exceptions' }));
    expect(await screen.findByText('Page 1 of 2 · 26 exceptions')).toBeInTheDocument();

    fireEvent.click(screen.getAllByRole('button', { name: 'Next' }).find(button => !button.hasAttribute('disabled'))!);
    await waitFor(() => expect(api.getReportingWindowExceptionsPage).toHaveBeenLastCalledWith('window-1', expect.objectContaining({ page: 2, pageSize: 25, sortBy: 'approvedAt', sortDirection: 'desc' })));

    fireEvent.change(screen.getByLabelText('Search scoped exceptions'), { target: { value: 'approved extension' } });
    await waitFor(() => expect(api.getReportingWindowExceptionsPage).toHaveBeenLastCalledWith('window-1', expect.objectContaining({ page: 1, search: 'approved extension' })));
  });

  it('searches and pages the full user directory for user-scoped exceptions', async () => {
    api.getReportingWindowsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'window-1', reportingPeriodPublicId: 'period-1', periodCode: 'Q1', submissionKind: 1, opensAt: '2026-07-01T00:00:00Z', closesAt: '2026-07-31T00:00:00Z', isActive: true, rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getUsersPage.mockImplementation(async ({ page = 1, search = '' }) => ({
      success: true,
      data: {
        items: [{ user: { id: `user-${page}`, publicId: `user-public-${page}`, userName: `user-${page}`, firstName: 'Scoped', lastName: `User ${page}`, fullName: `Scoped User ${page}`, email: `user${page}@example.test`, isActive: true, mustChangePassword: false }, roles: [] }],
        page,
        pageSize: 25,
        totalCount: search ? 1 : 26,
        totalPages: search ? 1 : 2,
      },
    }));
    render(<WorkflowGovernanceAdminPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Windows' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Manage exceptions' }));
    await waitFor(() => expect(api.getUsersPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }));

    fireEvent.change(screen.getByLabelText('Scope type'), { target: { value: 'user' } });
    const paging = await screen.findByText('Page 1 of 2 · 26 users');
    expect(screen.getByRole('option', { name: 'Scoped User 1 · user1@example.test' })).toBeInTheDocument();
    fireEvent.click(within(paging.parentElement!).getByRole('button', { name: 'Next' }));
    await waitFor(() => expect(api.getUsersPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }));
    expect(await screen.findByRole('option', { name: 'Scoped User 2 · user2@example.test' })).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search scoped users'), { target: { value: 'specific user' } });
    await waitFor(() => expect(api.getUsersPage).toHaveBeenLastCalledWith({ page: 1, pageSize: 25, search: 'specific user', sortBy: 'name', sortDirection: 'asc' }));
  });

  it('compares versions and renders authoritative stage differences', async () => {
    const stages = [{ publicId: 'stage-submit', code: 'SUBMIT', name: 'Submit', sequence: 1, requiredActionCode: 'OPMS_SUBMISSION.SUBMIT', requiredPermissionCode: 'OPMS_SUBMISSION.SUBMIT', isOptional: false, allowBypass: false, requireDifferentActorFromSubmitter: false, requireDifferentActorFromPreviousStage: false, isTerminal: true, requiresRating: false }];
    const version1 = { publicId: 'workflow-v1', municipalityFinancialYearPublicId: 'year-1', submissionKind: 1, code: 'DEFAULT', name: 'Default', version: 1, isActive: false, effectiveFrom: '2026-07-01T00:00:00Z', effectiveTo: '2026-08-01T00:00:00Z', rowVersion: 'AQ==', stages };
    const version2 = { ...version1, publicId: 'workflow-v2', version: 2, isActive: true, effectiveFrom: '2026-08-01T00:00:00Z', effectiveTo: null, rowVersion: 'Ag==', stages: [{ ...stages[0], publicId: 'stage-submit-v2', name: 'Capture and submit' }] };
    api.getWorkflowDefinitionsPage.mockResolvedValue({ success: true, data: { items: [version2, version1], page: 1, pageSize: 25, totalCount: 2, totalPages: 1 } });
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
    api.getWorkflowDefinitionsPage.mockResolvedValue({ success: true, data: { items: [version], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    render(<WorkflowGovernanceAdminPage />);

    fireEvent.change(await screen.findByLabelText(/Retirement reason for DEFAULT v2/), { target: { value: 'Replaced after annual governance review' } });
    fireEvent.click(screen.getByRole('button', { name: 'Retire version' }));
    await waitFor(() => expect(api.retireWorkflowDefinition).toHaveBeenCalledWith('workflow-v2', expect.objectContaining({ reason: 'Replaced after annual governance review', rowVersion: 'Ag==' })));
  });
});
