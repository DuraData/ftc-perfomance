import { useEffect, useState } from 'react';
import { Button } from '../ui';
import { Textarea } from './Form';
import { Modal } from './Modal';

interface GovernedWithdrawalDialogProps {
  isOpen: boolean;
  recordLabel: string;
  busy?: boolean;
  onClose: () => void;
  onConfirm: (reason: string) => void;
}

export function GovernedWithdrawalDialog({ isOpen, recordLabel, busy = false, onClose, onConfirm }: GovernedWithdrawalDialogProps) {
  const [reason, setReason] = useState('');

  useEffect(() => {
    if (isOpen) setReason('');
  }, [isOpen]);

  const normalized = reason.trim();
  return (
    <Modal
      isOpen={isOpen}
      onClose={busy ? () => undefined : onClose}
      title={`Withdraw ${recordLabel}`}
      size="sm"
      footer={(
        <>
          <Button variant="outline" size="sm" disabled={busy} onClick={onClose}>Cancel</Button>
          <Button variant="error" size="sm" disabled={busy || !normalized || normalized.length > 1000} onClick={() => onConfirm(normalized)}>
            {busy ? 'Withdrawing…' : 'Withdraw'}
          </Button>
        </>
      )}
    >
      <p className="mb-3 text-xs text-secondary-600 dark:text-secondary-300">
        Withdrawal preserves the record and its history. It cannot be edited or advanced through workflow afterward.
      </p>
      <Textarea
        label="Governance reason"
        value={reason}
        rows={4}
        maxLength={1000}
        required
        onChange={(event) => setReason(event.target.value)}
      />
      <p className="mt-1 text-right text-[10px] text-secondary-500">{reason.length}/1000</p>
    </Modal>
  );
}
