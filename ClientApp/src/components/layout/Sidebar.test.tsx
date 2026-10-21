import { fireEvent, render, screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Sidebar } from './Sidebar';

const app = vi.hoisted(() => ({
  sidebarCollapsed: false,
  toggleSidebar: vi.fn(),
  expandedSidebarGroups: ['Operations'] as string[],
  toggleSidebarGroup: vi.fn(),
  expandSidebarGroup: vi.fn(),
  setCurrentPath: vi.fn(),
  currentPath: '/operations/queue',
  userProfile: null,
  menuItems: [
    { label: 'Dashboard', path: '/', icon: 'dashboard', isDivider: false, code: 'NAV.DASHBOARD' },
    {
      label: 'Operations', icon: 'workflow', isDivider: false, code: 'NAV.OPERATIONS',
      children: [
        { label: 'Queue', path: '/operations/queue', icon: 'workflow', isDivider: false, code: 'NAV.OPERATIONS.QUEUE' },
      ],
    },
    {
      label: 'IDP', icon: 'map', isDivider: false, code: 'NAV.IDP',
      children: [
        { label: 'IDP Plans', path: '/idp/plans', icon: 'map', isDivider: false, code: 'NAV.IDP.PLANS' },
      ],
    },
  ],
  logout: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));

describe('Sidebar navigation semantics', () => {
  beforeEach(() => {
    app.sidebarCollapsed = false;
    app.expandedSidebarGroups = ['Operations'];
    app.currentPath = '/operations/queue';
    vi.clearAllMocks();
  });

  it('associates expanded groups with their submenu and identifies the current page', () => {
    render(<Sidebar />);

    const trigger = screen.getByRole('button', { name: 'Operations' });
    const submenu = screen.getByRole('group', { name: 'Operations submenu' });
    expect(trigger).toHaveAttribute('aria-expanded', 'true');
    expect(trigger).toHaveAttribute('aria-controls', submenu.id);
    expect(within(submenu).getByRole('button', { name: 'Queue' })).toHaveAttribute('aria-current', 'page');

    fireEvent.click(trigger);
    expect(app.toggleSidebarGroup).toHaveBeenCalledWith('Operations');
  });

  it('gives icon-only destinations accessible names when navigation is collapsed', () => {
    app.sidebarCollapsed = true;
    app.expandedSidebarGroups = [];
    app.currentPath = '/';

    render(<Sidebar />);

    expect(screen.getByRole('button', { name: 'Dashboard' })).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('button', { name: 'Operations' })).not.toHaveAttribute('aria-expanded');
    expect(screen.getByRole('button', { name: 'Expand navigation' })).toHaveAttribute('aria-expanded', 'false');
  });

  it('marks the canonical navigation destination current for a registered compatibility route', () => {
    app.currentPath = '/idp/overview';
    app.expandedSidebarGroups = ['IDP'];

    render(<Sidebar />);

    const submenu = screen.getByRole('group', { name: 'IDP submenu' });
    expect(within(submenu).getByRole('button', { name: 'IDP Plans' })).toHaveAttribute('aria-current', 'page');
  });
});
