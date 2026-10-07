import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TenantEmployeeAdministration } from './TenantEmployeeAdministration';

const api = vi.hoisted(() => ({
  getMunicipalEmployeesPage: vi.fn(), getDepartmentMastersPage: vi.fn(), getUnitMastersPage: vi.fn(), getPositionMastersPage: vi.fn(), getWardMastersPage: vi.fn(), getVoteNumberMastersPage: vi.fn(), getUsersPage: vi.fn(), getEmployeeAssignmentsPage: vi.fn(),
  createMunicipalEmployee: vi.fn(), updateMunicipalEmployee: vi.fn(), createEmployeeAssignment: vi.fn(), closeEmployeeAssignment: vi.fn(),
}));
const security = vi.hoisted(() => ({
  canCreate: vi.fn<(resource: string) => boolean>(() => true), canUpdate: vi.fn<(resource: string) => boolean>(() => true),
  canReadField: vi.fn<(resource: string, member: string) => boolean>(() => true), canEditField: vi.fn<(resource: string, member: string) => boolean>(() => true),
}));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));

describe('TenantEmployeeAdministration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canCreate.mockReturnValue(true);
    security.canUpdate.mockReturnValue(true);
    security.canReadField.mockReturnValue(true);
    security.canEditField.mockReturnValue(true);
    api.getMunicipalEmployeesPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'employee-1', employeeNumber: 'E001', salaryReference: 'SAL-001', firstName: 'Ada', lastName: 'Mokoena', emailAddress: 'ada@example.test', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'department-1', code: 'FIN', name: 'Finance', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getUnitMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'unit-1', departmentPublicId: 'department-1', departmentName: 'Finance', code: 'BUD', name: 'Budget', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'Ag==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getPositionMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'position-1', departmentPublicId: 'department-1', departmentName: 'Finance', unitPublicId: 'unit-1', unitName: 'Budget', code: 'CFO', name: 'Chief Financial Officer', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'Aw==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getUsersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 } });
    api.getEmployeeAssignmentsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'assignment-1', employeePublicId: 'employee-1', departmentPublicId: 'department-1', departmentName: 'Finance', unitPublicId: 'unit-1', unitName: 'Budget', positionCode: 'CFO', positionName: 'Chief Financial Officer', effectiveFrom: '2026-07-01T00:00:00Z', effectiveTo: null, isPrimary: true, isActive: true, rowVersion: 'Ag==' }], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });
    api.createEmployeeAssignment.mockResolvedValue({ success: true, data: {} });
    api.closeEmployeeAssignment.mockResolvedValue({ success: true, data: {} });
    api.createMunicipalEmployee.mockResolvedValue({ success: true, data: {} });
  });

  it('loads tenant employees and creates an effective-dated placement using public identifiers', async () => {
    render(<TenantEmployeeAdministration />);
    const employeeButton = await screen.findByRole('button', { name: /Ada Mokoena/ });
    fireEvent.click(employeeButton);
    expect(api.getMunicipalEmployeesPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' });
    await waitFor(() => expect(api.getEmployeeAssignmentsPage).toHaveBeenCalledWith('employee-1', {
      page: 1, pageSize: 10, search: undefined, sortBy: 'effectiveFrom', sortDirection: 'desc',
    }));
    await waitFor(() => expect(employeeButton).toHaveClass('border-primary-500'));

    fireEvent.change(await screen.findByRole('combobox', { name: /Department/ }), { target: { value: 'department-1' } });
    fireEvent.change(await screen.findByRole('combobox', { name: 'Unit' }), { target: { value: 'unit-1' } });
    fireEvent.change(await screen.findByRole('combobox', { name: /Position/ }), { target: { value: 'position-1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create placement' }));

    await waitFor(() => expect(api.createEmployeeAssignment).toHaveBeenCalledWith(expect.objectContaining({ employeePublicId: 'employee-1', departmentPublicId: 'department-1', unitPublicId: 'unit-1', positionPublicId: 'position-1', isPrimary: true })));
  }, 10_000);

  it('ends a placement with its concurrency token and governance reason', async () => {
    render(<TenantEmployeeAdministration />);
    fireEvent.click(await screen.findByRole('button', { name: /Ada Mokoena/ }));
    await screen.findByRole('button', { name: 'End placement' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'End placement' })).not.toBeDisabled());
    fireEvent.change(await screen.findByLabelText(/Closure reason/), { target: { value: 'Organizational placement ended' } });
    fireEvent.click(screen.getByRole('button', { name: 'End placement' }));
    await waitFor(() => expect(api.closeEmployeeAssignment).toHaveBeenCalledWith('assignment-1', expect.objectContaining({ reason: 'Organizational placement ended', rowVersion: 'Ag==' })));
  });

  it('creates a salary reference only through the protected employee member control', async () => {
    render(<TenantEmployeeAdministration />);
    fireEvent.change(await screen.findByLabelText(/Employee number/), { target: { value: 'E002' } });
    fireEvent.change(screen.getByLabelText('Salary reference'), { target: { value: 'SAL-002' } });
    fireEvent.change(screen.getByLabelText(/First name/), { target: { value: 'Lebo' } });
    fireEvent.change(screen.getByLabelText(/Last name/), { target: { value: 'Dlamini' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create employee' }));

    await waitFor(() => expect(api.createMunicipalEmployee).toHaveBeenCalledWith(expect.objectContaining({
      employeeNumber: 'E002', salaryReference: 'SAL-002', firstName: 'Lebo', lastName: 'Dlamini',
    })));
  });

  it('searches and pages retained placement history with authoritative totals', async () => {
    api.getEmployeeAssignmentsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'assignment-1', employeePublicId: 'employee-1', departmentPublicId: 'department-1', departmentName: 'Finance', unitPublicId: 'unit-1', unitName: 'Budget', positionCode: 'CFO', positionName: 'Chief Financial Officer', effectiveFrom: '2026-07-01T00:00:00Z', effectiveTo: null, isPrimary: true, isActive: true, rowVersion: 'Ag==' }], page: 1, pageSize: 10, totalCount: 11, totalPages: 2 } });
    render(<TenantEmployeeAdministration />);
    fireEvent.click(await screen.findByRole('button', { name: /Ada Mokoena/ }));

    expect(await screen.findByText(/11 retained placements/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next placements' }));
    await waitFor(() => expect(api.getEmployeeAssignmentsPage).toHaveBeenCalledWith('employee-1', expect.objectContaining({
      page: 2, pageSize: 10, sortBy: 'effectiveFrom', sortDirection: 'desc',
    })));

    fireEvent.change(screen.getByLabelText('Search placement history'), { target: { value: 'finance' } });
    await waitFor(() => expect(api.getEmployeeAssignmentsPage).toHaveBeenCalledWith('employee-1', expect.objectContaining({
      page: 1, pageSize: 10, search: 'finance', sortBy: 'effectiveFrom', sortDirection: 'desc',
    })));
  });

  it('does not expose protected employee identifiers or load linked logins without member permissions', async () => {
    security.canReadField.mockImplementation((_resource, member) => member !== 'EmployeeNumber' && member !== 'SalaryReference' && member !== 'IdentityUserId');
    security.canEditField.mockImplementation((_resource, member) => member !== 'EmployeeNumber' && member !== 'SalaryReference' && member !== 'IdentityUserId');
    api.getMunicipalEmployeesPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'employee-1', employeeNumber: null, salaryReference: null, firstName: 'Ada', lastName: 'Mokoena', emailAddress: 'ada@example.test', identityUserPublicId: null, isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });

    render(<TenantEmployeeAdministration />);

    expect(await screen.findByText(/Employee number protected/)).toBeInTheDocument();
    expect(screen.queryByLabelText('Employee number')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Salary reference')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Linked login')).not.toBeInTheDocument();
    expect(api.getUsersPage).not.toHaveBeenCalled();
    expect(screen.queryByRole('option', { name: 'Employee number' })).not.toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Salary reference' })).not.toBeInTheDocument();
  });
});
