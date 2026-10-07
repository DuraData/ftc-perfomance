import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { PermissionSimulationPage, RoleAccessMatrixPage, RolePermissionCrudAuditPage } from './AccessGovernancePages';

const api = vi.hoisted(() => ({
  getPermissionsPage: vi.fn(),
  getRoleAccessMatrixPage: vi.fn(),
  getSystemCoverageAudit: vi.fn(),
  getUsersPage: vi.fn(),
  simulateAccess: vi.fn(),
}));

vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../common/OrganizationMasterPicker', () => ({ OrganizationMasterPicker: ({ label }: { label: string }) => <label>{label}<select /></label> }));

describe('PermissionSimulationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getPermissionsPage.mockImplementation(async ({ page = 1, search = '' }) => ({
      success: true,
      data: {
        items: [{ id: page, module: 'Security', feature: 'Simulation', action: 'Read', code: `SIMULATION.PAGE_${page}`, isActive: true }],
        page,
        pageSize: 25,
        totalCount: search ? 1 : 26,
        totalPages: search ? 1 : 2,
      },
    }));
    api.getRoleAccessMatrixPage.mockImplementation(async ({ page = 1, search = '' }) => ({
      success: true,
      data: {
        items: [{ role: `Role ${page}`, permissions: ['OPMS_KPI.READ'], scope: ['Municipality:7'], menus: ['OPMS'], allowedActions: ['OPMS KPI Read'], reports: [], testUser: `User ${page}` }],
        page,
        pageSize: 25,
        totalCount: search ? 1 : 26,
        totalPages: search ? 1 : 2,
      },
    }));
    api.getSystemCoverageAudit.mockResolvedValue({ success: true, data: [] });
    api.getUsersPage.mockImplementation(async ({ page = 1, search = '' }) => ({
      success: true,
      data: {
        items: [{ user: { publicId: `user-public-${page}`, userName: `user-${page}`, firstName: 'Governed', lastName: `User ${page}`, fullName: `Governed User ${page}`, email: `user${page}@example.test`, isActive: true, mustChangePassword: false }, roles: [{ publicId: 'role-public', name: 'Reviewer', isSystemRole: false, isActive: true }] }],
        page,
        pageSize: 25,
        totalCount: search ? 1 : 26,
        totalPages: search ? 1 : 2,
      },
    }));
  });

  it('searches and pages the tenant user directory without losing the selected simulation subject', async () => {
    render(<PermissionSimulationPage />);

    await waitFor(() => expect(api.getUsersPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }));
    fireEvent.change(screen.getByLabelText('User'), { target: { value: 'user-public-1' } });
    expect(screen.getByText('Page 1 of 2 · 26 users')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Next users' }));
    await waitFor(() => expect(api.getUsersPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }));
    expect(screen.getByLabelText('User')).toHaveValue('user-public-1');
    expect(screen.getByRole('option', { name: 'Governed User 1 (Reviewer)' })).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search users'), { target: { value: 'specific user' } });
    await waitFor(() => expect(api.getUsersPage).toHaveBeenLastCalledWith({ page: 1, pageSize: 25, search: 'specific user', sortBy: 'name', sortDirection: 'asc' }));

    await waitFor(() => expect(api.getPermissionsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'code', sortDirection: 'asc' }));
    fireEvent.change(screen.getByLabelText('Permission'), { target: { value: 'SIMULATION.PAGE_1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Next permissions' }));
    await waitFor(() => expect(api.getPermissionsPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, search: '', sortBy: 'code', sortDirection: 'asc' }));
    expect(screen.getByLabelText('Permission')).toHaveValue('SIMULATION.PAGE_1');
    expect(screen.getByRole('option', { name: 'SIMULATION.PAGE_1' })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Search permissions'), { target: { value: 'submission' } });
    await waitFor(() => expect(api.getPermissionsPage).toHaveBeenLastCalledWith({ page: 1, pageSize: 25, search: 'submission', sortBy: 'code', sortDirection: 'asc' }));
  });

  it('searches and pages the dynamic role access matrix', async () => {
    render(<RoleAccessMatrixPage />);

    expect(await screen.findByText('26 roles · Page 1 of 2')).toBeInTheDocument();
    await waitFor(() => expect(api.getRoleAccessMatrixPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }));
    fireEvent.click(screen.getByRole('button', { name: 'Next roles' }));
    await waitFor(() => expect(api.getRoleAccessMatrixPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }));
    fireEvent.change(screen.getByLabelText('Search role access matrix'), { target: { value: 'reviewer' } });
    await waitFor(() => expect(api.getRoleAccessMatrixPage).toHaveBeenLastCalledWith({ page: 1, pageSize: 25, search: 'reviewer', sortBy: 'name', sortDirection: 'asc' }));
  });

  it('uses the same bounded role page for the merged CRUD audit', async () => {
    render(<RolePermissionCrudAuditPage />);

    expect(await screen.findByText('26 roles · Page 1 of 2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next CRUD roles' }));
    await waitFor(() => expect(api.getRoleAccessMatrixPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }));
    fireEvent.change(screen.getByLabelText('Search role CRUD audit'), { target: { value: 'submitter' } });
    await waitFor(() => expect(api.getRoleAccessMatrixPage).toHaveBeenLastCalledWith({ page: 1, pageSize: 25, search: 'submitter', sortBy: 'name', sortDirection: 'asc' }));
  });
});
