import { render } from '@testing-library/react';
import axe from 'axe-core';
import { describe, expect, it, vi } from 'vitest';
import { AppShell } from '../layout/AppShell';
import { NotFoundPage } from '../security/AccessControl';
import { Checkbox, Input, Select, Textarea } from './Form';
import { Accordion, TabPanel, Tabs } from './Tabs';

vi.mock('../layout/Sidebar', () => ({ Sidebar: () => <nav aria-label="Primary navigation" /> }));
vi.mock('../layout/TopBar', () => ({ TopBar: ({ title }: { title?: string }) => <header><h1 id="page-title">{title}</h1></header> }));
vi.mock('../../context/AppContext', () => ({
  useApp: () => ({ sidebarCollapsed: false, toasts: [{ id: 'notice', type: 'success', message: 'Saved' }] }),
}));

async function expectNoAutomatedViolations(container: HTMLElement) {
  const result = await axe.run(container, {
    rules: {
      // JSDOM cannot calculate CSS layout or rendered colour contrast.
      'color-contrast': { enabled: false },
    },
  });
  expect(result.violations).toEqual([]);
}

describe('shared accessibility baseline', () => {
  it('provides landmarks, a keyboard skip link, and announced notifications', async () => {
    const { container, getByRole, getByText } = render(<AppShell title="Governed workspace"><p>Content</p></AppShell>);

    expect(getByText('Skip to main content')).toHaveAttribute('href', '#main-content');
    expect(getByRole('main')).toHaveAttribute('id', 'main-content');
    expect(getByRole('main', { name: 'Governed workspace' })).toHaveAttribute('aria-labelledby', 'page-title');
    expect(document.title).toBe('Governed workspace | OPMS');
    expect(getByRole('status')).toHaveTextContent('Saved');
    await expectNoAutomatedViolations(container);
  });

  it('associates labels, validation messages, help text, and descriptions', async () => {
    const { container, getByLabelText } = render(
      <form aria-label="Accessible form">
        <Input label="Indicator name" error="Indicator name is required" required />
        <Select label="Reporting period" error="Select a period" options={[{ value: 'Q1', label: 'Quarter 1' }]} />
        <Textarea label="Comment" helpText="Explain the change" />
        <Checkbox label="Published" description="Makes this visible to authorised readers" />
      </form>,
    );

    expect(getByLabelText(/Indicator name/)).toHaveAttribute('aria-invalid', 'true');
    expect(getByLabelText('Reporting period')).toHaveAccessibleDescription('Select a period');
    expect(getByLabelText('Comment')).toHaveAccessibleDescription('Explain the change');
    expect(getByLabelText('Published')).toHaveAccessibleDescription('Makes this visible to authorised readers');
    await expectNoAutomatedViolations(container);
  });

  it('renders an honest accessible not-found workspace instead of substitute content', async () => {
    const { container, getByRole, getByText } = render(<NotFoundPage />);

    expect(getByRole('main', { name: 'Page Not Found' })).toBeInTheDocument();
    expect(getByText('The address does not match a registered OPMS workspace. No substitute page was loaded.')).toBeInTheDocument();
    expect(document.title).toBe('Page Not Found | OPMS');
    await expectNoAutomatedViolations(container);
  });

  it('associates a named tab set with its selected panel', async () => {
    const { container, getByRole } = render(<>
      <Tabs
        id="accessibility-sections"
        ariaLabel="Accessible sections"
        tabs={[{ id: 'summary', label: 'Summary' }, { id: 'history', label: 'History' }]}
        activeTab="summary"
        onChange={vi.fn()}
      />
      <TabPanel tabSetId="accessibility-sections" tabId="summary">Summary content</TabPanel>
    </>);

    expect(getByRole('tab', { name: 'Summary' })).toHaveAttribute('aria-controls', 'accessibility-sections-summary-panel');
    expect(getByRole('tabpanel', { name: 'Summary' })).toHaveAttribute('aria-labelledby', 'accessibility-sections-summary-tab');
    await expectNoAutomatedViolations(container);
  });

  it('announces disclosure state and associates accordion triggers with their panels', async () => {
    const { container, getByRole } = render(<Accordion items={[
      { id: 'definition', title: 'Definition', content: <p>Governed definition</p>, defaultOpen: true },
      { id: 'history', title: 'History', content: <p>Governed history</p> },
    ]} />);

    expect(getByRole('button', { name: 'Definition' })).toHaveAttribute('aria-expanded', 'true');
    expect(getByRole('region', { name: 'Definition' })).toBeInTheDocument();
    expect(getByRole('button', { name: 'History' })).toHaveAttribute('aria-expanded', 'false');
    await expectNoAutomatedViolations(container);
  });
});
