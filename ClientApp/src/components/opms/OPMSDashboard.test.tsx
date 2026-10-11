import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { PerformanceDashboardDto } from '../../types';
import { OPMSDashboardPage } from './OPMSDashboard';

const api = vi.hoisted(() => ({ getOpmsPerformanceDashboard: vi.fn() }));
const setCurrentPath = vi.hoisted(() => vi.fn());

vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ setCurrentPath }) }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../common/CalendarMasterPicker', () => ({
  CalendarMasterPicker: ({ label, value, onChange, disabled }: { label: string; value: string; onChange: (value: string) => void; disabled?: boolean }) => (
    <label>{label}<select aria-label={label} value={value} disabled={disabled} onChange={event => onChange(event.target.value)}>
      <option value="" />
      {label === 'Financial year' ? <option value="year-1">2026/27</option> : <><option value="period-1">Q1</option><option value="period-2">Q2</option></>}
    </select></label>
  ),
}));

const dashboard: PerformanceDashboardDto = {
  totalTargets: 4,
  activeTargets: 4,
  completedTargets: 1,
  overdueTargets: 1,
  atRiskTargets: 1,
  outstandingTargets: 1,
  draftSubmissions: 1,
  submittedSubmissions: 1,
  returnedSubmissions: 1,
  approvedSubmissions: 1,
  pendingVerification: 1,
  pendingApproval: 2,
  municipalityFinancialYearPublicId: 'year-1',
  financialYearCode: '2026/27',
  financialYearName: '2026/2027 Financial Year',
  reportingPeriodPublicId: 'period-1',
  reportingPeriodCode: 'Q1',
  reportingPeriodName: 'Quarter 1',
  reportingWindowState: 'Open',
  reportingWindowOpensAt: '2026-10-01T00:00:00Z',
  reportingWindowClosesAt: '2026-10-31T00:00:00Z',
  ratingBreakdown: [{ label: 'Achieved', count: 1 }],
  teamBreakdown: [{ departmentPublicId: 'department-1', departmentName: 'Roads', targetCount: 4, achievedCount: 1, atRiskCount: 1 }],
  periodBreakdown: [
    { reportingPeriodPublicId: 'period-1', code: 'Q1', name: 'Quarter 1', sequence: 1, windowState: 'Open', opensAt: '2026-10-01T00:00:00Z', closesAt: '2026-10-31T00:00:00Z', submissionCount: 4, achievedCount: 1, atRiskCount: 1, outstandingCount: 1 },
    { reportingPeriodPublicId: 'period-2', code: 'Q2', name: 'Quarter 2', sequence: 2, windowState: 'Upcoming', opensAt: '2027-01-01T00:00:00Z', closesAt: '2027-01-31T00:00:00Z', submissionCount: 0, achievedCount: 0, atRiskCount: 0, outstandingCount: 4 },
  ],
};

describe('OPMS dashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getOpmsPerformanceDashboard.mockResolvedValue({ success: true, data: dashboard });
  });

  it('renders governed period, performance, workflow, rating and department aggregates', async () => {
    render(<OPMSDashboardPage />);

    expect(await screen.findByRole('heading', { name: 'Performance results' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Workflow progress' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Achieved KPIs: 1 of 4' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Outstanding KPIs: 1 of 4' })).toBeInTheDocument();
    expect(screen.getByText('Achieved')).toBeInTheDocument();
    expect(screen.getByText('Roads')).toBeInTheDocument();
    expect(screen.getByText('1 of 4 achieved')).toBeInTheDocument();
    expect(screen.getByText('Overdue submissions')).toBeInTheDocument();
    expect(screen.queryByText(/performance summary for/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/visible in the performance grid/i)).not.toBeInTheDocument();

    await waitFor(() => expect(api.getOpmsPerformanceDashboard).toHaveBeenCalledWith({
      municipalityFinancialYearPublicId: 'year-1',
      reportingPeriodPublicId: 'period-1',
    }));
    await waitFor(() => {
      const button = screen.getByRole('button', { name: 'Achieved KPIs: 1 of 4' });
      expect(button).toBeEnabled();
      fireEvent.click(button);
      expect(setCurrentPath).toHaveBeenCalledWith('/opms/targets?municipalityFinancialYearPublicId=year-1&reportingPeriodPublicId=period-1&dashboardFilter=achieved');
    });
  });

  it('reloads the exact authorised aggregate population when a period is selected', async () => {
    render(<OPMSDashboardPage />);
    await screen.findByRole('heading', { name: 'Performance results' });

    fireEvent.click(screen.getByRole('button', { name: /Q2 · Quarter 2/ }));

    await waitFor(() => expect(api.getOpmsPerformanceDashboard).toHaveBeenCalledWith({
      municipalityFinancialYearPublicId: 'year-1',
      reportingPeriodPublicId: 'period-2',
    }));
  });

  it('shows a recoverable error instead of silently presenting zero metrics', async () => {
    api.getOpmsPerformanceDashboard.mockResolvedValueOnce({ success: false, message: 'Dashboard scope is unavailable.' });
    render(<OPMSDashboardPage />);

    expect(await screen.findByRole('heading', { name: 'OPMS dashboard unavailable' })).toBeInTheDocument();
    expect(screen.getByText('Dashboard scope is unavailable.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() => expect(api.getOpmsPerformanceDashboard).toHaveBeenCalledTimes(2));
  });
});
