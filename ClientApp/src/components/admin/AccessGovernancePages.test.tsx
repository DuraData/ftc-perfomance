import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { PermissionSimulationPage } from './AccessGovernancePages';

const api = vi.hoisted(() => ({
  getPermissions: vi.fn(),
  getRoleAccessMatrix: vi.fn(),
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
    api.getPermissions.mockResolvedValue({ success: true, data: [] });
    api.getUsersPage.mockImplementation(async ({ page = 1, search = '' }) => ({
      success: true,
      data: {
        items: [{ user: { id: `user-${page}`, publicId: `user-public-${page}`, userName: `user-${page}`, firstName: 'Governed', lastName: `User ${page}`, fullName: `Governed User ${page}`, email: `user${page}@example.test`, isActive: true, mustChangePassword: false }, roles: [{ id: 'role-1', name: 'Reviewer', isSystemRole: false, isActive: true }] }],
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
    fireEvent.change(screen.getByLabelText('User'), { target: { value: 'user-1' } });
    expect(screen.getByText('Page 1 of 2 · 26 users')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Next users' }));
    await waitFor(() => expect(api.getUsersPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }));
    expect(screen.getByLabelText('User')).toHaveValue('user-1');
    expect(screen.getByRole('option', { name: 'Governed User 1 (Reviewer)' })).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search users'), { target: { value: 'specific user' } });
    await waitFor(() => expect(api.getUsersPage).toHaveBeenLastCalledWith({ page: 1, pageSize: 25, search: 'specific user', sortBy: 'name', sortDirection: 'asc' }));
  });
});
