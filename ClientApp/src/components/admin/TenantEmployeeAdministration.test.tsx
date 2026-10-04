import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TenantEmployeeAdministration } from './TenantEmployeeAdministration';

const api = vi.hoisted(() => ({
  getMunicipalEmployeesPage: vi.fn(), getDepartmentMastersPage: vi.fn(), getUnitMastersPage: vi.fn(), getPositionMastersPage: vi.fn(), getWardMastersPage: vi.fn(), getVoteNumberMastersPage: vi.fn(), getUsersPage: vi.fn(), getEmployeeAssignments: vi.fn(),
  createMunicipalEmployee: vi.fn(), updateMunicipalEmployee: vi.fn(), createEmployeeAssignment: vi.fn(), closeEmployeeAssignment: vi.fn(),
}));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canCreate: () => true, canUpdate: () => true, canReadField: () => true, canEditField: () => true }) }));

describe('TenantEmployeeAdministration', () => {
  beforeEach(() => {
    api.getMunicipalEmployeesPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'employee-1', employeeNumber: 'E001', firstName: 'Ada', lastName: 'Mokoena', emailAddress: 'ada@example.test', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'department-1', code: 'FIN', name: 'Finance', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getUnitMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'unit-1', departmentPublicId: 'department-1', departmentName: 'Finance', code: 'BUD', name: 'Budget', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'Ag==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getPositionMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'position-1', departmentPublicId: 'department-1', departmentName: 'Finance', unitPublicId: 'unit-1', unitName: 'Budget', code: 'CFO', name: 'Chief Financial Officer', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'Aw==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getUsersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 } });
    api.getEmployeeAssignments.mockResolvedValue({ success: true, data: [{ publicId: 'assignment-1', employeePublicId: 'employee-1', departmentPublicId: 'department-1', departmentName: 'Finance', unitPublicId: 'unit-1', unitName: 'Budget', positionCode: 'CFO', positionName: 'Chief Financial Officer', effectiveFrom: '2026-07-01T00:00:00Z', effectiveTo: null, isPrimary: true, isActive: true, rowVersion: 'Ag==' }] });
    api.createEmployeeAssignment.mockResolvedValue({ success: true, data: {} });
    api.closeEmployeeAssignment.mockResolvedValue({ success: true, data: {} });
  });

  it('loads tenant employees and creates an effective-dated placement using public identifiers', async () => {
    render(<TenantEmployeeAdministration />);
    const employeeButton = await screen.findByRole('button', { name: /Ada Mokoena/ });
    fireEvent.click(employeeButton);
    expect(api.getMunicipalEmployeesPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' });
    await waitFor(() => expect(api.getEmployeeAssignments).toHaveBeenCalledWith('employee-1'));
    await waitFor(() => expect(employeeButton).toHaveClass('border-primary-500'));

    fireEvent.change(await screen.findByRole('combobox', { name: /Department/ }), { target: { value: 'department-1' } });
    fireEvent.change(await screen.findByRole('combobox', { name: 'Unit' }), { target: { value: 'unit-1' } });
    fireEvent.change(await screen.findByRole('combobox', { name: /Position/ }), { target: { value: 'position-1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create placement' }));

    await waitFor(() => expect(api.createEmployeeAssignment).toHaveBeenCalledWith(expect.objectContaining({ employeePublicId: 'employee-1', departmentPublicId: 'department-1', unitPublicId: 'unit-1', positionPublicId: 'position-1', isPrimary: true })));
  });

  it('ends a placement with its concurrency token and governance reason', async () => {
    render(<TenantEmployeeAdministration />);
    fireEvent.click(await screen.findByRole('button', { name: /Ada Mokoena/ }));
    await screen.findByRole('button', { name: 'End placement' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'End placement' })).not.toBeDisabled());
    fireEvent.change(await screen.findByLabelText(/Closure reason/), { target: { value: 'Organizational placement ended' } });
    fireEvent.click(screen.getByRole('button', { name: 'End placement' }));
    await waitFor(() => expect(api.closeEmployeeAssignment).toHaveBeenCalledWith('assignment-1', expect.objectContaining({ reason: 'Organizational placement ended', rowVersion: 'Ag==' })));
  });
});
