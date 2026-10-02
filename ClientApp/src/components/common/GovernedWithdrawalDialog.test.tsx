import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { GovernedWithdrawalDialog } from './GovernedWithdrawalDialog';

describe('GovernedWithdrawalDialog', () => {
  it('requires a reason and submits the normalized governance reason', () => {
    const onConfirm = vi.fn();
    render(
      <GovernedWithdrawalDialog
        isOpen
        recordLabel="OPMS target"
        onClose={vi.fn()}
        onConfirm={onConfirm}
      />,
    );

    const confirm = screen.getByRole('button', { name: 'Withdraw' });
    expect(confirm).toBeDisabled();
    fireEvent.change(screen.getByLabelText(/Governance reason/), { target: { value: '  Superseded by approved plan  ' } });
    expect(confirm).toBeEnabled();
    fireEvent.click(confirm);

    expect(onConfirm).toHaveBeenCalledWith('Superseded by approved plan');
    expect(screen.getByText(/preserves the record and its history/i)).toBeInTheDocument();
  });
});
