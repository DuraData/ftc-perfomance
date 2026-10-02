import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { SecurityAdministrationPage } from './SecurityAdministration';

const api = vi.hoisted(() => ({
  getSecurityRoles: vi.fn(), getSecurityUsers: vi.fn(), getSecurityPermissionDefinitions: vi.fn(),
  getDepartments: vi.fn(), getUnits: vi.fn(), getRoleSecurityConfiguration: vi.fn(),
  getSecurityUserRoles: vi.fn(), saveSecurityUserRoles: vi.fn(), getEffectiveSecurityPreview: vi.fn(),
  createSecurityRole: vi.fn(), saveRoleSecurityConfiguration: vi.fn(), updateSecurityRole: vi.fn(),
}));
vi.mock('../../api/api', () => api);
vi.mock('./NavigationRegistryEditor', () => ({ NavigationRegistryEditor: () => <div>Navigation editor</div> }));

describe('SecurityAdministrationPage role assignments', () => {
  beforeEach(() => {
    api.getSecurityRoles.mockResolvedValue({ success: true, data: [{ id: 'role-1', publicId: 'role-public', roleCode: 'DEPARTMENT_REVIEWER', name: 'Department Reviewer', municipalityId: 7, isSystemRole: false, isActive: true, effectiveFrom: '2026-01-01T00:00:00Z', rowVersion: 'AQ==' }] });
    api.getSecurityUsers.mockResolvedValue({ success: true, data: [{ id: 'user-1', fullName: 'Review User', email: 'review@example.test' }] });
    api.getSecurityPermissionDefinitions.mockResolvedValue({ success: true, data: [] });
    api.getDepartments.mockResolvedValue({ success: true, data: [{ id: 10, publicId: 'dep-1', code: 'FIN', name: 'Finance' }] });
    api.getUnits.mockResolvedValue({ success: true, data: [{ id: 20, publicId: 'unit-1', departmentId: 10, departmentName: 'Finance', code: 'REV', name: 'Revenue' }] });
    api.getRoleSecurityConfiguration.mockResolvedValue({ success: true, data: { roleId: 'role-1', publicId: 'role-public', name: 'Department Reviewer', roleRowVersion: 'AQ==', permissions: [] } });
    api.getSecurityUserRoles.mockResolvedValue({ success: true, data: { userId: 'user-1', userName: 'Review User', assignments: [] } });
    api.saveSecurityUserRoles.mockResolvedValue({ success: true, data: true });
  });

  it('authors department, unit, and effective dates instead of role ids alone', async () => {
    render(<SecurityAdministrationPage />);
    const userSelect = (await screen.findAllByRole('combobox'))[1];
    fireEvent.change(userSelect, { target: { value: 'user-1' } });
    fireEvent.click(await screen.findByRole('checkbox', { name: 'Department Reviewer' }));
    fireEvent.change(screen.getByLabelText('Department Reviewer department'), { target: { value: '10' } });
    fireEvent.change(screen.getByLabelText('Department Reviewer unit'), { target: { value: '20' } });
    fireEvent.change(screen.getByLabelText('Department Reviewer effective from'), { target: { value: '2026-10-01T08:00' } });
    fireEvent.change(screen.getByLabelText('Department Reviewer effective to'), { target: { value: '2026-12-31T17:00' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save user roles' }));

    await waitFor(() => expect(api.saveSecurityUserRoles).toHaveBeenCalledWith('user-1', expect.anything(), [expect.objectContaining({ roleId: 'role-1', municipalityId: 7, departmentId: 10, unitId: 20, effectiveFrom: expect.any(String), effectiveTo: expect.any(String) })]));
  });
});
