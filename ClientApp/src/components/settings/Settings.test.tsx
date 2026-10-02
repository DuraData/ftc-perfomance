import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { Settings } from './Settings';

const api = vi.hoisted(() => ({ getAuthSessions: vi.fn(), revokeAuthSession: vi.fn(), revokeAllAuthSessions: vi.fn() }));
const app = vi.hoisted(() => ({ logout: vi.fn(), pushToast: vi.fn() }));
vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ ...app, userProfile: null, darkMode: false, toggleDarkMode: vi.fn() }) }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));

describe('Security session settings', () => {
  beforeEach(() => {
    localStorage.setItem('settings_active_tab', 'security');
    api.getAuthSessions.mockResolvedValue({ success: true, data: [{ sessionId: 'session-1', createdAt: '2026-10-02T08:00:00Z', lastUsedAt: '2026-10-02T09:00:00Z', absoluteExpiresAt: '2026-10-03T08:00:00Z', userAgent: 'Test Browser', isCurrent: false }] });
    api.revokeAuthSession.mockResolvedValue({ success: true, data: true });
    api.revokeAllAuthSessions.mockResolvedValue({ success: true, data: 1 });
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
});
