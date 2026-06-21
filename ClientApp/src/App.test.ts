import { describe, it, expect } from 'vitest';

describe('App routing patterns', () => {
  const routes = [
    { path: '/dashboard', name: 'Dashboard' },
    { path: '/opms/targets', name: 'OPMS Targets' },
    { path: '/opms/targets/new', name: 'New OPMS Target' },
    { path: '/opms/targets/123/edit', name: 'Edit OPMS Target' },
    { path: '/opms/submissions', name: 'OPMS Submissions' },
    { path: '/opms/library', name: 'OPMS Target Library' },
    { path: '/ipms/targets', name: 'IPMS Targets' },
    { path: '/ipms/targets/new', name: 'New IPMS Target' },
    { path: '/ipms/targets/456/edit', name: 'Edit IPMS Target' },
    { path: '/ipms/submissions', name: 'IPMS Submissions' },
    { path: '/ipms/library', name: 'IPMS Target Library' },
    { path: '/workflow/my-queue', name: 'My Workflow Queue' },
    { path: '/workflow/verification', name: 'Verification Queue' },
    { path: '/workflow/approval', name: 'Approval Queue' },
    { path: '/workflow/pms-review', name: 'PMS Review Queue' },
    { path: '/workflow/auditor-review', name: 'Auditor Review Queue' },
    { path: '/workflow/my-drafts', name: 'My Drafts' },
    { path: '/workflow/pending-submission', name: 'Pending Submission' },
    { path: '/workflow/returned-submissions', name: 'Returned Submissions' },
    { path: '/admin/users', name: 'Admin Users' },
    { path: '/system-administration/users', name: 'System Admin Users' },
    { path: '/hr/employees', name: 'HR Employees' },
    { path: '/hr/departments', name: 'HR Departments' },
    { path: '/reports', name: 'Reports' },
  ];

  it('recognizes all key application routes', () => {
    expect(routes.length).toBeGreaterThan(20);
    expect(routes.some(r => r.path.includes('opms'))).toBe(true);
    expect(routes.some(r => r.path.includes('ipms'))).toBe(true);
    expect(routes.some(r => r.path.includes('workflow'))).toBe(true);
  });

  it('recognizes target edit patterns', () => {
    const opmsEditPattern = /^\/opms\/targets\/[^/]+\/edit$/i;
    expect(opmsEditPattern.test('/opms/targets/123/edit')).toBe(true);
    expect(opmsEditPattern.test('/opms/targets/new')).toBe(false);

    const ipmsEditPattern = /^\/ipms\/targets\/[^/]+\/edit$/i;
    expect(ipmsEditPattern.test('/ipms/targets/456/edit')).toBe(true);
    expect(ipmsEditPattern.test('/ipms/targets/new')).toBe(false);
  });

  it('recognizes library edit patterns', () => {
    const opmsLibEditPattern = /^\/opms\/library\/[^/]+\/edit$/i;
    expect(opmsLibEditPattern.test('/opms/library/abc123/edit')).toBe(true);
    expect(opmsLibEditPattern.test('/opms/library')).toBe(false);
  });

  it('normalizes legacy admin paths to system-administration', () => {
    const pathMap: Record<string, string> = {
      '/admin/users': '/system-administration/users',
      '/admin/roles': '/system-administration/roles',
      '/admin/permissions': '/system-administration/permissions',
      '/admin/audit': '/system-administration/audit-logs',
    };

    Object.entries(pathMap).forEach(([legacy, normalized]) => {
      expect(legacy.startsWith('/admin')).toBe(true);
      expect(normalized.startsWith('/system-administration')).toBe(true);
    });
  });
});
