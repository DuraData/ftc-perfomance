import { GovernedReasonDialog } from './GovernedReasonDialog';

interface GovernedWithdrawalDialogProps {
  isOpen: boolean;
  recordLabel: string;
  busy?: boolean;
  onClose: () => void;
  onConfirm: (reason: string) => void;
}

export function GovernedWithdrawalDialog({ isOpen, recordLabel, busy = false, onClose, onConfirm }: GovernedWithdrawalDialogProps) {
  return (
    <GovernedReasonDialog
      isOpen={isOpen}
      title={`Withdraw ${recordLabel}`}
      description="Withdrawal preserves the record and its history. It cannot be edited or advanced through workflow afterward."
      confirmLabel="Withdraw"
      busy={busy}
      onClose={onClose}
      onConfirm={onConfirm}
    />
  );
}
