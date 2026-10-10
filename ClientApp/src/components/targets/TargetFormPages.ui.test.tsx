import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { buildPeriodSelectOptions, TargetFormActions } from './TargetFormPages';

describe('TargetFormPages UI regressions', () => {
  it('keeps an explicit empty period option ahead of loaded periods', () => {
    expect(buildPeriodSelectOptions([{ id: 42, name: '2026/2027 Financial Year' }])).toEqual([
      { value: '', label: 'Select Period' },
      { value: '42', label: '2026/2027 Financial Year' },
    ]);
  });

  it('renders a usable save action in edit mode', () => {
    const onCancel = vi.fn();
    const onSave = vi.fn();

    render(<TargetFormActions isEditing onCancel={onCancel} onSave={onSave} />);

    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
    expect(onSave).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole('button', { name: 'Submit for Approval' })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('preserves draft and approval actions in create mode', () => {
    const onSave = vi.fn();

    render(<TargetFormActions isEditing={false} onCancel={vi.fn()} onSave={onSave} />);

    fireEvent.click(screen.getByRole('button', { name: 'Save Draft' }));
    fireEvent.click(screen.getByRole('button', { name: 'Submit for Approval' }));
    expect(onSave).toHaveBeenCalledTimes(2);
  });
});
