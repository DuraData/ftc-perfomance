import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const api = vi.hoisted(() => ({ getStrategicPlanningMastersPage: vi.fn() }));
vi.mock('../../api/api', () => api);
import { StrategicPlanningMasterPicker } from './StrategicPlanningMasterPicker';

describe('StrategicPlanningMasterPicker', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getStrategicPlanningMastersPage.mockResolvedValue({
      success: true,
      data: {
        items: [{ publicId: 'goal-1', code: 'SG1', name: 'Inclusive growth', description: null, effectiveFromFinancialYearPublicId: null, effectiveFromFinancialYearCode: null, effectiveToFinancialYearPublicId: null, effectiveToFinancialYearCode: null, displayOrder: 1, isActive: true, rowVersion: 'AQ==' }],
        page: 1, pageSize: 25, totalCount: 26, totalPages: 2,
      },
    });
  });

  it('searches and pages strategic options through the bounded contract', async () => {
    const onChange = vi.fn();
    render(<StrategicPlanningMasterPicker kind="strategic-goals" label="Parent" value="" onChange={onChange} />);
    await waitFor(() => expect(api.getStrategicPlanningMastersPage).toHaveBeenCalledWith('strategic-goals', expect.objectContaining({ page: 1, pageSize: 25, sortBy: 'name' })));
    fireEvent.change(screen.getByLabelText('Parent search'), { target: { value: 'growth' } });
    await waitFor(() => expect(api.getStrategicPlanningMastersPage).toHaveBeenLastCalledWith('strategic-goals', expect.objectContaining({ page: 1, search: 'growth' })));
    fireEvent.change(screen.getByLabelText('Parent', { selector: 'select' }), { target: { value: 'goal-1' } });
    expect(onChange).toHaveBeenCalledWith('goal-1', expect.objectContaining({ name: 'Inclusive growth' }));
    fireEvent.click(screen.getByRole('button', { name: 'Next parent' }));
    await waitFor(() => expect(api.getStrategicPlanningMastersPage).toHaveBeenLastCalledWith('strategic-goals', expect.objectContaining({ page: 2, search: 'growth' })));
  });

  it('retains a selected option that is outside the current page', async () => {
    render(<StrategicPlanningMasterPicker kind="strategic-objectives" label="Child" value="historic-objective" selectedLabel="Historic objective" onChange={vi.fn()} />);
    expect(await screen.findByRole('option', { name: 'Historic objective' })).toHaveValue('historic-objective');
  });

  it('resolves a legacy name-backed selection to its governed public identity', async () => {
    const onChange = vi.fn();
    render(<StrategicPlanningMasterPicker kind="strategic-goals" label="Goal" value="" selectedLabel="Inclusive growth" onChange={onChange} />);
    await waitFor(() => expect(api.getStrategicPlanningMastersPage).toHaveBeenCalledWith('strategic-goals', expect.objectContaining({ search: 'Inclusive growth' })));
    await waitFor(() => expect(onChange).toHaveBeenCalledWith('goal-1', expect.objectContaining({ name: 'Inclusive growth' })));
  });
});
