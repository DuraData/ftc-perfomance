import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { Settings } from './Settings';

const api = vi.hoisted(() => ({ getAuthSessions: vi.fn(), revokeAuthSession: vi.fn(), revokeAllAuthSessions: vi.fn(), getMfaStatus: vi.fn(), setupMfa: vi.fn(), enableMfa: vi.fn(), disableMfa: vi.fn(), changePassword: vi.fn(), getMyNotificationPreferences: vi.fn(), saveMyNotificationPreferences: vi.fn() }));
const app = vi.hoisted(() => ({ logout: vi.fn(), pushToast: vi.fn(), userProfile: null as null | { mustChangePassword: boolean } }));
vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ ...app, darkMode: false, toggleDarkMode: vi.fn() }) }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));

describe('Security session settings', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.setItem('settings_active_tab', 'security');
    api.getAuthSessions.mockResolvedValue({ success: true, data: [{ sessionId: 'session-1', createdAt: '2026-10-02T08:00:00Z', lastUsedAt: '2026-10-02T09:00:00Z', absoluteExpiresAt: '2026-10-03T08:00:00Z', userAgent: 'Test Browser', authenticationMethod: 'LOCAL', isCurrent: false }] });
    api.revokeAuthSession.mockResolvedValue({ success: true, data: true });
    api.revokeAllAuthSessions.mockResolvedValue({ success: true, data: 1 });
    api.getMfaStatus.mockResolvedValue({ success: true, data: { isEnabled: false, enrollmentRequired: true, recoveryCodesLeft: 0 } });
    api.setupMfa.mockResolvedValue({ success: true, data: { sharedKey: 'abcd efgh', authenticatorUri: 'otpauth://totp/test' } });
    api.enableMfa.mockResolvedValue({ success: true, data: { recoveryCodes: ['recovery-one', 'recovery-two'] } });
    api.changePassword.mockResolvedValue({ success: true, data: true });
    api.getMyNotificationPreferences.mockResolvedValue({ success: true, data: { emailEnabled: true, smsEnabled: false, dailyDigestEnabled: true, weeklySummaryEnabled: false, rowVersion: 'AQ==' } });
    api.saveMyNotificationPreferences.mockImplementation(async (value) => ({ success: true, data: { ...value, rowVersion: 'Ag==' }, message: 'Optional preferences saved.' }));
    app.userProfile = null;
  });

  it('loads and persists notification preferences while explaining mandatory delivery', async () => {
    localStorage.setItem('settings_active_tab', 'notifications');
    render(<Settings />);
    await waitFor(() => expect(api.getMyNotificationPreferences).toHaveBeenCalledOnce());
    expect(screen.getByText(/Mandatory workflow, deadline, RFI and escalation notifications cannot be disabled/)).toBeInTheDocument();
    fireEvent.click(screen.getByLabelText('SMS'));
    fireEvent.click(screen.getByRole('button', { name: 'Save notification preferences' }));
    await waitFor(() => expect(api.saveMyNotificationPreferences).toHaveBeenCalledWith(expect.objectContaining({ smsEnabled: true, rowVersion: 'AQ==' })));
  });

  it('shows real sessions and revokes a selected session', async () => {
    render(<Settings />);
    expect(await screen.findByText(/Test Browser/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Revoke' }));
    await waitFor(() => expect(api.revokeAuthSession).toHaveBeenCalledWith('session-1', 'User revoked session from account settings'));
  });

  it('signs out locally after revoking all sessions', async () => {
    render(<Settings />);
    await screen.findByText(/Test Browser/);
    fireEvent.click(screen.getByRole('button', { name: 'Sign out all' }));
    await waitFor(() => expect(api.revokeAllAuthSessions).toHaveBeenCalled());
    expect(app.logout).toHaveBeenCalled();
  });

  it('enrolls an authenticator and shows one-time recovery codes', async () => {
    render(<Settings />);
    expect(await screen.findByText(/privileged permissions require MFA/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Set up authenticator' }));
    expect(await screen.findByLabelText('Authenticator shared key')).toHaveTextContent('abcd efgh');
    fireEvent.change(screen.getByLabelText('Authenticator code'), { target: { value: '123456' } });
    fireEvent.click(screen.getByRole('button', { name: 'Verify and enable' }));
    expect(await screen.findByText('recovery-one')).toBeInTheDocument();
    expect(api.enableMfa).toHaveBeenCalledWith('123456');
  });

  it('enforces a matching new password and signs out after changing it', async () => {
    app.userProfile = { mustChangePassword: true };
    render(<Settings />);
    expect(screen.getByText(/must change your password/i)).toBeInTheDocument();
    await screen.findByText(/Test Browser/);
    fireEvent.change(screen.getByLabelText('Current Password'), { target: { value: 'OldPassword1!' } });
    fireEvent.change(screen.getByLabelText('New Password'), { target: { value: 'NewPassword2@' } });
    fireEvent.change(screen.getByLabelText('Confirm'), { target: { value: 'NewPassword2@' } });
    fireEvent.click(screen.getByRole('button', { name: 'Change password' }));
    await waitFor(() => expect(api.changePassword).toHaveBeenCalledWith('OldPassword1!', 'NewPassword2@'));
    expect(app.logout).toHaveBeenCalled();
  });
});
