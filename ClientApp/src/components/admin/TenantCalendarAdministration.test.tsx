import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TenantCalendarAdministration } from './TenantCalendarAdministration';

const api = vi.hoisted(() => ({
  getFinancialYearMasters: vi.fn(), getMunicipalityFinancialYearMasters: vi.fn(), getReportingPeriodMasters: vi.fn(),
  createFinancialYearMaster: vi.fn(), createMunicipalityFinancialYearMaster: vi.fn(), createReportingPeriodMaster: vi.fn(), updateMunicipalityFinancialYearMaster: vi.fn(),
}));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canCreate: () => true, canUpdate: () => true }) }));

describe('TenantCalendarAdministration', () => {
  beforeEach(() => {
    api.getFinancialYearMasters.mockResolvedValue({ success: true, data: [{ publicId: 'fy-1', code: '2026/27', name: '2026/27', startDate: '2026-07-01T00:00:00Z', endDate: '2027-06-30T00:00:00Z', isActive: true, rowVersion: 'AQ==' }] });
    api.getMunicipalityFinancialYearMasters.mockResolvedValue({ success: true, data: [{ publicId: 'mfy-1', financialYearPublicId: 'fy-1', code: '2026/27', name: '2026/27', isCurrent: false, isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'Ag==' }] });
    api.getReportingPeriodMasters.mockResolvedValue({ success: true, data: [] });
    api.createReportingPeriodMaster.mockResolvedValue({ success: true, data: {} });
    api.updateMunicipalityFinancialYearMaster.mockResolvedValue({ success: true, data: {} });
  });

  it('loads authoritative masters, creates a canonical period, and promotes a current year with RowVersion', async () => {
    render(<TenantCalendarAdministration />);
    expect((await screen.findAllByText('2026/27 · 2026/27')).length).toBeGreaterThan(0);

    fireEvent.change(screen.getByLabelText('Municipality year'), { target: { value: 'mfy-1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create period' }));
    await waitFor(() => expect(api.createReportingPeriodMaster).toHaveBeenCalledWith(expect.objectContaining({ municipalityFinancialYearPublicId: 'mfy-1', code: 'Q1', periodType: 1 })));

    fireEvent.click(screen.getByRole('button', { name: 'Make current' }));
    await waitFor(() => expect(api.updateMunicipalityFinancialYearMaster).toHaveBeenCalledWith('mfy-1', expect.objectContaining({ isCurrent: true, rowVersion: 'Ag==' })));
  });
});
