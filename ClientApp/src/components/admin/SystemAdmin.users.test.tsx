import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { AdminUsersPage } from './SystemAdmin';

const api = vi.hoisted(() => ({
  getUsersPage: vi.fn(),
  getSecurityRolesPage: vi.fn(),
  getSecurityUserRoles: vi.fn(),
  getEffectiveSecurityPreview: vi.fn(),
  activateUser: vi.fn(),
  deactivateUser: vi.fn(),
  deleteUser: vi.fn(),
  createUser: vi.fn(),
  updateUser: vi.fn(),
  saveSecurityUserRoles: vi.fn(),
}));
const security = vi.hoisted(() => ({ enabled: false }));

vi.mock('../../api/api', async importOriginal => ({
  ...(await importOriginal<typeof import('../../api/api')>()),
  ...api,
}));
vi.mock('../../context/AppContext', () => ({
  useApp: () => ({ permissions: [], pushToast: vi.fn(), currentMunicipalityPublicId: 'municipality-public' }),
}));
vi.mock('../../context/SecurityContext', () => ({
  useSecurity: () => ({
    canCreate: () => security.enabled,
    canUpdate: () => security.enabled,
    canDelete: () => security.enabled,
    canExecute: (code: string) => security.enabled || code === 'SECURITY.VIEW_EFFECTIVE',
    canReadField: () => security.enabled,
    canEditField: () => security.enabled,
  }),
}));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));

describe('User administration member permissions', () => {
  afterEach(() => { security.enabled = false; vi.clearAllMocks(); });

  it('does not render denied email or phone values and hides mutation controls', async () => {
    api.getUsersPage.mockResolvedValue({
      success: true,
      data: { items: [{
        user: {
          id: 'user-1', publicId: '11111111-1111-1111-1111-111111111111', rowVersion: 'AQID', userName: 'user-1',
          firstName: 'Protected', lastName: 'User', fullName: 'Protected User', email: null,
          phoneNumber: null, isActive: true, mustChangePassword: false,
        },
        roles: [],
      }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    api.getSecurityRolesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getEffectiveSecurityPreview.mockResolvedValue({
      success: true,
      data: { userPublicId: '11111111-1111-1111-1111-111111111111', roles: ['Reviewer'], scopes: ['Municipality:7'], permissions: ['USER.READ'], assignments: [] },
    });

    render(<AdminUsersPage />);

    expect(await screen.findByText('Protected User')).toBeInTheDocument();
    expect(api.getUsersPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' });
    expect(api.getSecurityRolesPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' });
    expect(screen.queryByText('private@example.test')).not.toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: 'Email' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add User' })).toBeDisabled();
    expect(screen.queryByRole('button', { name: 'Edit user' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Delete user' })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Permissions' }));
    await waitFor(() => expect(api.getEffectiveSecurityPreview).toHaveBeenCalledWith('11111111-1111-1111-1111-111111111111'));
    expect(await screen.findByText('USER.READ')).toBeInTheDocument();
    expect(screen.getByText(/direct user permission overrides are retired/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
  });

  it('requires and transports a governance reason with the current RowVersion for account suspension', async () => {
    security.enabled = true;
    api.getUsersPage.mockResolvedValue({
      success: true,
      data: { items: [{
        user: {
          publicId: '11111111-1111-1111-1111-111111111111', rowVersion: 'AQID', userName: 'governed@example.test',
          firstName: 'Governed', lastName: 'User', fullName: 'Governed User', email: 'governed@example.test',
          phoneNumber: '0123456789', isActive: true, mustChangePassword: false,
        },
        roles: [],
      }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    api.getSecurityRolesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.deactivateUser.mockResolvedValue({ success: true, data: true });

    render(<AdminUsersPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Deactivate' }));
    const reason = await screen.findByRole('textbox', { name: /Governance reason/i });
    const submit = screen.getByRole('button', { name: 'Disable' });
    expect(submit).toBeDisabled();
    fireEvent.change(reason, { target: { value: 'Approved temporary account suspension' } });
    expect(submit).toBeEnabled();
    fireEvent.click(submit);

    await waitFor(() => expect(api.deactivateUser).toHaveBeenCalledWith(
      '11111111-1111-1111-1111-111111111111', 'AQID', 'Approved temporary account suspension'));
  });

  it('marks managed-account fields so saved administrator credentials are not offered to the new user form', async () => {
    security.enabled = true;
    api.getUsersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getSecurityRolesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });

    render(<AdminUsersPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Add User' }));

    expect(screen.getByLabelText('First Name*')).toHaveAttribute('autocomplete', 'off');
    expect(screen.getByLabelText('Last Name*')).toHaveAttribute('autocomplete', 'off');
    expect(screen.getByLabelText('Email*')).toHaveAttribute('autocomplete', 'off');
    expect(screen.getByLabelText('Phone Number')).toHaveAttribute('autocomplete', 'off');
    expect(screen.getByLabelText('Phone Number')).toHaveAttribute('type', 'tel');
    expect(screen.getByLabelText('Password*')).toHaveAttribute('autocomplete', 'new-password');
  });

  it('scopes a new role assignment to the active municipality', async () => {
    security.enabled = true;
    api.getUsersPage.mockResolvedValue({
      success: true,
      data: { items: [{
        user: {
          publicId: '11111111-1111-1111-1111-111111111111', rowVersion: 'AQID', userName: 'governed@example.test',
          firstName: 'Governed', lastName: 'User', fullName: 'Governed User', email: 'governed@example.test',
          phoneNumber: null, isActive: true, mustChangePassword: false,
        },
        roles: [],
      }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    api.getSecurityRolesPage.mockResolvedValue({
      success: true,
      data: { items: [{ publicId: 'role-public', name: 'Reviewer', isSystemRole: true }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    api.getSecurityUserRoles.mockResolvedValue({
      success: true,
      data: { userPublicId: '11111111-1111-1111-1111-111111111111', userName: 'Governed User', assignments: [] },
    });
    api.saveSecurityUserRoles.mockResolvedValue({ success: true, data: true });

    render(<AdminUsersPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Roles' }));
    fireEvent.click(await screen.findByRole('checkbox', { name: 'Reviewer' }));
    fireEvent.change(screen.getByRole('textbox', { name: /Governance reason/i }), { target: { value: 'Approved tenant role assignment' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => expect(api.saveSecurityUserRoles).toHaveBeenCalledWith(
      '11111111-1111-1111-1111-111111111111',
      expect.anything(),
      [expect.objectContaining({
        rolePublicId: 'role-public',
        municipalityPublicId: 'municipality-public',
        effectiveFrom: expect.any(String),
      })],
      'Approved tenant role assignment'));
  });
});
