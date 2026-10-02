import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TopBar } from './TopBar';

const app = vi.hoisted(() => ({
  userProfile: { firstName: 'Test', lastName: 'User', fullName: 'Test User', email: 'test@example.gov.za' },
  darkMode: false,
  toggleDarkMode: vi.fn(),
  logout: vi.fn(),
  setCurrentPath: vi.fn(),
  pushToast: vi.fn(),
  tenantContexts: [],
  currentMunicipalityId: null,
  switchMunicipality: vi.fn(),
}));
const api = vi.hoisted(() => ({ getNotifications: vi.fn(), markNotificationRead: vi.fn() }));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../api/api', () => api);

describe('TopBar notification feed', () => {
  it('loads only the recent server page while displaying the authoritative unread count', async () => {
    api.getNotifications.mockResolvedValue({
      success: true,
      data: {
        items: [
          { id: 'n-1', userId: 'user-1', type: 'Submission', title: 'First', message: 'First message', isRead: false, createdAt: '2026-10-02T10:00:00Z' },
          { id: 'n-2', userId: 'user-1', type: 'Approval', title: 'Second', message: 'Second message', isRead: true, createdAt: '2026-10-02T09:00:00Z' },
        ],
        page: 1,
        pageSize: 8,
        totalCount: 42,
        totalPages: 6,
        unreadCount: 12,
      },
    });
    api.markNotificationRead.mockResolvedValue({ success: true, data: true });

    render(<TopBar title="Dashboard" />);

    const notificationsButton = await screen.findByRole('button', { name: 'Notifications, 12 unread' });
    expect(api.getNotifications).toHaveBeenCalledWith({ page: 1, pageSize: 8, sortBy: 'createdAt', sortDirection: 'desc' });
    fireEvent.click(notificationsButton);
    expect(screen.getByText('12 unread')).toBeInTheDocument();
    expect(screen.getByText('First')).toBeInTheDocument();
    expect(screen.getByText('Second')).toBeInTheDocument();

    fireEvent.click(screen.getByText('First'));
    await waitFor(() => expect(api.markNotificationRead).toHaveBeenCalledWith('n-1'));
    expect(await screen.findByRole('button', { name: 'Notifications, 11 unread' })).toBeInTheDocument();
  });
});
