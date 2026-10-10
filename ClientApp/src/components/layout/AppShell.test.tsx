import { fireEvent, render, screen } from '@testing-library/react';
import { AppShell } from './AppShell';

const app = vi.hoisted(() => ({ sidebarCollapsed: false, toasts: [] }));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('./Sidebar', () => ({
  Sidebar: ({ mobileOpen, onMobileClose }: { mobileOpen: boolean; onMobileClose: () => void }) => (
    <aside data-testid="sidebar" data-mobile-open={mobileOpen}><button onClick={onMobileClose}>Close from sidebar</button></aside>
  ),
}));
vi.mock('./TopBar', () => ({
  TopBar: ({ onOpenNavigation }: { onOpenNavigation: () => void }) => <header><button onClick={onOpenNavigation}>Open from top bar</button></header>,
}));

describe('AppShell responsive navigation', () => {
  it('opens and closes the narrow-layout navigation without reserving desktop margin', () => {
    const { container } = render(<AppShell title="Workspace"><p>Content</p></AppShell>);
    expect(screen.getByTestId('sidebar')).toHaveAttribute('data-mobile-open', 'false');
    const contentShell = container.querySelector('.md\\:ml-64');
    expect(contentShell).toBeInTheDocument();
    expect(contentShell).not.toHaveAttribute('style');

    fireEvent.click(screen.getByRole('button', { name: 'Open from top bar' }));
    expect(screen.getByTestId('sidebar')).toHaveAttribute('data-mobile-open', 'true');
    fireEvent.click(screen.getByRole('button', { name: 'Close navigation' }));
    expect(screen.getByTestId('sidebar')).toHaveAttribute('data-mobile-open', 'false');
  });
});
