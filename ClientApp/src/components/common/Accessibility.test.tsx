import { render } from '@testing-library/react';
import axe from 'axe-core';
import { describe, expect, it, vi } from 'vitest';
import { AppShell } from '../layout/AppShell';
import { Checkbox, Input, Select, Textarea } from './Form';

vi.mock('../layout/Sidebar', () => ({ Sidebar: () => <nav aria-label="Primary navigation" /> }));
vi.mock('../layout/TopBar', () => ({ TopBar: ({ title }: { title?: string }) => <header><h1>{title}</h1></header> }));
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
});
