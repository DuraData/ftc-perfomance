import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { TaskManagement } from './TaskManagement';

vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: ReactNode }) => <>{children}</> }));

describe('TaskManagement', () => {
  it('does not expose fixture-backed or nonfunctional task actions', () => {
    render(<TaskManagement />);

    expect(screen.getByText('Task management is not enabled')).toBeInTheDocument();
    expect(screen.getByText(/No governed task service is configured/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /new task|create|edit/i })).not.toBeInTheDocument();
  });
});
