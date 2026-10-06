import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const api = vi.hoisted(() => ({
  getDepartmentMastersPage: vi.fn(),
  getUnitMastersPage: vi.fn(),
  getPositionMastersPage: vi.fn(),
  getWardMastersPage: vi.fn(),
  getVoteNumberMastersPage: vi.fn(),
}));
vi.mock('../../api/api', () => api);
import { OrganizationMasterPicker } from './OrganizationMasterPicker';

describe('OrganizationMasterPicker', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getUnitMastersPage.mockResolvedValue({
      success: true,
      data: {
        items: [{ publicId: 'unit-1', departmentPublicId: 'department-1', departmentName: 'Finance', code: 'BUD', name: 'Budget', isActive: true, effectiveFrom: '2026-07-01', rowVersion: 'AQ==' }],
        page: 1, pageSize: 25, totalCount: 26, totalPages: 2,
      },
    });
    api.getVoteNumberMastersPage.mockResolvedValue({
      success: true,
      data: {
        items: [{ publicId: 'vote-1', departmentPublicId: 'department-1', departmentName: 'Finance', municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27', code: 'V01', number: '001', name: 'Operating Vote', amount: 1000, isActive: true, effectiveFrom: '2026-07-01', rowVersion: 'AQ==' }],
        page: 1, pageSize: 25, totalCount: 1, totalPages: 1,
      },
    });
  });

  it('uses bounded dependent search and returns the authoritative option', async () => {
    const onChange = vi.fn();
    render(<OrganizationMasterPicker kind="unit" label="Unit" value="" departmentPublicId="department-1" onChange={onChange} />);
    await waitFor(() => expect(api.getUnitMastersPage).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25, active: true, departmentPublicId: 'department-1' })));
    fireEvent.change(screen.getByLabelText('Unit search'), { target: { value: 'budget' } });
    await waitFor(() => expect(api.getUnitMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, search: 'budget' })));
    fireEvent.change(screen.getByLabelText('Unit', { selector: 'select' }), { target: { value: 'unit-1' } });
    expect(onChange).toHaveBeenCalledWith('unit-1', expect.objectContaining({ departmentPublicId: 'department-1' }));
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    await waitFor(() => expect(api.getUnitMastersPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, search: 'budget' })));
  });

  it('preserves a selected value outside the current page', async () => {
    render(<OrganizationMasterPicker kind="unit" label="Unit" value="historic-unit" selectedLabel="Historic unit" onChange={vi.fn()} />);
    expect(await screen.findByRole('option', { name: 'Historic unit' })).toHaveValue('historic-unit');
  });

  it('scopes vote-number options to the selected municipality financial year', async () => {
    render(<OrganizationMasterPicker kind="vote-number" label="Vote Number" value="" municipalityFinancialYearPublicId="year-1" onChange={vi.fn()} />);
    await waitFor(() => expect(api.getVoteNumberMastersPage).toHaveBeenCalledWith(expect.objectContaining({
      active: true,
      municipalityFinancialYearPublicId: 'year-1',
    })));
    expect(await screen.findByRole('option', { name: 'V01 · 001 · Operating Vote' })).toHaveValue('vote-1');
  });
});
