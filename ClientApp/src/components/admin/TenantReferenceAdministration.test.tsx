import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TenantReferenceAdministration } from './TenantReferenceAdministration';

const api = vi.hoisted(() => ({
  getWardMastersPage: vi.fn(), getVoteNumberMastersPage: vi.fn(), getDepartmentMastersPage: vi.fn(), getUnitMastersPage: vi.fn(), getPositionMastersPage: vi.fn(),
  getFinancialYearMastersPage: vi.fn(), getMunicipalityFinancialYearMastersPage: vi.fn(), getReportingPeriodMastersPage: vi.fn(), getSdbipLayerMastersPage: vi.fn(),
  saveWardMaster: vi.fn(), saveVoteNumberMaster: vi.fn(),
}));
const security = vi.hoisted(() => ({ canReadField: vi.fn(() => true), canEditField: vi.fn(() => true) }));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canCreate: () => true, canUpdate: () => true, ...security }) }));

describe('TenantReferenceAdministration', () => {
  beforeEach(() => {
    security.canReadField.mockReturnValue(true);
    security.canEditField.mockReturnValue(true);
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'department-1', code: 'FIN', name: 'Finance', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getMunicipalityFinancialYearMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'municipality-year-1', financialYearPublicId: 'year-1', code: '2026-2027', name: '2026/2027', isCurrent: true, isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getVoteNumberMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'vote-1', id: 1, departmentPublicId: 'department-1', departmentName: 'Finance', municipalityFinancialYearPublicId: 'municipality-year-1', financialYearCode: '2026-2027', code: 'V01', number: '001', name: 'Operating Vote', amount: 1250, isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'Ag==' }], page: 1, pageSize: 25, totalCount: 27, totalPages: 2 } });
    api.getWardMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.saveVoteNumberMaster.mockResolvedValue({ success: true, data: {} });
  });

  it('edits a vote number through the governed API with tenant relationship and RowVersion', async () => {
    render(<TenantReferenceAdministration kind="vote-numbers" />);
    fireEvent.click(await screen.findByRole('button', { name: /Operating Vote/ }));
    const reason = screen.getAllByRole('textbox').find(element => element.tagName === 'TEXTAREA');
    expect(reason).toBeDefined();
    fireEvent.change(reason!, { target: { value: 'Council approved budget amendment' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    await waitFor(() => expect(api.saveVoteNumberMaster).toHaveBeenCalledWith('vote-1', expect.objectContaining({
      departmentPublicId: 'department-1', municipalityFinancialYearPublicId: 'municipality-year-1', code: 'V01', number: '001', amount: 1250, rowVersion: 'Ag==', reason: 'Council approved budget amendment',
    })));
  });

  it('filters and sorts the authoritative reference register', async () => {
    render(<TenantReferenceAdministration kind="vote-numbers" />);
    expect(await screen.findByText('27')).toBeInTheDocument();
    fireEvent.change(screen.getAllByLabelText('Status')[0], { target: { value: 'inactive' } });
    fireEvent.change(screen.getByLabelText('Sort'), { target: { value: 'amount:desc' } });
    await waitFor(() => expect(api.getVoteNumberMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ active: false, sortBy: 'amount', sortDirection: 'desc' })));
  });

  it('hides vote financial members and preserves them on unrelated edits without member permission', async () => {
    security.canReadField.mockReturnValue(false);
    security.canEditField.mockReturnValue(false);
    render(<TenantReferenceAdministration kind="vote-numbers" />);
    const row = await screen.findByRole('button', { name: /Operating Vote/ });
    expect(row).not.toHaveTextContent('001');
    expect(row).not.toHaveTextContent('1,250');
    expect(screen.queryByRole('option', { name: 'Vote number' })).not.toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Highest amount' })).not.toBeInTheDocument();
    fireEvent.click(row);
    expect(screen.queryByLabelText('Vote number')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Amount (R)')).not.toBeInTheDocument();
    const reason = screen.getAllByRole('textbox').find(element => element.tagName === 'TEXTAREA');
    fireEvent.change(reason!, { target: { value: 'Approved non-sensitive vote change' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));
    await waitFor(() => expect(api.saveVoteNumberMaster).toHaveBeenCalledWith('vote-1', expect.objectContaining({
      numberSpecified: false, amountSpecified: false,
    })));
  });
});
