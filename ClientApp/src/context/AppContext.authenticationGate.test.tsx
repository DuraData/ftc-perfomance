import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AppProvider, RESTRICTED_ACCOUNT_MENU, useApp } from './AppContext';
import { canAccessPath } from '../components/security/AccessControl';
import { login as apiLogin, getMyTenantContextsPage } from '../api/api';

vi.mock('../api/api', () => ({
  getCurrentMunicipalityId: vi.fn(() => null),
  getMyMenu: vi.fn(),
  getMyPermissions: vi.fn(),
  getMyTenantContextsPage: vi.fn(),
  login: vi.fn(),
  completeEnterpriseLogin: vi.fn(),
  isAuthenticated: vi.fn(() => true),
  logout: vi.fn(),
  setCurrentMunicipalityId: vi.fn(),
}));

const requiredUser = {
  publicId: '00000000-0000-0000-0000-000000000001',
  userName: 'admin@opms.local',
  firstName: 'System',
  lastName: 'Administrator',
  fullName: 'System Administrator',
  email: 'admin@opms.local',
  isActive: true,
  mustChangePassword: true,
};

function Harness() {
  const app = useApp();
  return <>
    <button type="button" onClick={() => void app.login('admin@opms.local', 'local-test-password')}>Sign in</button>
    <output aria-label="path">{app.currentPath}</output>
    <output aria-label="menu">{app.menuItems.map(item => item.code).join(',')}</output>
    <output aria-label="access-ready">{String(app.accessReady)}</output>
  </>;
}

describe('mandatory account-security bootstrap', () => {
  beforeEach(() => {
    localStorage.clear();
    window.history.replaceState({}, '', '/login');
    vi.clearAllMocks();
  });

  it('routes first login to the only permitted security-settings page', async () => {
    vi.mocked(apiLogin).mockResolvedValue({
      success: true,
      data: {
        expiresAt: '2026-10-08T00:00:00Z',
        user: requiredUser,
        roles: ['Super Admin'],
        permissions: ['SECURITY.SYSTEM_SCOPE', 'NAV.DASHBOARD'],
        menu: [{ label: 'Dashboard', path: '/dashboard', isDivider: false }],
        mfaEnrollmentRequired: true,
      },
    });

    render(<AppProvider><Harness /></AppProvider>);
    fireEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(screen.getByLabelText('path')).toHaveTextContent('/settings'));
    expect(screen.getByLabelText('menu')).toHaveTextContent('NAV.ACCOUNT.SECURITY');
    expect(canAccessPath('/settings', RESTRICTED_ACCOUNT_MENU)).toBe(true);
    expect(canAccessPath('/dashboard', RESTRICTED_ACCOUNT_MENU)).toBe(false);
    expect(localStorage.getItem('authentication_gate')).toBe('password_change');
    expect(getMyTenantContextsPage).not.toHaveBeenCalled();
    expect(screen.getByLabelText('access-ready')).toHaveTextContent('true');
  });

  it('recovers an existing first-login session from the former access-denied screen', async () => {
    localStorage.setItem('user_profile', JSON.stringify(requiredUser));
    localStorage.setItem('roles', JSON.stringify(['Super Admin']));
    localStorage.setItem('menu_items', JSON.stringify([{ label: 'Dashboard', path: '/dashboard', isDivider: false }]));
    window.history.replaceState({}, '', '/dashboard');

    render(<AppProvider><Harness /></AppProvider>);

    await waitFor(() => expect(screen.getByLabelText('path')).toHaveTextContent('/settings'));
    expect(screen.getByLabelText('menu')).toHaveTextContent('NAV.ACCOUNT.SECURITY');
    expect(window.location.pathname).toBe('/settings');
    expect(getMyTenantContextsPage).not.toHaveBeenCalled();
    expect(screen.getByLabelText('access-ready')).toHaveTextContent('true');
  });

  it('does not resolve an empty menu as access denied while tenant access is loading', async () => {
    let resolveContexts!: (value: Awaited<ReturnType<typeof getMyTenantContextsPage>>) => void;
    vi.mocked(getMyTenantContextsPage).mockReturnValue(new Promise(resolve => { resolveContexts = resolve; }));
    localStorage.setItem('user_profile', JSON.stringify({ ...requiredUser, mustChangePassword: false }));
    localStorage.setItem('roles', JSON.stringify(['Super Admin']));
    window.history.replaceState({}, '', '/dashboard');

    render(<AppProvider><Harness /></AppProvider>);

    await waitFor(() => expect(getMyTenantContextsPage).toHaveBeenCalled());
    expect(screen.getByLabelText('access-ready')).toHaveTextContent('false');
    expect(screen.getByLabelText('menu')).toBeEmptyDOMElement();

    resolveContexts({
      success: true,
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
    });
    await waitFor(() => expect(screen.getByLabelText('access-ready')).toHaveTextContent('true'));
  });
});
