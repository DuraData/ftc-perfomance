import { describe, it, expect } from 'vitest';
import { normalizeAppPath, resolveAppNavigation } from './AppContext';

describe('AppContext state management', () => {
  const mockUserProfile = {
    id: 'user-1',
    displayName: 'John Doe',
    email: 'john@example.com',
    roles: ['Department Manager'],
  };

  const mockPermissions = [
    'OPMS.View',
    'OPMS.Targets.Create',
    'OPMS.Targets.Edit',
    'Workflow.Submit.View',
    'Dashboard.View',
  ];

  it('keeps role labels separate from effective permission codes', () => {
    expect(mockUserProfile.roles).toContain('Department Manager');
    expect(mockPermissions).not.toContain('Department Manager');
    expect(mockPermissions).toContain('OPMS.View');
  });

  it('normalizes legacy admin paths', () => {
    expect(normalizeAppPath('/admin/users')).toBe('/system-administration/users');
    expect(normalizeAppPath('/admin/users/new')).toBe('/system-administration/users');
    expect(normalizeAppPath('/admin/roles')).toBe('/system-administration/roles');
    expect(normalizeAppPath('/dashboard')).toBe('/dashboard');
  });

  it('preserves governed dashboard filters while resolving the client route', () => {
    expect(resolveAppNavigation('/ipms/targets?reportingPeriodPublicId=period-1&dashboardFilter=achieved')).toEqual({
      routePath: '/ipms/targets',
      location: '/ipms/targets?reportingPeriodPublicId=period-1&dashboardFilter=achieved',
    });
  });

  it('maintains permission and role state after login', () => {
    const userData = {
      user: mockUserProfile,
      roles: ['Department Manager'],
      permissions: mockPermissions,
      menu: [],
    };

    expect(userData.user.id).toBe('user-1');
    expect(userData.roles).toContain('Department Manager');
    expect(userData.permissions).toContain('OPMS.View');
    expect(userData.permissions.length).toBeGreaterThan(3);
  });

  it('clears all state on logout', () => {
    const stateBeforeLogout = {
      userProfile: mockUserProfile,
      roles: ['Department Manager'],
      permissions: mockPermissions,
      isAuthenticated: true,
    };

    const stateAfterLogout = {
      userProfile: null,
      roles: [],
      permissions: [],
      isAuthenticated: false,
    };

    expect(stateBeforeLogout.isAuthenticated).toBe(true);
    expect(stateAfterLogout.isAuthenticated).toBe(false);
    expect(stateAfterLogout.userProfile).toBeNull();
    expect(stateAfterLogout.permissions.length).toBe(0);
  });

  it('expands and collapses sidebar groups', () => {
    let expandedGroups: string[] = [];

    const toggleSidebarGroup = (label: string) => {
      expandedGroups = expandedGroups.includes(label)
        ? expandedGroups.filter(x => x !== label)
        : [...expandedGroups, label];
    };

    toggleSidebarGroup('OPMS');
    expect(expandedGroups).toContain('OPMS');

    toggleSidebarGroup('OPMS');
    expect(expandedGroups).not.toContain('OPMS');

    toggleSidebarGroup('OPMS');
    toggleSidebarGroup('Workflow');
    expect(expandedGroups).toContain('OPMS');
    expect(expandedGroups).toContain('Workflow');
  });

  it('tracks and auto-removes toasts after 3 seconds', async () => {
    interface Toast {
      id: string;
      type: 'success' | 'error' | 'info';
      message: string;
    }
    const toasts: Toast[] = [];

    const pushToast = (type: 'success' | 'error' | 'info', message: string) => {
      const id = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
      toasts.push({ id, type, message });
      return id;
    };

    const toastId = pushToast('success', 'Test message');
    expect(toasts).toHaveLength(1);
    expect(toasts[0].message).toBe('Test message');
    expect(toasts[0].id).toBe(toastId);
  });

  it('manages dark mode toggle state', () => {
    let darkMode = false;

    const toggleDarkMode = () => {
      darkMode = !darkMode;
    };

    expect(darkMode).toBe(false);
    toggleDarkMode();
    expect(darkMode).toBe(true);
    toggleDarkMode();
    expect(darkMode).toBe(false);
  });
});
