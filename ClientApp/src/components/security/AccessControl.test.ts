import { canAccessPath, hasPermissionCode } from './AccessControl';

describe('AccessControl helpers', () => {
  it('does not allow a role-name flag to bypass permissions', () => {
    expect(canAccessPath('/opms/targets', ['anything'], true)).toBe(false);
    expect(canAccessPath('/system-administration/users', [], true)).toBe(false);
  });

  it('requires a registered dashboard permission', () => {
    expect(canAccessPath('/dashboard', [], false)).toBe(false);
    expect(canAccessPath('/dashboard', ['NAV.DASHBOARD'], false)).toBe(true);
  });

  it('denies unknown routes by default', () => {
    expect(canAccessPath('/unregistered-feature', ['anything'], false)).toBe(false);
  });

  it('denies access when required permissions are missing', () => {
    expect(canAccessPath('/opms/targets', ['Some.Other.Permission'], false)).toBe(false);
    expect(canAccessPath('/workflow/verification', ['Some.Other.Permission'], false)).toBe(false);
  });

  it('allows access when required permission is present', () => {
    expect(canAccessPath('/opms/targets', ['OPMS_KPI.READ'], false)).toBe(true);
    expect(canAccessPath('/workflow/verification', ['Workflow.Verify.View'], false)).toBe(true);
  });

  it('protects workflow governance with its stable action permission', () => {
    expect(canAccessPath('/admin/approval-setup', [], false)).toBe(false);
    expect(canAccessPath('/admin/approval-setup', ['WORKFLOW.CONFIGURE'], false)).toBe(true);
  });

  it('protects the TID workspace with dynamic resource or navigation permission', () => {
    expect(canAccessPath('/opms/tids', [], false)).toBe(false);
    expect(canAccessPath('/opms/tids', ['TID.READ'], false)).toBe(true);
    expect(canAccessPath('/opms/tids', ['NAV.SDBIP.TIDS'], false)).toBe(true);
  });

  it('allows reporting routes through stable report permissions', () => {
    expect(canAccessPath('/reports', [], false)).toBe(false);
    expect(canAccessPath('/reports', ['OPMS_REPORT.READ'], false)).toBe(true);
    expect(canAccessPath('/reports/performance', ['IPMS_REPORT.EXPORT'], false)).toBe(true);
  });

  it('allows access when permission matches case-insensitively', () => {
    expect(hasPermissionCode(['OPMS.View'], ['opms.view'])).toBe(true);
    expect(hasPermissionCode(['Workflow.Submit.View'], ['WORKFLOW.SUBMIT.VIEW'])).toBe(true);
  });

  it('allows access when one of the required permissions is granted', () => {
    expect(hasPermissionCode(['OPMS.View', 'Targets.View'], ['targets.view'])).toBe(true);
  });
});
