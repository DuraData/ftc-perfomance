import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TenantEmployeeAdministration } from './TenantEmployeeAdministration';

const api = vi.hoisted(() => ({
  getMunicipalEmployees: vi.fn(), getDepartments: vi.fn(), getUnits: vi.fn(), getPositionMasters: vi.fn(), getUsers: vi.fn(), getEmployeeAssignments: vi.fn(),
  createMunicipalEmployee: vi.fn(), updateMunicipalEmployee: vi.fn(), createEmployeeAssignment: vi.fn(), closeEmployeeAssignment: vi.fn(),
}));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canCreate: () => true, canUpdate: () => true }) }));

describe('TenantEmployeeAdministration', () => {
  beforeEach(() => {
    api.getMunicipalEmployees.mockResolvedValue({ success: true, data: [{ publicId: 'employee-1', employeeNumber: 'E001', firstName: 'Ada', lastName: 'Mokoena', emailAddress: 'ada@example.test', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }] });
    api.getDepartments.mockResolvedValue({ success: true, data: [{ id: 7, publicId: 'department-1', code: 'FIN', name: 'Finance' }] });
    api.getUnits.mockResolvedValue({ success: true, data: [{ id: 8, publicId: 'unit-1', departmentId: 7, departmentName: 'Finance', code: 'BUD', name: 'Budget' }] });
    api.getPositionMasters.mockResolvedValue({ success: true, data: [{ publicId: 'position-1', departmentPublicId: 'department-1', departmentName: 'Finance', unitPublicId: 'unit-1', unitName: 'Budget', code: 'CFO', name: 'Chief Financial Officer', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }] });
    api.getUsers.mockResolvedValue({ success: true, data: [] });
    api.getEmployeeAssignments.mockResolvedValue({ success: true, data: [{ publicId: 'assignment-1', employeePublicId: 'employee-1', departmentPublicId: 'department-1', departmentName: 'Finance', unitPublicId: 'unit-1', unitName: 'Budget', positionCode: 'CFO', positionName: 'Chief Financial Officer', effectiveFrom: '2026-07-01T00:00:00Z', effectiveTo: null, isPrimary: true, isActive: true, rowVersion: 'Ag==' }] });
    api.createEmployeeAssignment.mockResolvedValue({ success: true, data: {} });
    api.closeEmployeeAssignment.mockResolvedValue({ success: true, data: {} });
  });

  it('loads tenant employees and creates an effective-dated placement using public identifiers', async () => {
    render(<TenantEmployeeAdministration />);
    fireEvent.click(await screen.findByRole('button', { name: /Ada Mokoena/ }));
    await waitFor(() => expect(api.getEmployeeAssignments).toHaveBeenCalledWith('employee-1'));

    fireEvent.change(screen.getByLabelText('Department'), { target: { value: 'department-1' } });
    fireEvent.change(screen.getByLabelText('Unit'), { target: { value: 'unit-1' } });
    fireEvent.change(screen.getByLabelText('Position'), { target: { value: 'position-1' } });
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
