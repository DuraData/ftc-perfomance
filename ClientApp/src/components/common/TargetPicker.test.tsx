import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as api from '../../api/api';
import { TargetPicker } from './TargetPicker';

vi.mock('../../api/api', () => ({
  getOpmsTargetOptions: vi.fn(),
  getIpmsTargetOptions: vi.fn(),
  getOpmsTarget: vi.fn(),
  getIpmsTarget: vi.fn(),
}));

describe('TargetPicker', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getOpmsTargetOptions).mockResolvedValue({
      success: true,
      data: {
        items: [{ publicId: 'public-1', indicatorNumber: 'KPI-001', targetName: 'Water reliability', departmentName: 'Infrastructure' }],
        page: 1,
        pageSize: 25,
        totalCount: 26,
        totalPages: 2,
      },
    });
  });

  it('searches and pages through bounded options and returns the selected target', async () => {
    const onChange = vi.fn();
    render(<TargetPicker kind="opms" label="OPMS target" value="" onChange={onChange} />);

    await screen.findByRole('option', { name: 'KPI-001 · Water reliability · Infrastructure' });
    expect(api.getOpmsTargetOptions).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25, sortBy: 'indicatorNumber' }));

    fireEvent.change(screen.getByLabelText('OPMS target search'), { target: { value: 'water' } });
    await waitFor(() => expect(api.getOpmsTargetOptions).toHaveBeenCalledWith(expect.objectContaining({ search: 'water', page: 1 })));

    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    await waitFor(() => expect(api.getOpmsTargetOptions).toHaveBeenCalledWith(expect.objectContaining({ search: 'water', page: 2 })));

    fireEvent.change(screen.getByLabelText('OPMS target', { selector: 'select' }), { target: { value: 'public-1' } });
    expect(onChange).toHaveBeenCalledWith('public-1', expect.objectContaining({ publicId: 'public-1' }));
  });

  it('exposes an accessible validation error on the governed selector', async () => {
    render(<TargetPicker kind="opms" label="OPMS target" value="" onChange={vi.fn()} required error="Select a target." />);

    const selector = await screen.findByRole('combobox', { name: /^OPMS target/ });
    expect(selector).toHaveAttribute('aria-invalid', 'true');
    expect(selector).toHaveAccessibleDescription('Select a target.');
    expect(screen.getByRole('alert')).toHaveTextContent('Select a target.');
  });
});
