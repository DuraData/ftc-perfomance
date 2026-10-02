import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { NavigationRegistryEditor } from './NavigationRegistryEditor';

const api = vi.hoisted(() => ({
  getSecurityNavigationRegistry: vi.fn(),
  createSecurityNavigationItem: vi.fn(),
  updateSecurityNavigationItem: vi.fn(),
}));
vi.mock('../../api/api', () => api);

describe('NavigationRegistryEditor', () => {
  beforeEach(() => {
    api.getSecurityNavigationRegistry.mockResolvedValue({ success: true, data: [
      { publicId: 'root-1', code: 'NAV.REPORTS', name: 'Reports', displayOrder: 1, isActive: true, rowVersion: 'AQ==' },
      { publicId: 'child-1', parentPublicId: 'root-1', code: 'NAV.REPORTS.OPMS', name: 'OPMS report', route: '/reports', displayOrder: 1, requiredPermissionCode: 'NAV.REPORTS.OPMS', isActive: true, rowVersion: 'Ag==' },
    ] });
  });

  it('renders the hierarchy and submits audited optimistic updates', async () => {
    api.updateSecurityNavigationItem.mockResolvedValue({ success: true, data: { publicId: 'child-1', parentPublicId: 'root-1', code: 'NAV.REPORTS.OPMS', name: 'Performance reports', route: '/reports', displayOrder: 1, requiredPermissionCode: 'NAV.REPORTS.OPMS', isActive: true, rowVersion: 'Aw==' } });
    render(<NavigationRegistryEditor permissions={[{ code: 'NAV.REPORTS.OPMS', kind: 'Navigation' }]} />);

    fireEvent.click(await screen.findByRole('button', { name: /OPMS report/ }));
    fireEvent.change(screen.getByLabelText('Navigation name'), { target: { value: 'Performance reports' } });
    fireEvent.change(screen.getByLabelText('Navigation audit reason'), { target: { value: 'Clarify report navigation label' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save navigation item' }));

    await waitFor(() => expect(api.updateSecurityNavigationItem).toHaveBeenCalledWith(expect.objectContaining({ publicId: 'child-1', rowVersion: 'Ag==' }), expect.objectContaining({ name: 'Performance reports', reason: 'Clarify report navigation label' })));
  });
});
