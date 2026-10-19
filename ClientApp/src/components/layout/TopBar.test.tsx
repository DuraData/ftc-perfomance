import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import axe from 'axe-core';
import { TopBar } from './TopBar';

const app = vi.hoisted(() => ({
  userProfile: { firstName: 'Test', lastName: 'User', fullName: 'Test User', email: 'test@example.gov.za' },
  permissions: ['Notifications.View'],
  authenticationGate: null as null | 'password_change' | 'mfa_enrollment',
  darkMode: false,
  toggleDarkMode: vi.fn(),
  logout: vi.fn(),
  setCurrentPath: vi.fn(),
  pushToast: vi.fn(),
  tenantContexts: [] as Array<{ publicId: string; code: string; name: string; isCurrent: boolean }>,
  tenantContextPage: 1,
  tenantContextTotalPages: 0,
  tenantContextTotalCount: 0,
  tenantContextSearch: '',
  setTenantContextPage: vi.fn(),
  setTenantContextSearch: vi.fn(),
  currentMunicipalityPublicId: null as string | null,
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
    app.currentMunicipalityPublicId = null;
    app.authenticationGate = null;
    app.permissions = ['Notifications.View'];
    vi.clearAllMocks();
  });

  it('loads only the recent server page while displaying the authoritative unread count', async () => {
    api.getNotifications.mockResolvedValue({
      success: true,
      data: {
        items: [
          { publicId: '11111111-1111-1111-1111-111111111111', recipientUserPublicId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', recipientName: 'Test User', type: 'Submission', title: 'First', message: 'First message', isRead: false, createdAt: '2026-10-02T10:00:00Z', readAt: null, readByUserPublicId: null, readByUserName: null },
          { publicId: '22222222-2222-2222-2222-222222222222', recipientUserPublicId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', recipientName: 'Test User', type: 'Approval', title: 'Second', message: 'Second message', isRead: true, createdAt: '2026-10-02T09:00:00Z', readAt: '2026-10-02T09:01:00Z', readByUserPublicId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', readByUserName: 'Test User' },
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
    app.tenantContexts = [{ publicId: '77777777-7777-7777-7777-777777777777', code: 'MUN-007', name: 'Seventh Municipality', isCurrent: true }];
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
      { publicId: '77777777-7777-7777-7777-777777777777', code: 'MUN-007', name: 'Seventh Municipality', isCurrent: true },
      { publicId: '88888888-8888-8888-8888-888888888888', code: 'MUN-008', name: 'Eighth Municipality', isCurrent: false },
    ];
    app.tenantContextTotalCount = 2;
    app.currentMunicipalityPublicId = '77777777-7777-7777-7777-777777777777';
    app.switchMunicipality.mockResolvedValue(true);
    api.getNotifications.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 8, totalCount: 0, totalPages: 0, unreadCount: 0 } });

    render(<TopBar />);
    const trigger = screen.getByRole('button', { name: 'Choose municipality context' });
    fireEvent.click(trigger);
    const narrowSelector = screen.getByRole('dialog', { name: 'Municipality context selector' });
    expect(trigger).toHaveAttribute('aria-controls', narrowSelector.id);
    expect(trigger).toHaveAttribute('aria-haspopup', 'dialog');
    await waitFor(() => expect(screen.getByRole('textbox', { name: 'Search municipality contexts on narrow screens' })).toHaveFocus());
    fireEvent.change(narrowSelector.querySelector('select')!, { target: { value: '88888888-8888-8888-8888-888888888888' } });

    await waitFor(() => expect(app.switchMunicipality).toHaveBeenCalledWith('88888888-8888-8888-8888-888888888888'));
    expect(app.pushToast).toHaveBeenCalledWith('success', 'Municipality context changed');
    expect(screen.queryByRole('dialog', { name: 'Municipality context selector' })).not.toBeInTheDocument();
  });

  it('provides a named keyboard-contained notification flyout and restores its trigger', async () => {
    api.getNotifications.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 8, totalCount: 0, totalPages: 0, unreadCount: 0 } });

    const result = render(<TopBar title="Dashboard" />);
    const trigger = await screen.findByRole('button', { name: 'Notifications' });
    fireEvent.click(trigger);

    const flyout = screen.getByRole('dialog', { name: 'Notifications' });
    expect(trigger).toHaveAttribute('aria-expanded', 'true');
    expect(trigger).toHaveAttribute('aria-controls', flyout.id);
    expect(trigger).toHaveAttribute('aria-haspopup', 'dialog');
    const refresh = screen.getByRole('button', { name: 'Refresh' });
    const openSettings = screen.getByRole('button', { name: 'Open settings' });
    await waitFor(() => expect(refresh).toHaveFocus());

    openSettings.focus();
    fireEvent.keyDown(document, { key: 'Tab' });
    expect(refresh).toHaveFocus();
    fireEvent.keyDown(document, { key: 'Tab', shiftKey: true });
    expect(openSettings).toHaveFocus();

    const accessibility = await axe.run(result.container, { rules: { 'color-contrast': { enabled: false } } });
    expect(accessibility.violations).toEqual([]);
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog', { name: 'Notifications' })).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
  });

  it('gives the account flyout a complete trigger relationship and Escape boundary', async () => {
    api.getNotifications.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 8, totalCount: 0, totalPages: 0, unreadCount: 0 } });

    render(<TopBar title="Dashboard" />);
    const trigger = screen.getByRole('button', { name: 'Open account menu for Test User' });
    fireEvent.click(trigger);

    const flyout = screen.getByRole('dialog', { name: 'Account menu' });
    expect(trigger).toHaveAttribute('aria-expanded', 'true');
    expect(trigger).toHaveAttribute('aria-controls', flyout.id);
    expect(trigger).toHaveAttribute('aria-haspopup', 'dialog');
    await waitFor(() => expect(screen.getByRole('button', { name: 'Profile' })).toHaveFocus());
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog', { name: 'Account menu' })).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
  });

  it('names the application search and appearance toggle', async () => {
    api.getNotifications.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 8, totalCount: 0, totalPages: 0, unreadCount: 0 } });

    render(<TopBar title="Dashboard" />);

    expect(screen.getByRole('textbox', { name: 'Search application' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Use dark mode' })).toBeInTheDocument();
  });

  it('does not request or expose notifications while account security remediation is required', () => {
    app.authenticationGate = 'password_change';

    render(<TopBar title="Account Security" />);

    expect(api.getNotifications).not.toHaveBeenCalled();
    expect(screen.queryByRole('button', { name: /^Notifications/ })).not.toBeInTheDocument();
  });

  it('does not request or expose notifications without the dynamic notification permission', () => {
    app.permissions = ['OPMS_KPI.READ'];

    render(<TopBar title="OPMS Targets" />);

    expect(api.getNotifications).not.toHaveBeenCalled();
    expect(screen.queryByRole('button', { name: /^Notifications/ })).not.toBeInTheDocument();
  });
});
