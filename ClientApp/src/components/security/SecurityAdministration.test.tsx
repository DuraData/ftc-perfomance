import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { SecurityAdministrationPage } from './SecurityAdministration';

const api = vi.hoisted(() => ({
  getSecurityRoles: vi.fn(), getSecurityUsersPage: vi.fn(), getSecurityPermissionDefinitions: vi.fn(),
  getDepartmentMastersPage: vi.fn(), getUnitMastersPage: vi.fn(), getPositionMastersPage: vi.fn(), getWardMastersPage: vi.fn(), getVoteNumberMastersPage: vi.fn(), getRoleSecurityConfiguration: vi.fn(),
  getSecurityUserRoles: vi.fn(), saveSecurityUserRoles: vi.fn(), getEffectiveSecurityPreview: vi.fn(),
  createSecurityRole: vi.fn(), saveRoleSecurityConfiguration: vi.fn(), updateSecurityRole: vi.fn(),
}));
vi.mock('../../api/api', () => api);
vi.mock('./NavigationRegistryEditor', () => ({ NavigationRegistryEditor: () => <div>Navigation editor</div> }));

describe('SecurityAdministrationPage role assignments', () => {
  beforeEach(() => {
    api.getSecurityRoles.mockResolvedValue({ success: true, data: [{ id: 'role-1', publicId: 'role-public', roleCode: 'DEPARTMENT_REVIEWER', name: 'Department Reviewer', municipalityId: 7, isSystemRole: false, isActive: true, effectiveFrom: '2026-01-01T00:00:00Z', rowVersion: 'AQ==' }] });
    api.getSecurityUsersPage.mockResolvedValue({ success: true, data: { items: [{ id: 'user-1', fullName: 'Review User', email: 'review@example.test' }], page: 1, pageSize: 100, totalCount: 1, totalPages: 1 } });
    api.getSecurityPermissionDefinitions.mockResolvedValue({ success: true, data: [] });
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'dep-1', code: 'FIN', name: 'Finance', isActive: true }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getUnitMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'unit-1', departmentPublicId: 'dep-1', departmentName: 'Finance', code: 'REV', name: 'Revenue', isActive: true }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getPositionMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getWardMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getVoteNumberMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getRoleSecurityConfiguration.mockResolvedValue({ success: true, data: { roleId: 'role-1', publicId: 'role-public', name: 'Department Reviewer', roleRowVersion: 'AQ==', permissions: [] } });
    api.getSecurityUserRoles.mockResolvedValue({ success: true, data: { userId: 'user-1', userName: 'Review User', assignments: [] } });
    api.saveSecurityUserRoles.mockResolvedValue({ success: true, data: true });
  });

  it('authors department, unit, and effective dates instead of role ids alone', async () => {
    render(<SecurityAdministrationPage />);
    const userSelect = (await screen.findAllByRole('combobox'))[1];
    fireEvent.change(userSelect, { target: { value: 'user-1' } });
    fireEvent.click(await screen.findByRole('checkbox', { name: 'Department Reviewer' }));
    fireEvent.change(await screen.findByLabelText('Department Reviewer department'), { target: { value: 'dep-1' } });
    await waitFor(() => expect(api.getUnitMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ departmentPublicId: 'dep-1' })));
    fireEvent.change(screen.getByLabelText('Department Reviewer unit'), { target: { value: 'unit-1' } });
    fireEvent.change(screen.getByLabelText('Department Reviewer effective from'), { target: { value: '2026-10-01T08:00' } });
    fireEvent.change(screen.getByLabelText('Department Reviewer effective to'), { target: { value: '2026-12-31T17:00' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save user roles' }));

    await waitFor(() => expect(api.saveSecurityUserRoles).toHaveBeenCalledWith('user-1', expect.anything(), [expect.objectContaining({ roleId: 'role-1', municipalityId: 7, departmentPublicId: 'dep-1', unitPublicId: 'unit-1', effectiveFrom: expect.any(String), effectiveTo: expect.any(String) })]));
  });
});
