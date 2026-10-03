import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { OPMSTargetFilters } from './OPMSTargetList';

describe('OPMSTargetFilters', () => {
  it('renders departments supplied by the live lookup service contract', () => {
    const onFilterChange = vi.fn();
    render(
      <OPMSTargetFilters
        departments={[{ id: 17, publicId: 'department-public', code: 'WAT', name: 'Live Water Services' }]}
        onFilterChange={onFilterChange}
      />,
    );

    fireEvent.change(screen.getByLabelText('Department'), { target: { value: '17' } });

    expect(screen.getByRole('option', { name: 'Live Water Services' })).toHaveValue('17');
    expect(onFilterChange).toHaveBeenCalledWith({ department: '17', status: '' });
  });
});
