import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { SystemOperationsPage } from './SystemOperationsPage';

const api = vi.hoisted(() => ({ getSystemOperationsOverview: vi.fn() }));

vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children, title }: { children: React.ReactNode; title: string }) => <main><h1>{title}</h1>{children}</main> }));

const overview = {
  generatedAt: '2026-10-18T10:00:00Z',
  startedAt: '2026-10-18T08:00:00Z',
  uptimeSeconds: 7_200,
  status: 'Degraded',
  totalHealthCheckDurationMilliseconds: 11,
  checks: [
    { name: 'database', status: 'Healthy', durationMilliseconds: 3 },
    { name: 'outbox', status: 'Degraded', durationMilliseconds: 7 },
  ],
  requests: { started: 51, completed: 50, failed: 2, active: 1, failureRatePercent: 4, averageDurationMilliseconds: 25.5 },
  alertThresholds: { minimumRequests: 20, failureRatePercent: 10, averageDurationMilliseconds: 2_000 },
} as const;

describe('SystemOperationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getSystemOperationsOverview.mockResolvedValue({ success: true, data: overview });
  });

  it('renders sanitised readiness, request telemetry, and alert thresholds', async () => {
    render(<SystemOperationsPage />);

    expect(screen.getByRole('status')).toHaveTextContent('Loading operational readiness');
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Readiness checks' })).toBeInTheDocument());
    expect(screen.getByText('database')).toBeInTheDocument();
    expect(screen.getByText('outbox')).toBeInTheDocument();
    expect(screen.getByText('50')).toBeInTheDocument();
    expect(screen.getByText('25.50 ms')).toBeInTheDocument();
    expect(screen.getByText('20 requests')).toBeInTheDocument();
    expect(screen.getByText('Sensitive provider details and connection information are intentionally excluded.')).toBeInTheDocument();
  });

  it('refreshes on demand and renders a safe API error state', async () => {
    render(<SystemOperationsPage />);
    await waitFor(() => expect(api.getSystemOperationsOverview).toHaveBeenCalledTimes(1));
    api.getSystemOperationsOverview.mockResolvedValueOnce({ success: false, message: 'Readiness service unavailable.' });

    fireEvent.click(screen.getByRole('button', { name: 'Refresh status' }));

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Readiness service unavailable.'));
    expect(api.getSystemOperationsOverview).toHaveBeenCalledTimes(2);
  });
});
