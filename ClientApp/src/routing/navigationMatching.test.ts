import { describe, expect, it, vi } from 'vitest';
import type { MenuItem } from '../types';
import { isNavigationItemActive, resolveNavigationSelection } from './navigationMatching';

const leaf = (code: string, label: string, path: string, matchMode: MenuItem['matchMode'] = 'Prefix'): MenuItem =>
  ({ code, label, path, matchMode, isDivider: false });

const registry: MenuItem[] = [
  leaf('NAV.DASHBOARD', 'Dashboard', '/dashboard', 'Exact'),
  { code: 'NAV.SDBIP', label: 'SDBIP / OPMS', isDivider: false, children: [
    leaf('NAV.SDBIP.CAPTURE', 'Performance Reporting', '/opms/submissions'),
    leaf('NAV.SDBIP.REGISTER', 'SDBIP Register', '/opms/targets'),
  ] },
  leaf('NAV.POE', 'POE', '/poe'),
  leaf('NAV.REPORTS', 'Reports', '/reports'),
  { code: 'NAV.WORKFLOW', label: 'My Workflow', isDivider: false, children: [
    leaf('NAV.WORKFLOW.APPROVE', 'Approval', '/workflow/approval'),
    leaf('NAV.WORKFLOW.AUDIT', 'Internal Audit', '/workflow/internal-audit'),
  ] },
  { code: 'NAV.IDP', label: 'IDP', isDivider: false, children: [
    leaf('NAV.IDP.PROJECTS', 'IDP Projects', '/idp/projects'),
  ] },
  { code: 'NAV.ADMIN', label: 'Administration', isDivider: false, children: [
    leaf('NAV.ADMIN.ROLES', 'Roles', '/admin/security/roles'),
    leaf('NAV.ADMIN.USERS', 'Users', '/admin/security/users'),
    leaf('NAV.ADMIN.PERMISSIONS', 'Permissions', '/admin/security/permissions'),
  ] },
];

function activeCode(path: string) {
  return resolveNavigationSelection(registry, path).activeCode;
}

describe('central navigation ownership', () => {
  it.each([
    ['/opms/submissions', 'NAV.SDBIP.CAPTURE'],
    ['/opms/submissions/123', 'NAV.SDBIP.CAPTURE'],
    ['/opms/submissions/?period=Q2#history', 'NAV.SDBIP.CAPTURE'],
    ['/poe', 'NAV.POE'],
    ['/poe/123/history', 'NAV.POE'],
    ['/admin/security/roles', 'NAV.ADMIN.ROLES'],
    ['/idp/projects/123', 'NAV.IDP.PROJECTS'],
    ['/reports', 'NAV.REPORTS'],
    ['/workflow/internal-audit', 'NAV.WORKFLOW.AUDIT'],
  ])('assigns %s to one primary owner', (path, expected) => {
    expect(activeCode(path)).toBe(expected);
  });

  it('keeps Performance Reporting and POE mutually exclusive', () => {
    const reporting = resolveNavigationSelection(registry, '/opms/submissions');
    const poe = resolveNavigationSelection(registry, '/poe');
    expect(isNavigationItemActive(registry[1].children![0], reporting)).toBe(true);
    expect(isNavigationItemActive(registry[2], reporting)).toBe(false);
    expect(isNavigationItemActive(registry[2], poe)).toBe(true);
    expect(isNavigationItemActive(registry[1].children![0], poe)).toBe(false);
  });

  it('uses the longest, most-specific valid owner', () => {
    const routes = [
      leaf('NAV.ADMIN.ROOT', 'Admin', '/admin'),
      leaf('NAV.ADMIN.SECURITY', 'Security', '/admin/security'),
      leaf('NAV.ADMIN.SECURITY.ROLES', 'Roles', '/admin/security/roles'),
    ];
    expect(resolveNavigationSelection(routes, '/admin/security/roles/123').activeCode).toBe('NAV.ADMIN.SECURITY.ROLES');
  });

  it('returns at most one active leaf for every registered route sample', () => {
    const samples = [
      '/dashboard', '/opms/submissions', '/opms/submissions/123', '/poe', '/poe/123/history',
      '/reports', '/workflow/approval', '/workflow/internal-audit', '/idp/projects/123',
      '/admin/security/roles', '/admin/security/users', '/admin/security/permissions',
    ];
    for (const sample of samples) {
      const selection = resolveNavigationSelection(registry, sample);
      const allLeaves = registry.flatMap(item => item.children ?? [item]);
      expect(allLeaves.filter(item => isNavigationItemActive(item, selection))).toHaveLength(1);
    }
  });

  it('logs an ambiguity diagnostic but still renders one deterministic owner', () => {
    const warning = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    const selection = resolveNavigationSelection([
      leaf('NAV.A', 'A', '/duplicate'),
      leaf('NAV.B', 'B', '/duplicate'),
    ], '/duplicate');
    expect(selection.activeCode).toBe('NAV.A');
    expect(warning).toHaveBeenCalledWith('Ambiguous navigation ownership detected.', expect.objectContaining({ navigationCodes: ['NAV.A', 'NAV.B'] }));
    warning.mockRestore();
  });
});
