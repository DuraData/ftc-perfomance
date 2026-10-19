import { useEffect, useState } from 'react';
import { Button } from '../ui';
import { Textarea } from './Form';
import { Modal } from './Modal';

interface GovernedReasonDialogProps {
  isOpen: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  busy?: boolean;
  reasonLabel?: string;
  maxLength?: number;
  onClose: () => void;
  onConfirm: (reason: string) => void;
}

export function GovernedReasonDialog({
  isOpen,
  title,
  description,
  confirmLabel,
  busy = false,
  reasonLabel = 'Governance reason',
  maxLength = 1000,
  onClose,
  onConfirm,
}: GovernedReasonDialogProps) {
  const [reason, setReason] = useState('');

  useEffect(() => {
    if (isOpen) setReason('');
  }, [isOpen]);

  const normalized = reason.trim();
  return (
    <Modal
      isOpen={isOpen}
      onClose={busy ? () => undefined : onClose}
      title={title}
      size="sm"
      footer={(
        <>
          <Button variant="outline" size="sm" disabled={busy} onClick={onClose}>Cancel</Button>
          <Button
            variant="error"
            size="sm"
            loading={busy}
            disabled={!normalized || normalized.length > maxLength}
            onClick={() => onConfirm(normalized)}
          >
            {confirmLabel}
          </Button>
        </>
      )}
    >
      <p className="mb-3 text-xs text-secondary-600 dark:text-secondary-300">{description}</p>
      <Textarea
        autoFocus
        label={reasonLabel}
        value={reason}
        rows={4}
        maxLength={maxLength}
        required
        onChange={(event) => setReason(event.target.value)}
      />
      <p className="mt-1 text-right text-[10px] text-secondary-500" aria-live="polite">{reason.length}/{maxLength}</p>
    </Modal>
  );
}
