import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { NotificationDeliveryOperations } from './NotificationDeliveryOperations';

const api = vi.hoisted(() => ({ getPendingNotificationDeliveries: vi.fn(), retryNotificationDelivery: vi.fn() }));
const security = vi.hoisted(() => ({
  canReadField: vi.fn((resource: string, member: string) => Boolean(resource && member)),
  canExecute: vi.fn((action: string) => Boolean(action)),
}));
vi.mock('../../api/api', () => api);
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));

describe('NotificationDeliveryOperations', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canReadField.mockReturnValue(true);
    security.canExecute.mockReturnValue(true);
  });

  it('shows provider evidence and submits a reasoned retry', async () => {
    const item = { publicId: 'event-1', eventType: 'Notification.Approval', aggregateType: 'OpmsSubmission', aggregateId: 'submission-1', occurredAt: '2026-10-02T08:00:00Z', availableAt: '2026-10-02T08:05:00Z', attemptCount: 10, lastError: 'Provider unavailable', isDeadLetter: true, rowVersion: 'AQ==', deliveries: [{ publicId: 'delivery-1', recipientUserId: 'user-1', channel: 'EMAIL', status: 'Failed', attemptCount: 10, attemptedAt: '2026-10-02T08:00:00Z', provider: 'MailProvider', error: 'Timeout' }] };
    api.getPendingNotificationDeliveries.mockResolvedValueOnce({ success: true, data: { items: [item], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } }).mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } });
    api.retryNotificationDelivery.mockResolvedValue({ success: true, data: item });
    render(<NotificationDeliveryOperations />);

    expect(await screen.findByText('MailProvider · 10 attempt(s)')).toBeInTheDocument();
    expect(api.getPendingNotificationDeliveries).toHaveBeenCalledWith({ page: 1, pageSize: 10, search: '', sortBy: 'createdAt', sortDirection: 'desc' });
    fireEvent.change(screen.getByLabelText('Retry audit reason'), { target: { value: 'Provider incident has been resolved' } });
    fireEvent.click(screen.getByRole('button', { name: 'Retry failed channels' }));

    await waitFor(() => expect(api.retryNotificationDelivery).toHaveBeenCalledWith(item, 'Provider incident has been resolved'));
  });

  it('hides protected delivery identities, references, and failure details when member reads are denied', async () => {
    security.canReadField.mockReturnValue(false);
    security.canExecute.mockReturnValue(false);
    const item = {
      publicId: 'event-2', eventType: 'Notification.Approval', aggregateType: 'OpmsSubmission', aggregateId: null,
      occurredAt: '2026-10-02T08:00:00Z', availableAt: '2026-10-02T08:05:00Z', attemptCount: 10,
      lastError: null, isDeadLetter: true, rowVersion: 'AQ==',
      deliveries: [{ publicId: 'delivery-2', recipientUserId: null, channel: 'EMAIL', status: 'Failed', attemptCount: 10, attemptedAt: '2026-10-02T08:00:00Z', provider: 'MailProvider', providerReference: null, error: null, responseDetail: null }],
    };
    api.getPendingNotificationDeliveries.mockResolvedValue({ success: true, data: { items: [item], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });

    render(<NotificationDeliveryOperations />);

    expect(await screen.findByText('OpmsSubmission · attempt 10')).toBeInTheDocument();
    expect(screen.getByText('EMAIL')).toBeInTheDocument();
    expect(screen.getByText('MailProvider · 10 attempt(s)')).toBeInTheDocument();
    expect(screen.queryByText('submission-1')).not.toBeInTheDocument();
    expect(screen.queryByText('user-1')).not.toBeInTheDocument();
    expect(screen.queryByText('Provider unavailable')).not.toBeInTheDocument();
    expect(screen.queryByText('Timeout')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Retry failed channels' })).not.toBeInTheDocument();
  });
});
