import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import axe from 'axe-core';
import { describe, expect, it, vi } from 'vitest';
import { ConfirmDialog, Modal } from './Modal';

describe('Modal', () => {
  it('moves focus inside, traps keyboard focus, closes with Escape, and restores the opener', async () => {
    const onClose = vi.fn();
    const { rerender } = render(
      <>
        <button type="button">Open editor</button>
        <Modal isOpen={false} onClose={onClose} title="Edit target">
          <input aria-label="Target name" />
          <button type="button">Save target</button>
        </Modal>
      </>,
    );
    const opener = screen.getByRole('button', { name: 'Open editor' });
    opener.focus();

    rerender(
      <>
        <button type="button">Open editor</button>
        <Modal isOpen onClose={onClose} title="Edit target">
          <input aria-label="Target name" />
          <button type="button">Save target</button>
        </Modal>
      </>,
    );

    const close = await screen.findByRole('button', { name: 'Close Edit target' });
    await waitFor(() => expect(close).toHaveFocus());
    const targetName = screen.getByLabelText('Target name');
    targetName.focus();
    rerender(
      <>
        <button type="button">Open editor</button>
        <Modal isOpen onClose={() => onClose()} title="Edit target">
          <input aria-label="Target name" />
          <button type="button">Save target</button>
        </Modal>
      </>,
    );
    expect(screen.getByLabelText('Target name')).toHaveFocus();
    const save = screen.getByRole('button', { name: 'Save target' });
    save.focus();
    fireEvent.keyDown(document, { key: 'Tab' });
    expect(screen.getByRole('button', { name: 'Close Edit target' })).toHaveFocus();
    fireEvent.keyDown(document, { key: 'Tab', shiftKey: true });
    expect(save).toHaveFocus();

    fireEvent.keyDown(document, { key: 'Escape' });
    expect(onClose).toHaveBeenCalledOnce();
    rerender(
      <>
        <button type="button">Open editor</button>
        <Modal isOpen={false} onClose={onClose} title="Edit target">
          <input aria-label="Target name" />
        </Modal>
      </>,
    );
    expect(screen.getByRole('button', { name: 'Open editor' })).toHaveFocus();
  });

  it('preserves the previous body overflow value while open', () => {
    document.body.style.overflow = 'clip';
    const { unmount } = render(<Modal isOpen onClose={() => undefined} title="Details">Content</Modal>);
    expect(document.body).toHaveStyle({ overflow: 'hidden' });
    unmount();
    expect(document.body).toHaveStyle({ overflow: 'clip' });
    document.body.style.overflow = '';
  });

  it('gives confirmation dialogs complete accessible modal semantics', async () => {
    const result = render(
      <ConfirmDialog
        isOpen
        onClose={() => undefined}
        onConfirm={() => undefined}
        title="Disable user"
        message="The user will no longer be able to sign in."
      />,
    );

    const dialog = screen.getByRole('dialog', { name: 'Disable user' });
    expect(dialog).toHaveAttribute('aria-modal', 'true');
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Confirm' })).toBeInTheDocument();
    const accessibility = await axe.run(result.container, {
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(accessibility.violations).toEqual([]);
  });
});
