import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { RoleImplementationAuditPage } from './RoleImplementationAuditPage';

const api = vi.hoisted(() => ({
  getRoleImplementationAuditPage: vi.fn(),
}));

vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));

describe('RoleImplementationAuditPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getRoleImplementationAuditPage.mockImplementation(async ({ page = 1, search = '' }) => ({
      success: true,
      data: {
        items: [{
          rolePublicId: `runtime-role-${page}`,
          roleCode: `RUNTIME_${page}`,
          role: `Runtime Role ${page}`,
          dashboard: true,
          menus: true,
          crud: false,
          scopeFiltering: true,
          notifications: false,
          reports: true,
          auditTrail: false,
          allowedPermissionCount: 3,
          deniedPermissionCount: 1,
          activeAssignmentCount: 1,
          complete: true,
        }],
        page,
        pageSize: 25,
        totalCount: search ? 1 : 26,
        totalPages: search ? 1 : 2,
      },
    }));
  });

  it('searches and pages persisted roles without relying on compiled role names', async () => {
    render(<RoleImplementationAuditPage />);

    expect(await screen.findByText('Runtime Role 1')).toBeInTheDocument();
    expect(screen.getByText('RUNTIME_1')).toBeInTheDocument();
    expect(screen.getByText('26 roles · Page 1 of 2')).toBeInTheDocument();
    await waitFor(() => expect(api.getRoleImplementationAuditPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }));

    fireEvent.click(screen.getByRole('button', { name: 'Next roles' }));
    await waitFor(() => expect(api.getRoleImplementationAuditPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }));

    fireEvent.change(screen.getByLabelText('Search role implementation audit'), { target: { value: 'municipal viewer' } });
    await waitFor(() => expect(api.getRoleImplementationAuditPage).toHaveBeenLastCalledWith({ page: 1, pageSize: 25, search: 'municipal viewer', sortBy: 'name', sortDirection: 'asc' }));
  });
});
