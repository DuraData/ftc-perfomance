import type { MenuItem } from '../../types';
import { RESTRICTED_ACCOUNT_MENU } from '../../context/AppContext';
import { canAccessPath, hasPermissionCode } from './AccessControl';

const menu: MenuItem[] = [
  { label: 'Dashboard', path: '/dashboard', isDivider: false },
  {
    label: 'Performance',
    isDivider: false,
    children: [
      { label: 'OPMS targets', path: '/opms/targets', isDivider: false },
      { label: 'Verification', path: '/workflow/verification', isDivider: false },
    ],
  },
];

describe('AccessControl helpers', () => {
  it('derives route access from the backend-filtered navigation tree', () => {
    expect(canAccessPath('/dashboard', menu)).toBe(true);
    expect(canAccessPath('/workflow/verification', menu)).toBe(true);
  });

  it('lets detail and edit pages inherit their authorized parent route', () => {
    expect(canAccessPath('/opms/targets/target-1', menu)).toBe(true);
    expect(canAccessPath('/opms/targets/target-1/edit', menu)).toBe(true);
  });

  it('denies routes absent from the filtered menu', () => {
    expect(canAccessPath('/system-administration/users', menu)).toBe(false);
    expect(canAccessPath('/unregistered-feature', menu)).toBe(false);
    expect(canAccessPath('/dashboard', [])).toBe(false);
  });

  it('limits mandatory account setup to the security settings route', () => {
    expect(canAccessPath('/settings', RESTRICTED_ACCOUNT_MENU)).toBe(true);
    expect(canAccessPath('/dashboard', RESTRICTED_ACCOUNT_MENU)).toBe(false);
    expect(canAccessPath('/system-administration/security', RESTRICTED_ACCOUNT_MENU)).toBe(false);
  });

  it('matches operation permission codes case-insensitively', () => {
    expect(hasPermissionCode(['OPMS.View'], ['opms.view'])).toBe(true);
    expect(hasPermissionCode(['Workflow.Submit.View'], ['WORKFLOW.SUBMIT.VIEW'])).toBe(true);
    expect(hasPermissionCode(['OPMS.View', 'Targets.View'], ['targets.view'])).toBe(true);
  });
});
