export function normalizeRoutePath(path: string) {
  const normalized = path.split(/[?#]/, 1)[0].replace(/\/{2,}/g, '/').replace(/\/+$/, '').toLowerCase();
  return normalized || '/';
}

const canonicalRouteByAlias: Record<string, string> = {
  '/admin/audit': '/system-administration/audit-logs',
  '/admin/organisations': '/hr/departments',
  '/admin/occupations': '/hr/positions',
  '/admin/permissions': '/system-administration/permissions',
  '/admin/roles': '/system-administration/roles',
  '/admin/users': '/system-administration/users',
  '/configuration/workflow': '/admin/approval-setup',
  '/hr/contacts': '/hr/employees',
  '/hr/resumes': '/hr/employees',
  '/idp/kpis': '/idp/hierarchy',
  '/idp/overview': '/idp/plans',
  '/idp/projects': '/idp/hierarchy',
  '/idp/strategic-objectives': '/idp/hierarchy',
  '/reports/idp-summary': '/idp/reports',
  '/workflow/my-drafts': '/workflow/my-queue',
  '/workflow/pending-submission': '/workflow/my-queue',
  '/workflow/returned-submissions': '/workflow/my-queue',
  '/workflow/under-verification': '/workflow/my-queue',
  '/workflow/under-review': '/workflow/my-queue',
  '/workflow/under-approval': '/workflow/my-queue',
  '/workflow/internal-audit-returned': '/workflow/my-queue',
  '/workflow/approved-closed': '/workflow/my-queue',
};

export function canonicalNavigationPath(path: string) {
  const normalized = normalizeRoutePath(path);
  const alias = Object.keys(canonicalRouteByAlias)
    .filter(candidate => normalized === candidate || normalized.startsWith(`${candidate}/`))
    .sort((left, right) => right.length - left.length)[0];
  return alias ? `${canonicalRouteByAlias[alias]}${normalized.slice(alias.length)}` : normalized;
}
