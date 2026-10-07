import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { Reports } from './Reports';

const api = vi.hoisted(() => ({
  getPerformanceReportSummary: vi.fn(),
  getReportingPeriodMastersPage: vi.fn(),
  getMunicipalityFinancialYearMastersPage: vi.fn(),
  downloadPerformanceReportCsv: vi.fn(),
  getOfficialReportTemplatesPage: vi.fn(),
  getOfficialReportGenerationsPage: vi.fn(),
  getOfficialReportJobsPage: vi.fn(),
  getOfficialReportSchedulesPage: vi.fn(),
  generateOfficialReport: vi.fn(),
  queueOfficialReportJob: vi.fn(),
  retryOfficialReportJob: vi.fn(),
  runOfficialReportSchedule: vi.fn(),
  downloadOfficialReport: vi.fn(),
  saveOfficialReportTemplate: vi.fn(),
  saveOfficialReportSchedule: vi.fn(),
  getDepartmentMastersPage: vi.fn(),
  getUnitMastersPage: vi.fn(),
  getPositionMastersPage: vi.fn(),
  getWardMastersPage: vi.fn(),
  getVoteNumberMastersPage: vi.fn(),
}));
const app = vi.hoisted(() => ({ permissions: ['OPMS_REPORT.READ', 'OPMS_REPORT.EXPORT', 'OPMS_REPORT.GENERATE'] as string[] }));

vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  BarChart: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  CartesianGrid: () => null,
  XAxis: () => null,
  YAxis: () => null,
  Tooltip: () => null,
  Bar: () => null,
}));
vi.mock('../../context/AppContext', () => ({
  useApp: () => ({ permissions: app.permissions, pushToast: vi.fn() }),
}));

describe('Reports', () => {
  beforeEach(() => {
    app.permissions = ['OPMS_REPORT.READ', 'OPMS_REPORT.EXPORT', 'OPMS_REPORT.GENERATE'];
    api.getReportingPeriodMastersPage.mockResolvedValue({
      success: true,
      data: { items: [{ publicId: 'period-1', municipalityFinancialYearPublicId: 'year-1', code: 'Q1', name: 'Quarter 1' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    api.getMunicipalityFinancialYearMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'year-1', code: '2026/27', name: '2026/27', isCurrent: true, isActive: true }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getOfficialReportTemplatesPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'template-1', templateFamilyPublicId: 'family-1', submissionKind: 1, reportType: 1, code: 'QUARTERLY', name: 'Quarterly report', format: 4, versionNumber: 2, headingTemplate: '{FinancialYear} {Period}', columns: [], isCurrent: true, isActive: true, effectiveFrom: '2026-07-01', approvalReference: 'Council-1', reason: 'Approved', createdAt: '2026-07-01', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getOfficialReportGenerationsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'generation-1', generationFamilyPublicId: 'generation-family-1', versionNumber: 1, templatePublicId: 'template-1', templateCode: 'QUARTERLY', templateName: 'Quarterly report', templateVersion: 2, reportType: 1, format: 4, submissionKind: 1, municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27', reportingPeriodPublicId: 'period-1', reportingPeriodCode: 'Q1', scopeJson: '{}', filterJson: '{}', dataVersionReference: 'a'.repeat(64), fileName: 'quarterly.pdf', contentType: 'application/pdf', sizeInBytes: 100, sha256: 'b'.repeat(64), rowCount: 4, generatedBy: 'auditor', generatedAt: '2026-10-01T10:00:00Z', downloadUrl: '/content' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getOfficialReportJobsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getOfficialReportSchedulesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getUnitMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getPositionMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getWardMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getVoteNumberMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getPerformanceReportSummary.mockResolvedValue({
      success: true,
      data: {
        targetCount: 4,
        submissionCount: 3,
        achievedCount: 2,
        atRiskCount: 1,
        pendingCount: 1,
        averageAchievementPercent: 87.5,
        generatedAt: '2026-10-01T10:00:00Z',
        departments: [{ department: 'Finance', submissionCount: 3, achievedCount: 2, averageAchievementPercent: 87.5 }],
      },
    });
  });

  it('renders tenant-scoped server metrics and enables governed CSV export', async () => {
    render(<Reports />);

    await waitFor(() => expect(api.getPerformanceReportSummary).toHaveBeenCalledWith(1, undefined));
    expect(screen.getByText('Configured targets')).toBeInTheDocument();
    expect(screen.getByText('87.5%')).toBeInTheDocument();
    expect(screen.getByText('Finance')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Export CSV/i })).toBeEnabled();
    expect(await screen.findByText('Official generation history')).toBeInTheDocument();
    expect(screen.getByText('Quarterly report')).toBeInTheDocument();
  });

  it('loads the durable report-job ledger through authoritative server paging and search', async () => {
    api.getOfficialReportJobsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    render(<Reports />);

    expect(await screen.findByText('26 jobs')).toBeInTheDocument();
    expect(api.getOfficialReportJobsPage).toHaveBeenCalledWith(1, expect.objectContaining({ page: 1, pageSize: 25 }));
    fireEvent.change(screen.getByLabelText('Search report jobs'), { target: { value: 'failed' } });
    await waitFor(() => expect(api.getOfficialReportJobsPage).toHaveBeenLastCalledWith(1, expect.objectContaining({
      page: 1,
      pageSize: 25,
      search: 'failed',
    })));
  });

  it('loads generation history through scope-safe authoritative server paging and search', async () => {
    api.getOfficialReportGenerationsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 29, totalPages: 2 } });
    render(<Reports />);

    expect(await screen.findByText('29 generations')).toBeInTheDocument();
    expect(api.getOfficialReportGenerationsPage).toHaveBeenCalledWith(1, undefined, expect.objectContaining({ page: 1, pageSize: 25 }));
    fireEvent.change(screen.getByLabelText('Search official generations'), { target: { value: 'annual' } });
    await waitFor(() => expect(api.getOfficialReportGenerationsPage).toHaveBeenLastCalledWith(1, undefined, expect.objectContaining({
      page: 1,
      pageSize: 25,
      search: 'annual',
    })));
  });

  it('masks hostile generation metadata and regenerates from typed selections instead of raw filter JSON', async () => {
    const generation = {
      publicId: 'generation-sensitive', generationFamilyPublicId: 'generation-family', versionNumber: 2,
      templatePublicId: 'template-1', templateCode: 'QUARTERLY', templateName: 'Quarterly report', templateVersion: 2,
      format: 4, submissionKind: 1, reportType: 1, municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27',
      reportingPeriodPublicId: 'period-1', reportingPeriodCode: 'Q1', departmentPublicId: 'typed-department', unitPublicId: 'typed-unit',
      scopeJson: '{"secret":"scope-secret"}', filterJson: '{"departmentPublicId":"hostile-department","secret":"filter-secret"}',
      dataVersionReference: 'DATA-VERSION-SECRET-0123456789', fileName: 'quarterly.pdf', contentType: 'application/pdf',
      sizeInBytes: 100, sha256: 'b'.repeat(64), rowCount: 4, generatedBy: 'generation-actor-secret',
      generatedAt: '2026-10-01T10:00:00Z', downloadUrl: '/content'
    };
    api.getOfficialReportGenerationsPage.mockResolvedValue({ success: true, data: { items: [generation], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.generateOfficialReport.mockResolvedValue({ success: true, data: generation });

    const rendered = render(<Reports />);
    expect(await screen.findByText('Official generation history')).toBeInTheDocument();
    expect(screen.queryByText('generation-actor-secret')).not.toBeInTheDocument();
    expect(screen.queryByText(/DATA-VERSION/)).not.toBeInTheDocument();
    expect(screen.queryByText(/scope-secret|filter-secret|hostile-department/)).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Regenerate now' }));
    await waitFor(() => expect(api.generateOfficialReport).toHaveBeenCalledWith(expect.objectContaining({
      previousGenerationPublicId: 'generation-sensitive',
      departmentPublicId: 'typed-department',
      unitPublicId: 'typed-unit',
    })));
    expect(api.generateOfficialReport).not.toHaveBeenCalledWith(expect.objectContaining({ departmentPublicId: 'hostile-department' }));

    app.permissions = [...app.permissions,
      'OPMS_REPORT.GenerationGeneratedBy.READ', 'OPMS_REPORT.GenerationDataVersionReference.READ'];
    rendered.rerender(<Reports />);
    expect(await screen.findByText('generation-actor-secret')).toBeInTheDocument();
    expect(screen.getByText(/DATA-VERSION/)).toBeInTheDocument();
  });

  it('loads the governed schedule register through authoritative server paging and search', async () => {
    app.permissions = [...app.permissions, 'OPMS_REPORT.CONFIGURE'];
    api.getOfficialReportSchedulesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 28, totalPages: 2 } });
    render(<Reports />);

    expect(await screen.findByText('28 schedules')).toBeInTheDocument();
    expect(api.getOfficialReportSchedulesPage).toHaveBeenCalledWith(1, false, expect.objectContaining({ page: 1, pageSize: 25 }));
    fireEvent.change(screen.getByLabelText('Search report schedules'), { target: { value: 'quarterly' } });
    await waitFor(() => expect(api.getOfficialReportSchedulesPage).toHaveBeenLastCalledWith(1, false, expect.objectContaining({
      page: 1,
      pageSize: 25,
      search: 'quarterly',
    })));
  });

  it('fails closed against hostile report job and schedule metadata until member permissions are present', async () => {
    app.permissions = [...app.permissions, 'OPMS_REPORT.CONFIGURE'];
    api.getOfficialReportJobsPage.mockResolvedValue({
      success: true,
      data: {
        items: [{
          publicId: 'job-sensitive', state: 5, templatePublicId: 'template-1', templateName: 'Quarterly report', reportType: 1,
          municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27', reportingPeriodPublicId: 'period-1', reportingPeriodCode: 'Q1',
          scheduledFor: '2026-10-01T09:00:00Z', availableAt: '2026-10-01T09:00:00Z', attemptCount: 2,
          lastError: 'SENSITIVE-JOB-ERROR', requestedBy: 'requester-secret', requestedAt: '2026-10-01T09:00:00Z',
          fileName: 'quarterly.pdf', distributionOutboxPublicId: 'distribution-secret', recipientUserIds: ['recipient-secret'],
          channels: ['EMAIL'], isMandatoryDistribution: true, retryReason: 'retry-secret', rowVersion: 'AQ=='
        }],
        page: 1, pageSize: 25, totalCount: 1, totalPages: 1,
      },
    });
    api.getOfficialReportSchedulesPage.mockResolvedValue({
      success: true,
      data: {
        items: [{
          publicId: 'schedule-sensitive', scheduleFamilyPublicId: 'schedule-family', versionNumber: 1,
          templatePublicId: 'template-1', templateName: 'Quarterly report', reportType: 1,
          municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27', reportingPeriodPublicId: 'period-1', reportingPeriodCode: 'Q1',
          code: 'SENSITIVE-SCHEDULE', name: 'Governed delivery', cadence: 1, interval: 1, nextRunAt: '2026-10-02T09:00:00Z',
          recipientKind: 1, recipientValues: ['schedule-recipient-secret'], channels: ['EMAIL'], isMandatory: true,
          isCurrent: true, isActive: true, approvalReference: 'Council-1', reason: 'Approved', createdBy: 'schedule-creator-secret',
          createdAt: '2026-10-01T09:00:00Z', rowVersion: 'AQ=='
        }],
        page: 1, pageSize: 25, totalCount: 1, totalPages: 1,
      },
    });

    const rendered = render(<Reports />);
    expect(await screen.findByText('Governed delivery')).toBeInTheDocument();
    expect(screen.queryByText(/requester-secret/)).not.toBeInTheDocument();
    expect(screen.queryByText('SENSITIVE-JOB-ERROR')).not.toBeInTheDocument();
    expect(screen.queryByText(/distribution queued/)).not.toBeInTheDocument();
    expect(screen.queryByText(/recipient-secret/)).not.toBeInTheDocument();
    expect(screen.queryByText(/retry-secret/)).not.toBeInTheDocument();
    expect(screen.queryByText(/schedule-recipient-secret/)).not.toBeInTheDocument();
    expect(screen.queryByText(/schedule-creator-secret/)).not.toBeInTheDocument();

    app.permissions = [...app.permissions,
      'OPMS_REPORT.JobRequestedBy.READ', 'OPMS_REPORT.JobLastError.READ',
      'OPMS_REPORT.JobDistributionOutboxPublicId.READ', 'OPMS_REPORT.JobRecipientUserIds.READ',
      'OPMS_REPORT.JobRetryReason.READ', 'OPMS_REPORT.ScheduleRecipientValues.READ',
      'OPMS_REPORT.ScheduleCreatedBy.READ'];
    rendered.rerender(<Reports />);

    expect(await screen.findByText(/requester-secret/)).toBeInTheDocument();
    expect(screen.getByText('SENSITIVE-JOB-ERROR')).toBeInTheDocument();
    expect(screen.getByText(/distribution queued/)).toBeInTheDocument();
    expect(screen.getByText('Recipients: recipient-secret')).toBeInTheDocument();
    expect(screen.getByText(/retry-secret/)).toBeInTheDocument();
    expect(screen.getByText(/schedule-recipient-secret/)).toBeInTheDocument();
    expect(screen.getByText(/schedule-creator-secret/)).toBeInTheDocument();
  });

  it('keeps generation template choices independent from the bounded administration register', async () => {
    app.permissions = [...app.permissions, 'OPMS_REPORT.CONFIGURE'];
    api.getOfficialReportTemplatesPage.mockImplementation(async (_kind: number, _history: boolean, financialYearPublicId: string | undefined, page: { pageSize?: number }) => ({
      success: true,
      data: {
        items: [{ publicId: 'template-1', templateFamilyPublicId: 'family-1', submissionKind: 1, reportType: 1, code: 'QUARTERLY', name: 'Quarterly report', format: 4, versionNumber: 2, headingTemplate: '{FinancialYear} {Period}', columns: [], isCurrent: true, isActive: true, effectiveFrom: '2026-07-01', approvalReference: 'Council-1', reason: 'Approved', createdAt: '2026-07-01', rowVersion: 'AQ==' }],
        page: 1,
        pageSize: page.pageSize ?? 25,
        totalCount: financialYearPublicId ? 1 : 27,
        totalPages: financialYearPublicId ? 1 : 2,
      },
    }));
    render(<Reports />);

    expect(await screen.findByText('27 templates')).toBeInTheDocument();
    fireEvent.change(await screen.findByLabelText('Financial year', { selector: 'select' }), { target: { value: 'year-1' } });
    await waitFor(() => expect(api.getOfficialReportTemplatesPage).toHaveBeenCalledWith(1, false, 'year-1', expect.objectContaining({
      page: 1,
      pageSize: 100,
      sortBy: 'reportType',
    })));
    expect(api.getOfficialReportTemplatesPage).toHaveBeenCalledWith(1, false, undefined, expect.objectContaining({
      page: 1,
      pageSize: 25,
      sortBy: 'code',
    }));

    fireEvent.change(screen.getByLabelText('Search report templates'), { target: { value: 'annual' } });
    await waitFor(() => expect(api.getOfficialReportTemplatesPage).toHaveBeenCalledWith(1, false, undefined, expect.objectContaining({
      page: 1,
      pageSize: 25,
      search: 'annual',
    })));
  });

  it('loads departmental report scope through the bounded organization master picker', async () => {
    api.getOfficialReportTemplatesPage.mockResolvedValue({
      success: true,
      data: { items: [{ publicId: 'template-department', templateFamilyPublicId: 'family-department', submissionKind: 1, reportType: 4, code: 'DEPARTMENTAL', name: 'Departmental report', format: 4, versionNumber: 1, headingTemplate: '{Department}', columns: [], isCurrent: true, isActive: true, effectiveFrom: '2026-07-01', approvalReference: 'Council-2', reason: 'Approved', createdAt: '2026-07-01', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    api.getDepartmentMastersPage.mockResolvedValue({
      success: true,
      data: { items: [{ publicId: 'department-1', code: 'FIN', name: 'Finance', description: null, managerUserPublicId: null, managerDisplayName: null, effectiveFrom: '2026-07-01', effectiveTo: null, isActive: true, createdAt: '2026-07-01', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });

    render(<Reports />);

    expect(await screen.findByLabelText('Department search')).toBeInTheDocument();
    await waitFor(() => expect(api.getDepartmentMastersPage).toHaveBeenCalledWith(expect.objectContaining({
      page: 1,
      pageSize: 25,
      active: true,
      sortBy: 'name',
    })));
    expect(await screen.findByRole('option', { name: 'FIN · Finance' })).toBeInTheDocument();
  });
});
