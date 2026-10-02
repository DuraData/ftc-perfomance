import { render, screen, waitFor } from '@testing-library/react';
import { Reports } from './Reports';

const api = vi.hoisted(() => ({
  getPerformanceReportSummary: vi.fn(),
  getReportingPeriodMasters: vi.fn(),
  downloadPerformanceReportCsv: vi.fn(),
}));

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
  useApp: () => ({ permissions: ['OPMS_REPORT.READ', 'OPMS_REPORT.EXPORT'], pushToast: vi.fn() }),
}));

describe('Reports', () => {
  beforeEach(() => {
    api.getReportingPeriodMasters.mockResolvedValue({
      success: true,
      data: [{ publicId: 'period-1', code: 'Q1', name: 'Quarter 1' }],
    });
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
  });
});
