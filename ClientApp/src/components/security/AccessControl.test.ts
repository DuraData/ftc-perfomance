import { canAccessPath, hasPermissionCode } from './AccessControl';

describe('AccessControl helpers', () => {
  it('returns true for super admin regardless of route', () => {
    expect(canAccessPath('/opms/targets', ['anything'], true)).toBe(true);
    expect(canAccessPath('/system-administration/users', [], true)).toBe(true);
  });

  it('returns true for dashboard without permissions', () => {
    expect(canAccessPath('/dashboard', [], false)).toBe(true);
  });

  it('denies access when required permissions are missing', () => {
    expect(canAccessPath('/opms/targets', ['Some.Other.Permission'], false)).toBe(false);
    expect(canAccessPath('/workflow/verification', ['Some.Other.Permission'], false)).toBe(false);
  });

  it('allows access when required permission is present', () => {
    expect(canAccessPath('/opms/targets', ['OPMS.View'], false)).toBe(true);
    expect(canAccessPath('/workflow/verification', ['Workflow.Verify.View'], false)).toBe(true);
  });

  it('allows access when permission matches case-insensitively', () => {
    expect(hasPermissionCode(['OPMS.View'], ['opms.view'])).toBe(true);
    expect(hasPermissionCode(['Workflow.Submit.View'], ['WORKFLOW.SUBMIT.VIEW'])).toBe(true);
  });

  it('allows access when one of the required permissions is granted', () => {
    expect(hasPermissionCode(['OPMS.View', 'Targets.View'], ['targets.view'])).toBe(true);
  });
});
