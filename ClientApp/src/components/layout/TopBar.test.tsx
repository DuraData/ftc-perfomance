import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TopBar } from './TopBar';

const app = vi.hoisted(() => ({
  userProfile: { firstName: 'Test', lastName: 'User', fullName: 'Test User', email: 'test@example.gov.za' },
  darkMode: false,
  toggleDarkMode: vi.fn(),
  logout: vi.fn(),
  setCurrentPath: vi.fn(),
  pushToast: vi.fn(),
  tenantContexts: [] as Array<{ id: number; publicId: string; code: string; name: string; isCurrent: boolean }>,
  tenantContextPage: 1,
  tenantContextTotalPages: 0,
  tenantContextTotalCount: 0,
  tenantContextSearch: '',
  setTenantContextPage: vi.fn(),
  setTenantContextSearch: vi.fn(),
  currentMunicipalityId: null,
  switchMunicipality: vi.fn(),
}));
const api = vi.hoisted(() => ({ getNotifications: vi.fn(), markNotificationRead: vi.fn() }));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../api/api', () => api);

describe('TopBar notification feed', () => {
  beforeEach(() => {
    app.tenantContexts = [];
    app.tenantContextPage = 1;
    app.tenantContextTotalPages = 0;
    app.tenantContextTotalCount = 0;
    vi.clearAllMocks();
  });

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

  it('searches and pages the municipality context directory', async () => {
    app.tenantContexts = [{ id: 7, publicId: 'municipality-7', code: 'MUN-007', name: 'Seventh Municipality', isCurrent: true }];
    app.tenantContextTotalCount = 31;
    app.tenantContextTotalPages = 2;
    api.getNotifications.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 8, totalCount: 0, totalPages: 0, unreadCount: 0 } });

    render(<TopBar />);
    fireEvent.change(screen.getByRole('textbox', { name: 'Search municipality contexts' }), { target: { value: ' seventh ' } });
    await waitFor(() => expect(app.setTenantContextSearch).toHaveBeenCalledWith('seventh'));
    fireEvent.click(screen.getByRole('button', { name: 'Next municipality contexts' }));
    expect(app.setTenantContextPage).toHaveBeenCalledWith(2);
  });
});
