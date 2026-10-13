import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { IPMSSubmissionsList, OPMSSubmissionsList } from './OPMSSubmissions';

const pushToast = vi.hoisted(() => vi.fn());
const app = vi.hoisted(() => ({ permissions: [] as string[] }));
const api = vi.hoisted(() => ({
  downloadPerformanceReportCsv: vi.fn(),
  getIpmsSubmissionsPage: vi.fn(),
  getOpmsSubmissionsPage: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({
  useApp: () => ({ permissions: app.permissions, pushToast }),
}));
vi.mock('../../api/api', async importOriginal => ({
  ...(await importOriginal<typeof import('../../api/api')>()),
  ...api,
}));
vi.mock('../layout/AppShell', () => ({
  AppShell: ({ children }: { children: React.ReactNode }) => <main>{children}</main>,
}));

const emptyPage = { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 };
const exportPermissions = (kind: 'OPMS' | 'IPMS') => [
  `${kind}_REPORT.EXPORT`,
  `${kind}_KPI.READ`,
  `${kind}_SUBMISSION.READ`,
  `${kind}_SUBMISSION.ActualPerformance.READ`,
  `${kind}_SUBMISSION.Variance.READ`,
];

describe('performance submission register exports', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    app.permissions = [];
    window.history.replaceState({}, '', '/');
    api.getOpmsSubmissionsPage.mockResolvedValue({ success: true, data: emptyPage });
    api.getIpmsSubmissionsPage.mockResolvedValue({ success: true, data: emptyPage });
    api.downloadPerformanceReportCsv.mockResolvedValue({ success: true, data: true });
  });

  it('exports the authorized OPMS reporting-period population and confirms the download', async () => {
    app.permissions = exportPermissions('OPMS');
    window.history.replaceState({}, '', '/opms/submissions?reportingPeriodPublicId=period-q1');

    render(<OPMSSubmissionsList />);

    fireEvent.click(await screen.findByRole('button', { name: 'Export' }));
    await waitFor(() => expect(api.downloadPerformanceReportCsv).toHaveBeenCalledWith(1, 'period-q1'));
    expect(pushToast).toHaveBeenCalledWith('success', 'OPMS performance CSV downloaded');
  });

  it('reports a failed IPMS export without claiming that a file downloaded', async () => {
    app.permissions = exportPermissions('IPMS');
    api.downloadPerformanceReportCsv.mockResolvedValue({ success: false, message: 'Export denied by current scope' });

    render(<IPMSSubmissionsList />);

    fireEvent.click(await screen.findByRole('button', { name: 'Export' }));
    await waitFor(() => expect(api.downloadPerformanceReportCsv).toHaveBeenCalledWith(2, undefined));
    expect(pushToast).toHaveBeenCalledWith('error', 'Export denied by current scope');
    expect(pushToast).not.toHaveBeenCalledWith('success', expect.any(String));
  });

  it.each([
    ['OPMS', OPMSSubmissionsList],
    ['IPMS', IPMSSubmissionsList],
  ] as const)('does not expose the %s export control when a protected source member is denied', async (kind, Component) => {
    app.permissions = exportPermissions(kind).filter(permission => !permission.toUpperCase().endsWith('.VARIANCE.READ'));

    render(<Component />);

    await waitFor(() => expect(kind === 'OPMS' ? api.getOpmsSubmissionsPage : api.getIpmsSubmissionsPage).toHaveBeenCalled());
    expect(screen.queryByRole('button', { name: 'Export' })).not.toBeInTheDocument();
    expect(api.downloadPerformanceReportCsv).not.toHaveBeenCalled();
  });

  it.each([
    ['OPMS', OPMSSubmissionsList],
    ['IPMS', IPMSSubmissionsList],
  ] as const)('does not expose the %s create control without the exact submission CREATE permission', async (kind, Component) => {
    app.permissions = [`${kind}_SUBMISSION.READ`];

    render(<Component />);

    await waitFor(() => expect(kind === 'OPMS' ? api.getOpmsSubmissionsPage : api.getIpmsSubmissionsPage).toHaveBeenCalled());
    expect(screen.queryByRole('button', { name: 'New Submission' })).not.toBeInTheDocument();
  });
});
