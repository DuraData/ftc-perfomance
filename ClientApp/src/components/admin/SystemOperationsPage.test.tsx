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
  operationalAlerts: {
    historyAvailable: true,
    configuration: { monitorEnabled: true, pollSeconds: 60, recipientsConfigured: true, channels: ['IN_APP', 'EMAIL'] },
    items: [{
      publicId: '11111111-1111-1111-1111-111111111111',
      incidentPublicId: '22222222-2222-2222-2222-222222222222',
      lifecycle: 'Raised', healthStatus: 'Degraded', checkNames: ['outbox'], occurredAt: '2026-10-18T09:55:00Z',
      totalDeliveries: 2, deliveredDeliveries: 1, failedDeliveries: 1, terminalDeliveries: 0,
    }],
  },
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
    expect(screen.getAllByText('outbox')).toHaveLength(2);
    expect(screen.getByText('50')).toBeInTheDocument();
    expect(screen.getByText('25.50 ms')).toBeInTheDocument();
    expect(screen.getByText('20 requests')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Alert handoff' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Recent operational alerts' })).toBeInTheDocument();
    expect(screen.getByText('IN_APP, EMAIL')).toBeInTheDocument();
    expect(screen.getByText('1/2 delivered · 1 retrying')).toBeInTheDocument();
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

  it('keeps live readiness visible when durable alert history is unavailable', async () => {
    api.getSystemOperationsOverview.mockResolvedValue({
      success: true,
      data: { ...overview, operationalAlerts: { ...overview.operationalAlerts, historyAvailable: false, items: [] } },
    });

    render(<SystemOperationsPage />);

    await waitFor(() => expect(screen.getByText('History unavailable')).toBeInTheDocument());
    expect(screen.getByRole('status')).toHaveTextContent('Alert history is temporarily unavailable');
    expect(screen.getByRole('heading', { name: 'Readiness checks' })).toBeInTheDocument();
  });
});
