import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { OPMSTargetFilters } from './OPMSTargetList';

const api = vi.hoisted(() => ({
  getDepartmentMastersPage: vi.fn(),
  getUnitMastersPage: vi.fn(),
  getPositionMastersPage: vi.fn(),
  getWardMastersPage: vi.fn(),
  getVoteNumberMastersPage: vi.fn(),
}));
vi.mock('../../api/api', () => api);

describe('OPMSTargetFilters', () => {
  it('loads bounded department options and emits a stable public-id filter', async () => {
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'department-public', code: 'WAT', name: 'Live Water Services', isActive: true }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    const onFilterChange = vi.fn();
    render(<OPMSTargetFilters onFilterChange={onFilterChange} />);

    await waitFor(() => expect(api.getDepartmentMastersPage).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25, active: true })));
    fireEvent.change(screen.getByLabelText('Department'), { target: { value: 'department-public' } });

    expect(screen.getByRole('option', { name: 'WAT · Live Water Services' })).toHaveValue('department-public');
    expect(onFilterChange).toHaveBeenCalledWith({ department: 'department-public', status: '' });
  });
});
