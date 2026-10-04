import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TenantOrganizationAdministration } from './TenantOrganizationAdministration';

const api = vi.hoisted(() => ({
  getDepartmentMastersPage: vi.fn(), getUnitMastersPage: vi.fn(), getPositionMastersPage: vi.fn(), getWardMastersPage: vi.fn(), getVoteNumberMastersPage: vi.fn(),
  saveDepartmentMaster: vi.fn(), saveUnitMaster: vi.fn(), savePositionMaster: vi.fn(),
}));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canCreate: () => true, canUpdate: () => true }) }));

describe('TenantOrganizationAdministration', () => {
  beforeEach(() => {
    api.getPositionMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'position-1', departmentPublicId: 'department-1', departmentName: 'Finance', unitPublicId: 'unit-1', unitName: 'Budget', code: 'CFO', name: 'Chief Financial Officer', grade: 'T20', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'Aw==' }], page: 1, pageSize: 25, totalCount: 31, totalPages: 2 } });
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'department-1', code: 'FIN', name: 'Finance', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getUnitMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'unit-1', departmentPublicId: 'department-1', departmentName: 'Finance', code: 'BUD', name: 'Budget', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'Ag==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.savePositionMaster.mockResolvedValue({ success: true, data: {} });
  });

  it('edits a governed position using public relationships, reason, and RowVersion', async () => {
    render(<TenantOrganizationAdministration kind="positions" />);
    fireEvent.click(await screen.findByRole('button', { name: /Chief Financial Officer/ }));
    const reason = screen.getAllByRole('textbox').find(element => element.tagName === 'TEXTAREA');
    expect(reason).toBeDefined();
    fireEvent.change(reason!, { target: { value: 'Council approved establishment change' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    await waitFor(() => expect(api.savePositionMaster).toHaveBeenCalledWith('position-1', expect.objectContaining({
      departmentPublicId: 'department-1', unitPublicId: 'unit-1', code: 'CFO', rowVersion: 'Aw==', reason: 'Council approved establishment change',
    })));
  });

  it('searches and pages the authoritative organization register', async () => {
    render(<TenantOrganizationAdministration kind="positions" />);
    expect(await screen.findByText('31')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Search positions'), { target: { value: 'chief' } });
    await waitFor(() => expect(api.getPositionMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'chief', page: 1, pageSize: 25, sortBy: 'name' })));
    const next = screen.getAllByRole('button', { name: 'Next' }).find(button => !(button as HTMLButtonElement).disabled);
    expect(next).toBeDefined();
    fireEvent.click(next!);
    await waitFor(() => expect(api.getPositionMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'chief', page: 2 })));
  });
});
