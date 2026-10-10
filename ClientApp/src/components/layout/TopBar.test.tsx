import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TopBar } from './TopBar';

const app = vi.hoisted(() => ({
  userProfile: { firstName: 'Test', lastName: 'User', fullName: 'Test User', email: 'test@example.gov.za' },
  authenticationGate: null as null | 'password_change' | 'mfa_enrollment',
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
  currentMunicipalityId: null as number | null,
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
    app.currentMunicipalityId = null;
    app.authenticationGate = null;
    vi.clearAllMocks();
  });

  it('loads only the recent server page while displaying the authoritative unread count', async () => {
    api.getNotifications.mockResolvedValue({
      success: true,
      data: {
        items: [
          { publicId: '11111111-1111-1111-1111-111111111111', recipientUserPublicId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', recipientName: 'Test User', type: 'Submission', title: 'First', message: 'First message', isRead: false, createdAt: '2026-10-02T10:00:00Z' },
          { publicId: '22222222-2222-2222-2222-222222222222', recipientUserPublicId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', recipientName: 'Test User', type: 'Approval', title: 'Second', message: 'Second message', isRead: true, createdAt: '2026-10-02T09:00:00Z' },
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
    await waitFor(() => expect(api.markNotificationRead).toHaveBeenCalledWith('11111111-1111-1111-1111-111111111111'));
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

  it('exposes municipality selection from the narrow-layout menu', async () => {
    app.tenantContexts = [
      { id: 7, publicId: 'municipality-7', code: 'MUN-007', name: 'Seventh Municipality', isCurrent: true },
      { id: 8, publicId: 'municipality-8', code: 'MUN-008', name: 'Eighth Municipality', isCurrent: false },
    ];
    app.tenantContextTotalCount = 2;
    app.currentMunicipalityId = 7;
    app.switchMunicipality.mockResolvedValue(true);
    api.getNotifications.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 8, totalCount: 0, totalPages: 0, unreadCount: 0 } });

    render(<TopBar />);
    fireEvent.click(screen.getByRole('button', { name: 'Choose municipality context' }));
    const narrowSelector = screen.getByRole('dialog', { name: 'Municipality context selector' });
    fireEvent.change(narrowSelector.querySelector('select')!, { target: { value: '8' } });

    await waitFor(() => expect(app.switchMunicipality).toHaveBeenCalledWith(8));
    expect(app.pushToast).toHaveBeenCalledWith('success', 'Municipality context changed');
    expect(screen.queryByRole('dialog', { name: 'Municipality context selector' })).not.toBeInTheDocument();
  });

  it('does not request or expose notifications while account security remediation is required', () => {
    app.authenticationGate = 'password_change';

    render(<TopBar title="Account Security" />);

    expect(api.getNotifications).not.toHaveBeenCalled();
    expect(screen.queryByRole('button', { name: /^Notifications/ })).not.toBeInTheDocument();
  });
});
