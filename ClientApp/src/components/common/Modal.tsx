import React, { useEffect } from 'react';
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
  useEffect(() => {
    if (isOpen) document.body.style.overflow = 'hidden';
    else document.body.style.overflow = 'unset';
    return () => { document.body.style.overflow = 'unset'; };
  }, [isOpen]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-secondary-900/50 backdrop-blur-sm" onClick={onClose} />
      <div className={`relative bg-white dark:bg-secondary-900 rounded-lg shadow-xl w-full ${modalSizes[size]} max-h-[85vh] flex flex-col animate-slide-up`}>
        {(title || showCloseButton) && (
          <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700">
            {title && <h2 className="text-sm font-semibold text-secondary-900 dark:text-white">{title}</h2>}
            {showCloseButton && (
              <button onClick={onClose} className="p-1 rounded hover:bg-secondary-100 dark:hover:bg-secondary-800">
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
  if (!isOpen) return null;

  const buttonVariants = { danger: 'error', warning: 'warning', info: 'primary' } as const;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-secondary-900/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative bg-white dark:bg-secondary-900 rounded-lg shadow-xl w-full max-w-sm p-4 animate-slide-up">
        <h3 className="text-sm font-semibold text-secondary-900 dark:text-white mb-1">{title}</h3>
        <p className="text-xs text-secondary-600 dark:text-secondary-400 mb-4">{message}</p>
        <div className="flex items-center justify-end gap-2">
          <Button variant="outline" size="sm" onClick={onClose}>{cancelLabel}</Button>
          <Button variant={buttonVariants[variant]} size="sm" onClick={onConfirm}>{confirmLabel}</Button>
        </div>
      </div>
    </div>
  );
}
