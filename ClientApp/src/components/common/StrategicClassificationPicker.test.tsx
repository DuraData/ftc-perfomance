import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const api = vi.hoisted(() => ({ getStrategicClassificationPage: vi.fn() }));
vi.mock('../../api/api', () => api);
import { StrategicClassificationPicker } from './StrategicClassificationPicker';

describe('StrategicClassificationPicker', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getStrategicClassificationPage.mockResolvedValue({
      success: true,
      data: {
        items: [{ publicId: 'goal-1', code: 'SG1', name: 'Inclusive growth', displayOrder: 1, symbol: null }],
        page: 1, pageSize: 25, totalCount: 26, totalPages: 2,
      },
    });
  });

  it('searches and pages hierarchy options through the KPI-authorized bounded contract', async () => {
    const onChange = vi.fn();
    render(<StrategicClassificationPicker targetKind="opms" classificationKind="strategic-goals" municipalityFinancialYearPublicId="year-1" label="Strategic Goal" value="" parentPublicId="kpa-1" relationshipType="municipal-kpa-strategic-goal" onChange={onChange} />);
    await waitFor(() => expect(api.getStrategicClassificationPage).toHaveBeenCalledWith(
      'opms', 'year-1', 'strategic-goals', expect.objectContaining({ page: 1, pageSize: 25, sortBy: 'name' }),
      { parentPublicId: 'kpa-1', relationshipType: 'municipal-kpa-strategic-goal' },
    ));
    fireEvent.change(screen.getByLabelText('Strategic Goal search'), { target: { value: 'growth' } });
    await waitFor(() => expect(api.getStrategicClassificationPage).toHaveBeenLastCalledWith(
      'opms', 'year-1', 'strategic-goals', expect.objectContaining({ page: 1, search: 'growth' }),
      { parentPublicId: 'kpa-1', relationshipType: 'municipal-kpa-strategic-goal' },
    ));
    fireEvent.change(screen.getByLabelText('Strategic Goal', { selector: 'select' }), { target: { value: 'goal-1' } });
    expect(onChange).toHaveBeenCalledWith('goal-1', expect.objectContaining({ name: 'Inclusive growth' }));
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    await waitFor(() => expect(api.getStrategicClassificationPage).toHaveBeenLastCalledWith(
      'opms', 'year-1', 'strategic-goals', expect.objectContaining({ page: 2, search: 'growth' }),
      { parentPublicId: 'kpa-1', relationshipType: 'municipal-kpa-strategic-goal' },
    ));
  });

  it('retains an off-page selection and waits for a hierarchy parent', async () => {
    render(<StrategicClassificationPicker targetKind="ipms" classificationKind="strategic-objectives" municipalityFinancialYearPublicId="year-1" label="Strategic Objective" value="historic-objective" selectedLabel="Historic objective" relationshipType="strategic-goal-objective" onChange={vi.fn()} />);
    expect(await screen.findByRole('option', { name: 'Historic objective' })).toHaveValue('historic-objective');
    expect(screen.getByText('Select the parent before choosing a strategic objective.')).toBeInTheDocument();
    expect(api.getStrategicClassificationPage).not.toHaveBeenCalled();
  });
});
