import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { LookupTables } from './AdminManagement';

const setCurrentPath = vi.fn();
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ setCurrentPath }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canRead: (resource: string) => resource === 'KPI_UNIT_OF_MEASURE' }) }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: ReactNode }) => <>{children}</> }));

describe('LookupTables', () => {
  it('shows only governed registers allowed by dynamic read permissions', () => {
    render(<LookupTables />);
    expect(screen.getByText('KPI Units of Measure')).toBeInTheDocument();
    expect(screen.queryByText('Budget Types')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Open' }));
    expect(setCurrentPath).toHaveBeenCalledWith('/admin/units-measure');
  });
});
