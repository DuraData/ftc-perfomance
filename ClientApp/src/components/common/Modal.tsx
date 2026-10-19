import React, { useEffect, useId, useRef } from 'react';
import { X } from 'lucide-react';
import { Button } from '../ui';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  children: React.ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'full';
  showCloseButton?: boolean;
  footer?: React.ReactNode;
}

const modalSizes = {
  sm: 'max-w-sm',
  md: 'max-w-lg',
  lg: 'max-w-2xl',
  xl: 'max-w-3xl',
  full: 'max-w-[90vw]',
};

export function Modal({
  isOpen,
  onClose,
  title,
  children,
  size = 'md',
  showCloseButton = true,
  footer,
}: ModalProps) {
  const titleId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const onCloseRef = useRef(onClose);

  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  useEffect(() => {
    if (!isOpen) return undefined;

    const previousOverflow = document.body.style.overflow;
    const previouslyFocused = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const focusableSelector = [
      'a[href]',
      'button:not([disabled])',
      'input:not([disabled])',
      'select:not([disabled])',
      'textarea:not([disabled])',
      '[tabindex]:not([tabindex="-1"])',
    ].join(',');
    const focusDialog = () => {
      const focusable = Array.from(dialogRef.current?.querySelectorAll<HTMLElement>(focusableSelector) ?? []);
      (focusable[0] ?? dialogRef.current)?.focus();
    };
    const animationFrame = window.requestAnimationFrame(focusDialog);

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        onCloseRef.current();
        return;
      }

      if (event.key !== 'Tab') return;
      const dialog = dialogRef.current;
      if (!dialog) return;
      const focusable = Array.from(dialog.querySelectorAll<HTMLElement>(focusableSelector));
      if (focusable.length === 0) {
        event.preventDefault();
        dialog.focus();
        return;
      }

      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && (document.activeElement === first || !dialog.contains(document.activeElement))) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && (document.activeElement === last || !dialog.contains(document.activeElement))) {
        event.preventDefault();
        first.focus();
      }
    };

    document.body.style.overflow = 'hidden';
    document.addEventListener('keydown', handleKeyDown);

    return () => {
      window.cancelAnimationFrame(animationFrame);
      document.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = previousOverflow;
      if (previouslyFocused?.isConnected) previouslyFocused.focus();
    };
  }, [isOpen]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-secondary-900/50 backdrop-blur-sm" aria-hidden="true" onClick={onClose} />
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={title ? titleId : undefined}
        aria-label={title ? undefined : 'Dialog'}
        tabIndex={-1}
        className={`relative bg-white dark:bg-secondary-900 rounded-lg shadow-xl w-full ${modalSizes[size]} max-h-[85vh] flex flex-col animate-slide-up`}
      >
        {(title || showCloseButton) && (
          <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700">
            {title && <h2 id={titleId} className="text-sm font-semibold text-secondary-900 dark:text-white">{title}</h2>}
            {showCloseButton && (
              <button type="button" aria-label={title ? `Close ${title}` : 'Close dialog'} onClick={onClose} className="p-1 rounded hover:bg-secondary-100 dark:hover:bg-secondary-800">
                <X className="w-4 h-4 text-secondary-500" />
              </button>
            )}
          </div>
        )}
        <div className="flex-1 overflow-y-auto px-4 py-3">{children}</div>
        {footer && (
          <div className="flex items-center justify-end gap-2 px-4 py-3 border-t border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-800 rounded-b-lg">
            {footer}
          </div>
        )}
      </div>
    </div>
  );
}

interface ConfirmDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'danger' | 'warning' | 'info';
}

export function ConfirmDialog({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  variant = 'danger',
}: ConfirmDialogProps) {
  const buttonVariants = { danger: 'error', warning: 'warning', info: 'primary' } as const;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={title}
      size="sm"
      showCloseButton={false}
      footer={(
        <>
          <Button variant="outline" size="sm" onClick={onClose}>{cancelLabel}</Button>
          <Button variant={buttonVariants[variant]} size="sm" onClick={onConfirm}>{confirmLabel}</Button>
        </>
      )}
    >
      <p className="text-xs text-secondary-600 dark:text-secondary-400">{message}</p>
    </Modal>
  );
}
