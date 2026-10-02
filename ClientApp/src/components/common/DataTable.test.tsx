import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { DataTable } from './DataTable';

describe('DataTable server mode', () => {
  it('delegates search, sort, and page changes without slicing the supplied server page', () => {
    const onPageChange = vi.fn();
    const onSearchChange = vi.fn();
    const onSortChange = vi.fn();

    render(
      <DataTable
        data={[{ id: '26', name: 'Alpha' }, { id: '27', name: 'Beta' }]}
        columns={[{ id: 'name', header: 'Name', accessor: 'name', sortKey: 'targetName' }]}
        searchable
        getRowId={row => row.id}
        serverState={{
          page: 2,
          pageSize: 25,
          totalCount: 60,
          search: 'water',
          sortBy: 'targetName',
          sortDirection: 'asc',
          onPageChange,
          onSearchChange,
          onSortChange,
        }}
      />,
    );

    expect(screen.getByText('Alpha')).toBeInTheDocument();
    expect(screen.getByText('Beta')).toBeInTheDocument();
    expect(screen.getByText('26-27 of 60')).toBeInTheDocument();

    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'roads' } });
    expect(onSearchChange).toHaveBeenCalledWith('roads');

    fireEvent.click(screen.getByRole('button', { name: 'Name' }));
    expect(onSortChange).toHaveBeenCalledWith('targetName', 'desc');

    fireEvent.click(screen.getByRole('button', { name: 'Page 3' }));
    expect(onPageChange).toHaveBeenCalledWith(3);
  });
});
