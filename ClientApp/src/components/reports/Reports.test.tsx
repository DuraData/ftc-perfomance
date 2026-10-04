import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { Reports } from './Reports';

const api = vi.hoisted(() => ({
  getPerformanceReportSummary: vi.fn(),
  getReportingPeriodMasters: vi.fn(),
  getMunicipalityFinancialYearMasters: vi.fn(),
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
  getDepartments: vi.fn(),
  getUnits: vi.fn(),
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
    api.getReportingPeriodMasters.mockResolvedValue({
      success: true,
      data: [{ publicId: 'period-1', municipalityFinancialYearPublicId: 'year-1', code: 'Q1', name: 'Quarter 1' }],
    });
    api.getMunicipalityFinancialYearMasters.mockResolvedValue({ success: true, data: [{ publicId: 'year-1', code: '2026/27', name: '2026/27', isCurrent: true, isActive: true }] });
    api.getOfficialReportTemplatesPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'template-1', templateFamilyPublicId: 'family-1', submissionKind: 1, reportType: 1, code: 'QUARTERLY', name: 'Quarterly report', format: 4, versionNumber: 2, headingTemplate: '{FinancialYear} {Period}', columns: [], isCurrent: true, isActive: true, effectiveFrom: '2026-07-01', approvalReference: 'Council-1', reason: 'Approved', createdAt: '2026-07-01', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getOfficialReportGenerationsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'generation-1', generationFamilyPublicId: 'generation-family-1', versionNumber: 1, templatePublicId: 'template-1', templateCode: 'QUARTERLY', templateName: 'Quarterly report', templateVersion: 2, reportType: 1, format: 4, submissionKind: 1, municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27', reportingPeriodPublicId: 'period-1', reportingPeriodCode: 'Q1', scopeJson: '{}', filterJson: '{}', dataVersionReference: 'a'.repeat(64), fileName: 'quarterly.pdf', contentType: 'application/pdf', sizeInBytes: 100, sha256: 'b'.repeat(64), rowCount: 4, generatedBy: 'auditor', generatedAt: '2026-10-01T10:00:00Z', downloadUrl: '/content' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getOfficialReportJobsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getOfficialReportSchedulesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getDepartments.mockResolvedValue({ success: true, data: [] });
    api.getUnits.mockResolvedValue({ success: true, data: [] });
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
});
