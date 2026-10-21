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

const workflowMenu: MenuItem[] = [
  { label: 'My Workflow', path: '/workflow/my-queue', isDivider: false },
];

const configurationMenu: MenuItem[] = [
  { label: 'Workflow Governance', path: '/admin/approval-setup', isDivider: false },
];

const canonicalRouteMenu: MenuItem[] = [
  {
    label: 'Organisation',
    isDivider: false,
    children: [
      { label: 'Employees', path: '/hr/employees', isDivider: false },
      { label: 'Departments', path: '/hr/departments', isDivider: false },
      { label: 'Positions', path: '/hr/positions', isDivider: false },
    ],
  },
  {
    label: 'IDP',
    isDivider: false,
    children: [
      { label: 'Plans', path: '/idp/plans', isDivider: false },
      { label: 'Hierarchy', path: '/idp/hierarchy', isDivider: false },
      { label: 'Reports', path: '/idp/reports', isDivider: false },
    ],
  },
  {
    label: 'Administration',
    isDivider: false,
    children: [
      { label: 'Users', path: '/system-administration/users', isDivider: false },
      { label: 'Roles', path: '/system-administration/roles', isDivider: false },
      { label: 'Permissions', path: '/system-administration/permissions', isDivider: false },
      { label: 'Audit Logs', path: '/system-administration/audit-logs', isDivider: false },
    ],
  },
];

describe('AccessControl helpers', () => {
  it('derives route access from the backend-filtered navigation tree', () => {
    expect(canAccessPath('/dashboard', menu)).toBe(true);
    expect(canAccessPath('/workflow/verification', menu)).toBe(true);
  });

  it('lets read-only detail pages inherit their authorized parent route', () => {
    expect(canAccessPath('/opms/targets/target-1', menu)).toBe(true);
  });

  it('requires the matching CRUD permission for inherited create and edit routes', () => {
    expect(canAccessPath('/opms/targets/new', menu)).toBe(false);
    expect(canAccessPath('/opms/targets/target-1/edit', menu)).toBe(false);
    expect(canAccessPath('/opms/targets/new', menu, ['OPMS_KPI.CREATE'])).toBe(true);
    expect(canAccessPath('/opms/targets/target-1/edit', menu, ['opms_kpi.update'])).toBe(true);
  });

  it('denies routes absent from the filtered menu', () => {
    expect(canAccessPath('/system-administration/users', menu)).toBe(false);
    expect(canAccessPath('/unregistered-feature', menu)).toBe(false);
    expect(canAccessPath('/dashboard', [])).toBe(false);
  });

  it('denies the retired generic task workspace while governed workflow routes remain available', () => {
    expect(canAccessPath('/tasks', menu)).toBe(false);
    expect(canAccessPath('/workflow/verification', menu)).toBe(true);
  });

  it('lets every named work-queue view inherit the backend-authorized workflow parent', () => {
    for (const path of [
      '/workflow/my-drafts',
      '/workflow/pending-submission',
      '/workflow/returned-submissions',
      '/workflow/under-verification',
      '/workflow/under-review',
      '/workflow/under-approval',
      '/workflow/internal-audit-returned',
      '/workflow/approved-closed',
    ]) expect(canAccessPath(path, workflowMenu)).toBe(true);
    expect(canAccessPath('/workflow/verification', workflowMenu)).toBe(false);
  });

  it('maps the requirements workflow-configuration route to the governed navigation entry', () => {
    expect(canAccessPath('/configuration/workflow', configurationMenu)).toBe(true);
    expect(canAccessPath('/configuration/workflow', workflowMenu)).toBe(false);
  });

  it.each([
    ['/hr/contacts', '/hr/employees'],
    ['/hr/resumes', '/hr/employees'],
    ['/admin/organisations', '/hr/departments'],
    ['/admin/occupations', '/hr/positions'],
    ['/idp/overview', '/idp/plans'],
    ['/idp/strategic-objectives', '/idp/hierarchy'],
    ['/idp/projects', '/idp/hierarchy'],
    ['/idp/kpis', '/idp/hierarchy'],
    ['/reports/idp-summary', '/idp/reports'],
    ['/admin/users', '/system-administration/users'],
    ['/admin/roles', '/system-administration/roles'],
    ['/admin/permissions', '/system-administration/permissions'],
    ['/admin/audit', '/system-administration/audit-logs'],
  ] as const)('maps compatibility route %s only through canonical navigation %s', (alias, canonical) => {
    const authorizedItem: MenuItem[] = [{ label: 'Authorized', path: canonical, isDivider: false }];
    expect(canAccessPath(alias, authorizedItem)).toBe(true);
    expect(canAccessPath(alias, [{ label: 'Unrelated', path: '/dashboard', isDivider: false }])).toBe(false);
  });

  it('admits the complete compatibility-route matrix through a canonical filtered menu', () => {
    for (const path of [
      '/hr/contacts', '/hr/resumes', '/admin/organisations', '/admin/occupations',
      '/idp/overview', '/idp/strategic-objectives', '/idp/projects', '/idp/kpis', '/reports/idp-summary',
      '/admin/users', '/admin/roles', '/admin/permissions', '/admin/audit',
    ]) expect(canAccessPath(path, canonicalRouteMenu)).toBe(true);
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
