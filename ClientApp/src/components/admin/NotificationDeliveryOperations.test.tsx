import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { NotificationDeliveryOperations } from './NotificationDeliveryOperations';

const api = vi.hoisted(() => ({ getPendingNotificationDeliveries: vi.fn(), retryNotificationDelivery: vi.fn() }));
vi.mock('../../api/api', () => api);

describe('NotificationDeliveryOperations', () => {
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
});
