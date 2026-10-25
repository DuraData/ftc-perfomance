import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import axe from 'axe-core';
import { afterEach } from 'vitest';
import { AppShell, WorkspaceShell } from './AppShell';

const app = vi.hoisted(() => ({ sidebarCollapsed: false, toasts: [] }));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('./Sidebar', () => ({
  Sidebar: ({ mobileOpen, onMobileClose, mobileNavigationRef }: { mobileOpen: boolean; onMobileClose: () => void; mobileNavigationRef: React.Ref<HTMLDivElement> }) => (
    <div ref={mobileNavigationRef} id="mobile-navigation-dialog" role={mobileOpen ? 'dialog' : 'complementary'} aria-label="Primary navigation" data-testid="sidebar" data-mobile-open={mobileOpen}>
      <button type="button" onClick={onMobileClose}>Close from sidebar</button>
      <button type="button">Last navigation item</button>
    </div>
  ),
}));
vi.mock('./TopBar', () => ({
  TopBar: ({ title, onOpenNavigation, navigationOpen, navigationTriggerRef }: { title?: string; onOpenNavigation: () => void; navigationOpen: boolean; navigationTriggerRef: React.Ref<HTMLButtonElement> }) => <header><button ref={navigationTriggerRef} type="button" aria-expanded={navigationOpen} aria-controls="mobile-navigation-dialog" aria-haspopup="dialog" onClick={onOpenNavigation}>Open from top bar</button>{title && <h1 id="page-title">{title}</h1>}</header>,
}));

describe('AppShell responsive navigation', () => {
  afterEach(() => {
    cleanup();
    document.title = '';
  });

  it('opens a keyboard-contained narrow-layout navigation and restores its trigger', async () => {
    const { container } = render(<AppShell title="Workspace"><p>Content</p></AppShell>);
    expect(screen.getByTestId('sidebar')).toHaveAttribute('data-mobile-open', 'false');
    const contentShell = container.querySelector('.md\\:ml-64');
    expect(contentShell).toBeInTheDocument();
    expect(contentShell).not.toHaveAttribute('style');

    const trigger = screen.getByRole('button', { name: 'Open from top bar' });
    trigger.focus();
    fireEvent.click(trigger);
    expect(screen.getByTestId('sidebar')).toHaveAttribute('data-mobile-open', 'true');
    const dialog = screen.getByRole('dialog', { name: 'Primary navigation' });
    expect(trigger).toHaveAttribute('aria-expanded', 'true');
    expect(trigger).toHaveAttribute('aria-controls', dialog.id);
    expect(trigger).toHaveAttribute('aria-haspopup', 'dialog');
    expect(contentShell).toHaveAttribute('aria-hidden', 'true');
    expect(container.querySelector('[role="region"][aria-label="Notifications"]')).toHaveAttribute('aria-hidden', 'true');
    expect(document.body).toHaveStyle({ overflow: 'hidden' });
    const first = screen.getByRole('button', { name: 'Close from sidebar' });
    const last = screen.getByRole('button', { name: 'Last navigation item' });
    await waitFor(() => expect(first).toHaveFocus());

    const openAccessibility = await axe.run(container, { rules: { 'color-contrast': { enabled: false } } });
    expect(openAccessibility.violations).toEqual([]);

    last.focus();
    fireEvent.keyDown(document, { key: 'Tab' });
    expect(first).toHaveFocus();
    fireEvent.keyDown(document, { key: 'Tab', shiftKey: true });
    expect(last).toHaveFocus();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.getByTestId('sidebar')).toHaveAttribute('data-mobile-open', 'false');
    expect(trigger).toHaveFocus();
    expect(document.body).not.toHaveStyle({ overflow: 'hidden' });

    const accessibility = await axe.run(container, { rules: { 'color-contrast': { enabled: false } } });
    expect(accessibility.violations).toEqual([]);
  });

  it('gives each workspace a descriptive document title and named main landmark', () => {
    const { rerender } = render(<AppShell title="SDBIP import and reconciliation"><p>Content</p></AppShell>);

    expect(document.title).toBe('SDBIP import and reconciliation | OPMS');
    expect(screen.getByRole('main', { name: 'SDBIP import and reconciliation' })).toHaveAttribute('aria-labelledby', 'page-title');

    rerender(<AppShell title="OPMS Dashboard"><p>Content</p></AppShell>);

    expect(document.title).toBe('OPMS Dashboard | OPMS');
    expect(screen.getByRole('main', { name: 'OPMS Dashboard' })).toBeInTheDocument();
  });

  it('keeps application chrome mounted while route content and page chrome change', async () => {
    const { rerender } = render(
      <WorkspaceShell><AppShell title="First workspace"><p>First content</p></AppShell></WorkspaceShell>,
    );
    const sidebar = screen.getByTestId('sidebar');
    await waitFor(() => expect(screen.getByRole('main', { name: 'First workspace' })).toBeInTheDocument());

    rerender(
      <WorkspaceShell><AppShell title="Second workspace"><p>Second content</p></AppShell></WorkspaceShell>,
    );

    await waitFor(() => expect(screen.getByRole('main', { name: 'Second workspace' })).toBeInTheDocument());
    expect(screen.getByTestId('sidebar')).toBe(sidebar);
    expect(screen.queryByText('First content')).not.toBeInTheDocument();
    expect(screen.getByText('Second content')).toBeInTheDocument();
  });
});
