import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { SecurityAdministrationPage } from './SecurityAdministration';

const api = vi.hoisted(() => ({
  getSecurityRolesPage: vi.fn(), getSecurityUsersPage: vi.fn(), getSecurityPermissionDefinitionsPage: vi.fn(),
  getDepartmentMastersPage: vi.fn(), getUnitMastersPage: vi.fn(), getPositionMastersPage: vi.fn(), getWardMastersPage: vi.fn(), getVoteNumberMastersPage: vi.fn(), getRoleSecurityConfiguration: vi.fn(),
  getSecurityUserRoles: vi.fn(), saveSecurityUserRoles: vi.fn(), getEffectiveSecurityPreview: vi.fn(),
  createSecurityRole: vi.fn(), saveRoleSecurityConfiguration: vi.fn(), updateSecurityRole: vi.fn(),
}));
vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ currentMunicipalityId: 7, tenantContexts: [{ id: 7, publicId: 'municipality-public' }] }) }));
vi.mock('./NavigationRegistryEditor', () => ({ NavigationRegistryEditor: () => <div>Navigation editor</div> }));
vi.mock('./SecurityRegistryEditor', () => ({ SecurityRegistryEditor: () => <div>Security registry editor</div> }));

describe('SecurityAdministrationPage role assignments', () => {
  beforeEach(() => {
    api.getSecurityRolesPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'role-public', roleCode: 'DEPARTMENT_REVIEWER', name: 'Department Reviewer', municipalityPublicId: 'municipality-public', isSystemRole: false, isActive: true, effectiveFrom: '2026-01-01T00:00:00Z', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getSecurityUsersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'user-public', fullName: 'Review User', email: 'review@example.test' }], page: 1, pageSize: 100, totalCount: 1, totalPages: 1 } });
    api.getSecurityPermissionDefinitionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'dep-1', code: 'FIN', name: 'Finance', isActive: true }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getUnitMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'unit-1', departmentPublicId: 'dep-1', departmentName: 'Finance', code: 'REV', name: 'Revenue', isActive: true }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getPositionMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getWardMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getVoteNumberMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getRoleSecurityConfiguration.mockResolvedValue({ success: true, data: { rolePublicId: 'role-public', name: 'Department Reviewer', roleRowVersion: 'AQ==', permissions: [] } });
    api.getSecurityUserRoles.mockResolvedValue({ success: true, data: { userPublicId: 'user-public', userName: 'Review User', assignments: [] } });
    api.saveSecurityUserRoles.mockResolvedValue({ success: true, data: true });
  });

  it('authors department, unit, and effective dates instead of role ids alone', async () => {
    render(<SecurityAdministrationPage />);
    await waitFor(() => expect(api.getSecurityRolesPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }, true));
    await waitFor(() => expect(api.getSecurityPermissionDefinitionsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'code', sortDirection: 'asc' }, ['Resource']));
    const userSelect = (await screen.findAllByRole('combobox'))[1];
    fireEvent.change(userSelect, { target: { value: 'user-public' } });
    fireEvent.click(await screen.findByRole('checkbox', { name: 'Department Reviewer' }));
    fireEvent.change(await screen.findByLabelText('Department Reviewer department'), { target: { value: 'dep-1' } });
    await waitFor(() => expect(api.getUnitMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ departmentPublicId: 'dep-1' })));
    fireEvent.change(screen.getByLabelText('Department Reviewer unit'), { target: { value: 'unit-1' } });
    fireEvent.change(screen.getByLabelText('Department Reviewer effective from'), { target: { value: '2026-10-01T08:00' } });
    fireEvent.change(screen.getByLabelText('Department Reviewer effective to'), { target: { value: '2026-12-31T17:00' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save user roles' }));

    await waitFor(() => expect(api.saveSecurityUserRoles).toHaveBeenCalledWith('user-public', expect.anything(), [expect.objectContaining({ rolePublicId: 'role-public', municipalityPublicId: 'municipality-public', departmentPublicId: 'dep-1', unitPublicId: 'unit-1', effectiveFrom: expect.any(String), effectiveTo: expect.any(String) })]));
  });

  it('preserves role rules while permission-definition pages change', async () => {
    api.getSecurityPermissionDefinitionsPage.mockImplementation(({ page }: { page: number }) => Promise.resolve({ success: true, data: {
      items: page === 1
        ? [{ code: 'CASE.READ', kind: 'Resource', resourceCode: 'CASE', operation: 'Read' }]
        : [{ code: 'CASE.UPDATE', kind: 'Resource', resourceCode: 'CASE', operation: 'Update' }],
      page, pageSize: 25, totalCount: 2, totalPages: 2,
    } }));
    api.getRoleSecurityConfiguration.mockResolvedValue({ success: true, data: { rolePublicId: 'role-public', name: 'Department Reviewer', roleRowVersion: 'AQ==', permissions: [{ permissionCode: 'AUDIT.READ', kind: 'Resource', resourceCode: 'AUDIT', state: 'ALLOW', scopeType: 'InstitutionScope', rowVersion: 'Ag==' }] } });
    api.saveRoleSecurityConfiguration.mockResolvedValue({ success: true, data: true });

    render(<SecurityAdministrationPage />);
    fireEvent.change(await screen.findByLabelText('CASE.READ decision'), { target: { value: 'ALLOW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Next permissions' }));
    fireEvent.change(await screen.findByLabelText('CASE.UPDATE decision'), { target: { value: 'DENY' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save role security' }));

    await waitFor(() => expect(api.saveRoleSecurityConfiguration).toHaveBeenCalledWith('role-public', 'AQ==', expect.arrayContaining([
      expect.objectContaining({ permissionCode: 'AUDIT.READ', state: 'ALLOW', scopeType: 'InstitutionScope' }),
      expect.objectContaining({ permissionCode: 'CASE.READ', state: 'ALLOW' }),
      expect.objectContaining({ permissionCode: 'CASE.UPDATE', state: 'DENY' }),
    ])));
  });
});
