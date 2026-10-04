import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const api = vi.hoisted(() => ({
  getFinancialYearMastersPage: vi.fn(),
  getMunicipalityFinancialYearMastersPage: vi.fn(),
  getReportingPeriodMastersPage: vi.fn(),
  getSdbipLayerMastersPage: vi.fn(),
}));
vi.mock('../../api/api', () => api);
import { CalendarMasterPicker } from './CalendarMasterPicker';

describe('CalendarMasterPicker', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getMunicipalityFinancialYearMastersPage.mockResolvedValue({
      success: true,
      data: {
        items: [{ publicId: 'year-1', financialYearPublicId: 'fy-1', code: '2026/27', name: '2026/27', isCurrent: true, isActive: true, effectiveFrom: '2026-07-01', rowVersion: 'AQ==' }],
        page: 1, pageSize: 25, totalCount: 26, totalPages: 2,
      },
    });
  });

  it('searches, pages and returns an authoritative selected option', async () => {
    const onChange = vi.fn();
    render(<CalendarMasterPicker kind="municipality-financial-year" label="Financial year" value="" onChange={onChange} />);
    await waitFor(() => expect(api.getMunicipalityFinancialYearMastersPage).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25, active: true, sortBy: 'startDate' })));
    fireEvent.change(screen.getByLabelText('Financial year search'), { target: { value: '2026' } });
    await waitFor(() => expect(api.getMunicipalityFinancialYearMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, search: '2026' })));
    fireEvent.change(screen.getByLabelText('Financial year', { selector: 'select' }), { target: { value: 'year-1' } });
    expect(onChange).toHaveBeenCalledWith('year-1', expect.objectContaining({ code: '2026/27' }));
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    await waitFor(() => expect(api.getMunicipalityFinancialYearMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, search: '2026' })));
  });

  it('retains a selected value that is outside the current page', async () => {
    render(<CalendarMasterPicker kind="municipality-financial-year" label="Financial year" value="historic-year" selectedLabel="2020/21 · Historic" onChange={vi.fn()} />);
    expect(await screen.findByRole('option', { name: '2020/21 · Historic' })).toHaveValue('historic-year');
  });
});
