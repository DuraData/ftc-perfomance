import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { TabPanel, Tabs } from './Tabs';

const tabs = [
  { id: 'overview', label: 'Overview' },
  { id: 'restricted', label: 'Restricted', disabled: true },
  { id: 'history', label: 'History' },
  { id: 'audit', label: 'Audit' },
];

function TabsHarness({ variant = 'default' }: { variant?: 'default' | 'pills' | 'underline' | 'compact' }) {
  const [activeTab, setActiveTab] = useState('overview');
  return <>
    <Tabs id="kpi-workspace" ariaLabel="KPI workspace" tabs={tabs} activeTab={activeTab} onChange={setActiveTab} variant={variant} />
    <TabPanel tabSetId="kpi-workspace" tabId={activeTab}>{activeTab} content</TabPanel>
  </>;
}

describe('Tabs', () => {
  it.each(['default', 'pills', 'underline', 'compact'] as const)('exposes tab semantics for the %s variant', (variant) => {
    render(<TabsHarness variant={variant} />);

    expect(screen.getByRole('tablist', { name: 'KPI workspace' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Overview' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Overview' })).toHaveAttribute('tabindex', '0');
    expect(screen.getByRole('tab', { name: 'Overview' })).toHaveAttribute('aria-controls', 'kpi-workspace-overview-panel');
    expect(screen.getByRole('tab', { name: 'History' })).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByRole('tab', { name: 'History' })).toHaveAttribute('tabindex', '-1');
    expect(screen.getByRole('tabpanel', { name: 'Overview' })).toHaveAttribute('id', 'kpi-workspace-overview-panel');
  });

  it('supports wrapped arrow navigation, Home and End while skipping disabled tabs', async () => {
    const user = userEvent.setup();
    render(<TabsHarness />);
    const overview = screen.getByRole('tab', { name: 'Overview' });

    overview.focus();
    await user.keyboard('{ArrowRight}');
    expect(screen.getByRole('tab', { name: 'History' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'History' })).toHaveFocus();

    await user.keyboard('{End}');
    expect(screen.getByRole('tab', { name: 'Audit' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Audit' })).toHaveFocus();

    await user.keyboard('{ArrowRight}');
    expect(overview).toHaveAttribute('aria-selected', 'true');
    expect(overview).toHaveFocus();

    await user.keyboard('{ArrowLeft}');
    expect(screen.getByRole('tab', { name: 'Audit' })).toHaveFocus();

    await user.keyboard('{Home}');
    expect(overview).toHaveFocus();
    expect(screen.getByRole('tab', { name: 'Restricted' })).toBeDisabled();
  });

  it('selects a tab by pointer and maintains one tab stop', async () => {
    const user = userEvent.setup();
    render(<TabsHarness />);

    await user.click(screen.getByRole('tab', { name: 'History' }));

    expect(screen.getByRole('tab', { name: 'History' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'History' })).toHaveAttribute('tabindex', '0');
    expect(screen.getByRole('tab', { name: 'Overview' })).toHaveAttribute('tabindex', '-1');
  });
});
