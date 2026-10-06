import { render, screen } from '@testing-library/react';
import { AdminUsersPage } from './SystemAdmin';

const api = vi.hoisted(() => ({
  getUsersPage: vi.fn(),
  getSecurityRolesPage: vi.fn(),
  getPermissionsPage: vi.fn(),
}));

vi.mock('../../api/api', async importOriginal => ({
  ...(await importOriginal<typeof import('../../api/api')>()),
  ...api,
}));
vi.mock('../../context/AppContext', () => ({
  useApp: () => ({ permissions: [], pushToast: vi.fn() }),
}));
vi.mock('../../context/SecurityContext', () => ({
  useSecurity: () => ({
    canCreate: () => false,
    canUpdate: () => false,
    canDelete: () => false,
    canExecute: () => false,
    canReadField: () => false,
    canEditField: () => false,
  }),
}));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));

describe('User administration member permissions', () => {
  it('does not render denied email or phone values and hides mutation controls', async () => {
    api.getUsersPage.mockResolvedValue({
      success: true,
      data: { items: [{
        user: {
          id: 'user-1', publicId: '11111111-1111-1111-1111-111111111111', userName: 'user-1',
          firstName: 'Protected', lastName: 'User', fullName: 'Protected User', email: null,
          phoneNumber: null, isActive: true, mustChangePassword: false,
        },
        roles: [],
      }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    api.getSecurityRolesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getPermissionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });

    render(<AdminUsersPage />);

    expect(await screen.findByText('Protected User')).toBeInTheDocument();
    expect(api.getUsersPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' });
    expect(api.getSecurityRolesPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' });
    expect(api.getPermissionsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'code', sortDirection: 'asc' });
    expect(screen.queryByText('private@example.test')).not.toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: 'Email' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add User' })).toBeDisabled();
    expect(screen.queryByRole('button', { name: 'Edit user' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Delete user' })).not.toBeInTheDocument();
  });
});
