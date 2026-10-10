import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TenantCalendarAdministration } from './TenantCalendarAdministration';

const api = vi.hoisted(() => ({
  getFinancialYearMastersPage: vi.fn(),
  getMunicipalityFinancialYearMastersPage: vi.fn(), getReportingPeriodMastersPage: vi.fn(), getSdbipLayerMastersPage: vi.fn(),
  createFinancialYearMaster: vi.fn(), updateFinancialYearMaster: vi.fn(), createMunicipalityFinancialYearMaster: vi.fn(), createReportingPeriodMaster: vi.fn(), updateMunicipalityFinancialYearMaster: vi.fn(), updateReportingPeriodMaster: vi.fn(),
  createSdbipLayerMaster: vi.fn(), updateSdbipLayerMaster: vi.fn(),
}));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canCreate: () => true, canUpdate: () => true }) }));

describe('TenantCalendarAdministration', () => {
  beforeEach(() => {
    api.getFinancialYearMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'fy-1', code: '2026/27', name: '2026/27', startDate: '2026-07-01T00:00:00Z', endDate: '2027-06-30T00:00:00Z', isActive: true, rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getMunicipalityFinancialYearMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'mfy-1', financialYearPublicId: 'fy-1', code: '2026/27', name: '2026/27', isCurrent: false, isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'Ag==' }], page: 1, pageSize: 25, totalCount: 27, totalPages: 2 } });
    api.getReportingPeriodMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getSdbipLayerMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.createReportingPeriodMaster.mockResolvedValue({ success: true, data: {} });
    api.createSdbipLayerMaster.mockResolvedValue({ success: true, data: {} });
    api.updateFinancialYearMaster.mockResolvedValue({ success: true, data: {} });
    api.updateMunicipalityFinancialYearMaster.mockResolvedValue({ success: true, data: {} });
    api.updateReportingPeriodMaster.mockResolvedValue({ success: true, data: {} });
  });

  it('edits a global financial year with a governance reason and RowVersion', async () => {
    render(<TenantCalendarAdministration />);
    fireEvent.click(await screen.findByRole('button', { name: 'Edit global year' }));
    expect(screen.getByRole('heading', { name: 'Edit global financial year' })).toBeInTheDocument();
    fireEvent.change(screen.getAllByLabelText(/^Name/)[0], { target: { value: '2026/27 Municipal Financial Year' } });
    fireEvent.change(screen.getByLabelText('Financial year governance reason*'), { target: { value: 'Clarify the governed global year name' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save global year' }));
    await waitFor(() => expect(api.updateFinancialYearMaster).toHaveBeenCalledWith('fy-1', expect.objectContaining({
      name: '2026/27 Municipal Financial Year', isActive: true, reason: 'Clarify the governed global year name', rowVersion: 'AQ==',
    })));
  });

  it('loads authoritative masters, creates a canonical period, and promotes a current year with RowVersion', async () => {
    render(<TenantCalendarAdministration />);
    expect((await screen.findAllByText('2026/27 · 2026/27')).length).toBeGreaterThan(0);

    const municipalityYearSelectors = await screen.findAllByRole('combobox', { name: /Municipality year/ });
    fireEvent.change(municipalityYearSelectors[1], { target: { value: 'mfy-1' } });
    fireEvent.change(screen.getByLabelText(/Display name/), { target: { value: 'Top Layer SDBIP' } });
    fireEvent.change(screen.getAllByLabelText(/^Code/)[2], { target: { value: 'TOP' } });
    fireEvent.change(screen.getByLabelText(/Governance reason/), { target: { value: 'Configure the top layer' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create layer' }));
    await waitFor(() => expect(api.createSdbipLayerMaster).toHaveBeenCalledWith(expect.objectContaining({ municipalityFinancialYearPublicId: 'mfy-1', code: 'TOP', name: 'Top Layer SDBIP', displayOrder: 1, reason: 'Configure the top layer' })));

    await waitFor(() => expect(screen.getByRole('button', { name: 'Refresh' })).not.toBeDisabled());
    fireEvent.change((await screen.findAllByRole('combobox', { name: /Municipality year/ }))[0], { target: { value: 'mfy-1' } });
    fireEvent.change(screen.getByLabelText(/^Reporting period governance reason/), { target: { value: 'Create the approved first-quarter period' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create period' }));
    await waitFor(() => expect(api.createReportingPeriodMaster).toHaveBeenCalledWith(expect.objectContaining({ municipalityFinancialYearPublicId: 'mfy-1', code: 'Q1', periodType: 1, reason: 'Create the approved first-quarter period' })));

    fireEvent.change(screen.getByLabelText(/^Current-year governance reason/), { target: { value: 'Council approved this as the current year' } });
    fireEvent.click(screen.getByRole('button', { name: 'Make current' }));
    await waitFor(() => expect(api.updateMunicipalityFinancialYearMaster).toHaveBeenCalledWith('mfy-1', expect.objectContaining({ isCurrent: true, reason: 'Council approved this as the current year', rowVersion: 'Ag==' })));
  }, 10_000);

  it('searches, filters, sorts, and pages calendar registers on the server', async () => {
    render(<TenantCalendarAdministration />);
    expect(await screen.findByText('27')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Search municipality years'), { target: { value: '2026' } });
    fireEvent.change(screen.getByLabelText('municipality years status'), { target: { value: 'active' } });
    fireEvent.change(screen.getByLabelText('Sort municipality years'), { target: { value: 'current:desc' } });
    await waitFor(() => expect(api.getMunicipalityFinancialYearMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ search: '2026', active: true, sortBy: 'current', sortDirection: 'desc' })));
    fireEvent.click(screen.getAllByRole('button', { name: 'Next' }).find(button => !button.hasAttribute('disabled'))!);
    await waitFor(() => expect(api.getMunicipalityFinancialYearMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 })));
  });

  it('edits municipality-year dates and reporting periods with governance reasons and RowVersion', async () => {
    api.getReportingPeriodMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'period-1', municipalityFinancialYearPublicId: 'mfy-1', code: 'Q1', name: 'Quarter 1', periodType: 1, sequence: 1, startDate: '2026-07-01T00:00:00Z', endDate: '2026-09-30T00:00:00Z', isActive: true, rowVersion: 'Aw==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });

    render(<TenantCalendarAdministration />);
    fireEvent.click(await screen.findByRole('button', { name: 'Edit year' }));
    expect(screen.getByLabelText('Effective from')).toHaveValue('2026-07-01');
    fireEvent.change(screen.getByLabelText('Effective from'), { target: { value: '2026-07-02' } });
    fireEvent.change(screen.getByLabelText('Municipality year governance reason*'), { target: { value: 'Correct the municipality year effective date' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save municipality year' }));
    await waitFor(() => expect(api.updateMunicipalityFinancialYearMaster).toHaveBeenCalledWith('mfy-1', expect.objectContaining({
      effectiveFrom: '2026-07-02T00:00:00.000Z', reason: 'Correct the municipality year effective date', rowVersion: 'Ag==',
    })));

    await waitFor(() => expect(screen.getByRole('button', { name: 'Refresh' })).not.toBeDisabled());
    fireEvent.click(screen.getByRole('button', { name: 'Edit period' }));
    expect(screen.getAllByLabelText('Start')[1]).toHaveValue('2026-07-01');
    fireEvent.change(screen.getAllByLabelText(/^Name/)[1], { target: { value: 'Quarter One' } });
    fireEvent.change(screen.getByLabelText('Reporting period governance reason*'), { target: { value: 'Clarify the canonical reporting period name' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save period' }));
    await waitFor(() => expect(api.updateReportingPeriodMaster).toHaveBeenCalledWith('period-1', expect.objectContaining({
      name: 'Quarter One', isActive: true, reason: 'Clarify the canonical reporting period name', rowVersion: 'Aw==',
    })));
  });
});
