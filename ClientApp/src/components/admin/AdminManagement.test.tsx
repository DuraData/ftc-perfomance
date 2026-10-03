import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { LookupTables } from './AdminManagement';

vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: ReactNode }) => <>{children}</> }));

describe('LookupTables', () => {
  it('does not expose fixture-backed lookup values or nonfunctional mutations', () => {
    render(<LookupTables />);

    expect(screen.getByText('Governed lookup administration is not configured')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /add item|edit|export/i })).not.toBeInTheDocument();
  });
});
