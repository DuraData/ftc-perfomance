import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AppProvider, RESTRICTED_ACCOUNT_MENU, useApp } from './AppContext';
import { canAccessPath } from '../components/security/AccessControl';
import { AUTH_SESSION_EXPIRED_EVENT, login as apiLogin, getMunicipalityFinancialYearMastersPage, getMyMenu, getMyPermissions, getMyTenantContextsPage } from '../api/api';

vi.mock('../api/api', () => ({
  AUTH_SESSION_EXPIRED_EVENT: 'opms:auth-session-expired',
  getCurrentMunicipalityPublicId: vi.fn(() => null),
  getMyMenu: vi.fn(),
  getMyPermissions: vi.fn(),
  getMyTenantContextsPage: vi.fn(),
  getMunicipalityFinancialYearMastersPage: vi.fn(),
  login: vi.fn(),
  completeEnterpriseLogin: vi.fn(),
  isAuthenticated: vi.fn(() => true),
  logout: vi.fn(),
  setCurrentMunicipalityPublicId: vi.fn(),
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
    <output aria-label="authenticated">{String(app.isAuthenticated)}</output>
    <output aria-label="active-financial-year">{app.currentFinancialYearLabel ?? 'none'}</output>
    <button type="button" onClick={() => app.switchFinancialYear('22222222-2222-4222-8222-222222222222')}>Switch financial year</button>
    <button type="button" onClick={() => app.setCurrentPath('/workflow/verification?municipalityFinancialYearPublicId=11111111-1111-4111-8111-111111111112')}>Open scoped route</button>
  </>;
}

describe('mandatory account-security bootstrap', () => {
  beforeEach(() => {
    localStorage.clear();
    window.history.replaceState({}, '', '/login');
    vi.clearAllMocks();
    vi.mocked(getMyPermissions).mockResolvedValue({ success: true, data: [] });
    vi.mocked(getMyMenu).mockResolvedValue({ success: true, data: [] });
    vi.mocked(getMunicipalityFinancialYearMastersPage).mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 } });
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

  it('retains backend-authorized system access when no municipality context is required', async () => {
    vi.mocked(apiLogin).mockResolvedValue({
      success: true,
      data: {
        expiresAt: '2026-10-08T00:00:00Z',
        user: { ...requiredUser, mustChangePassword: false },
        roles: ['Super Admin'],
        permissions: ['SECURITY.SYSTEM_SCOPE', 'NAV.DASHBOARD'],
        menu: [{ label: 'Dashboard', path: '/dashboard', isDivider: false, code: 'NAV.DASHBOARD' }],
        mfaEnrollmentRequired: false,
      },
    });
    vi.mocked(getMyTenantContextsPage).mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
    });
    vi.mocked(getMyPermissions).mockResolvedValue({ success: true, data: ['SECURITY.SYSTEM_SCOPE', 'NAV.DASHBOARD'] });
    vi.mocked(getMyMenu).mockResolvedValue({ success: true, data: [{ label: 'Dashboard', path: '/dashboard', isDivider: false, code: 'NAV.DASHBOARD' }] });

    render(<AppProvider><Harness /></AppProvider>);
    fireEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(screen.getByLabelText('menu')).toHaveTextContent('NAV.DASHBOARD'));
    expect(screen.getByLabelText('path')).toHaveTextContent('/dashboard');
    expect(localStorage.getItem('permissions')).toContain('SECURITY.SYSTEM_SCOPE');
    expect(localStorage.getItem('menu_items')).toContain('NAV.DASHBOARD');
    expect(screen.getByLabelText('access-ready')).toHaveTextContent('true');
  });

  it('selects, persists, and dynamically switches the active municipality financial year', async () => {
    vi.mocked(apiLogin).mockResolvedValue({
      success: true,
      data: {
        expiresAt: '2026-10-08T00:00:00Z',
        user: { ...requiredUser, mustChangePassword: false },
        roles: ['Performance Manager'], permissions: ['Dashboard.View'], menu: [], mfaEnrollmentRequired: false,
      },
    });
    vi.mocked(getMyTenantContextsPage).mockResolvedValue({
      success: true,
      data: { items: [{ publicId: '11111111-1111-4111-8111-111111111111', code: 'MUN', name: 'Municipality', isCurrent: true }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    vi.mocked(getMyPermissions).mockResolvedValue({ success: true, data: ['Dashboard.View', 'FINANCIAL_YEAR.READ'] });
    vi.mocked(getMyMenu).mockResolvedValue({ success: true, data: [] });
    vi.mocked(getMunicipalityFinancialYearMastersPage).mockResolvedValue({
      success: true,
      data: {
        items: [
          { publicId: '11111111-1111-4111-8111-111111111112', financialYearPublicId: 'fy-1', code: '2025/26', name: '2025/26', isCurrent: true, isActive: true, effectiveFrom: '2025-07-01', rowVersion: 'AQ==' },
          { publicId: '22222222-2222-4222-8222-222222222222', financialYearPublicId: 'fy-2', code: '2026/27', name: '2026/27', isCurrent: false, isActive: true, effectiveFrom: '2026-07-01', rowVersion: 'Ag==' },
        ], page: 1, pageSize: 100, totalCount: 2, totalPages: 1,
      },
    });

    render(<AppProvider><Harness /></AppProvider>);
    fireEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(screen.getByLabelText('active-financial-year')).toHaveTextContent('2025/26'));
    expect(localStorage.getItem('active_financial_year:11111111-1111-4111-8111-111111111111')).toBe('11111111-1111-4111-8111-111111111112');
    fireEvent.click(screen.getByRole('button', { name: 'Switch financial year' }));
    expect(screen.getByLabelText('active-financial-year')).toHaveTextContent('2026/27');
    expect(localStorage.getItem('active_financial_year:11111111-1111-4111-8111-111111111111')).toBe('22222222-2222-4222-8222-222222222222');
    fireEvent.click(screen.getByRole('button', { name: 'Open scoped route' }));
    expect(screen.getByLabelText('active-financial-year')).toHaveTextContent('2025/26');
    expect(window.location.search).toContain('municipalityFinancialYearPublicId=11111111-1111-4111-8111-111111111112');
  });

  it('clears stale authenticated state when the API reports an expired session', async () => {
    localStorage.setItem('user_profile', JSON.stringify({ ...requiredUser, mustChangePassword: false }));
    localStorage.setItem('roles', JSON.stringify(['Super Admin']));
    localStorage.setItem('permissions', JSON.stringify(['SECURITY.SYSTEM_SCOPE']));
    localStorage.setItem('menu_items', JSON.stringify([{ label: 'Dashboard', path: '/dashboard', isDivider: false, code: 'NAV.DASHBOARD' }]));
    window.history.replaceState({}, '', '/settings');

    render(<AppProvider><Harness /></AppProvider>);
    await waitFor(() => expect(screen.getByLabelText('authenticated')).toHaveTextContent('true'));

    window.dispatchEvent(new Event(AUTH_SESSION_EXPIRED_EVENT));

    await waitFor(() => expect(screen.getByLabelText('authenticated')).toHaveTextContent('false'));
    expect(screen.getByLabelText('path')).toHaveTextContent('/login');
    expect(localStorage.getItem('user_profile')).toBeNull();
    expect(localStorage.getItem('roles')).toBeNull();
    expect(localStorage.getItem('permissions')).toBeNull();
    expect(localStorage.getItem('menu_items')).toBeNull();
  });
});
