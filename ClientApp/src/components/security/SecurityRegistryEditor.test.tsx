import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { SecurityRegistryEditor } from './SecurityRegistryEditor';

const api = vi.hoisted(() => ({
  getSecurityResourcesPage: vi.fn(), getSecurityActionsPage: vi.fn(), getSecurityMembersPage: vi.fn(),
  createSecurityResource: vi.fn(), updateSecurityResource: vi.fn(), createSecurityAction: vi.fn(),
  updateSecurityAction: vi.fn(), updateSecurityMember: vi.fn(),
}));
vi.mock('../../api/api', () => api);

describe('SecurityRegistryEditor', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getSecurityResourcesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getSecurityActionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getSecurityMembersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.createSecurityResource.mockResolvedValue({ success: true, data: { publicId: 'resource-1', code: 'CASE_FILE', name: 'Case File', type: 'ENTITY', canCreate: false, canRead: true, canUpdate: false, canDelete: false, canExport: false, canImport: false, supportsMembers: false, supportsCriteria: false, isActive: true, rowVersion: 'AQ==' } });
  });

  it('creates a governed resource definition with an audit reason', async () => {
    render(<SecurityRegistryEditor />);
    await screen.findByRole('combobox', { name: 'Registered resources' });
    expect(api.getSecurityResourcesPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'code', sortDirection: 'asc' });
    fireEvent.change(screen.getByLabelText('Resource code'), { target: { value: 'case_file' } });
    fireEvent.change(screen.getByLabelText('Resource name'), { target: { value: 'Case File' } });
    fireEvent.change(screen.getByLabelText('Registry audit reason'), { target: { value: 'Register case files' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save registry definition' }));

    await waitFor(() => expect(api.createSecurityResource).toHaveBeenCalledWith(expect.objectContaining({
      code: 'CASE_FILE', name: 'Case File', type: 'ENTITY', canRead: true, reason: 'Register case files',
    })));
  });

  it('edits only an approved member definition', async () => {
    const member = { publicId: 'member-1', resourceCode: 'EMPLOYEE', memberCode: 'SalaryReference', displayName: 'Salary Reference', isSensitive: true, isSystemManaged: false, isActive: true, rowVersion: 'AQ==' };
    api.getSecurityMembersPage.mockResolvedValue({ success: true, data: { items: [member], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.updateSecurityMember.mockResolvedValue({ success: true, data: member });
    render(<SecurityRegistryEditor />);
    fireEvent.click(await screen.findByRole('tab', { name: 'members' }));
    fireEvent.change(screen.getByLabelText('Approved member'), { target: { value: 'member-1' } });
    fireEvent.change(screen.getByLabelText('Display name'), { target: { value: 'Protected Salary Reference' } });
    fireEvent.change(screen.getByLabelText('Registry audit reason'), { target: { value: 'Clarify protected field' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save registry definition' }));

    await waitFor(() => expect(api.updateSecurityMember).toHaveBeenCalledWith(member, expect.objectContaining({
      displayName: 'Protected Salary Reference', isSensitive: true, reason: 'Clarify protected field',
    })));
    expect(screen.queryByRole('button', { name: /create member/i })).not.toBeInTheDocument();
  });
});
