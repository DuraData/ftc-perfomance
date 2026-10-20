import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import axe from 'axe-core';
import { describe, expect, it, vi } from 'vitest';
import { DataTable } from './DataTable';

describe('DataTable server mode', () => {
  it('publishes local sort state on the owning column header', async () => {
    const user = userEvent.setup();
    render(<DataTable tableLabel="Local targets" data={[{ name: 'Beta' }, { name: 'Alpha' }]} columns={[{ id: 'name', header: 'Name', accessor: 'name' }]} />);

    const header = screen.getByRole('columnheader', { name: 'Name' });
    expect(header).toHaveAttribute('scope', 'col');
    expect(header).toHaveAttribute('aria-sort', 'none');
    await user.click(screen.getByRole('button', { name: 'Name' }));
    expect(header).toHaveAttribute('aria-sort', 'ascending');
    await user.click(screen.getByRole('button', { name: 'Name' }));
    expect(header).toHaveAttribute('aria-sort', 'descending');
    await user.click(screen.getByRole('button', { name: 'Name' }));
    expect(header).toHaveAttribute('aria-sort', 'none');
  });

  it('makes row actions named, keyboard operable, and distinct from nested actions', async () => {
    const user = userEvent.setup();
    const onRowClick = vi.fn();
    const onNestedAction = vi.fn();
    const result = render(
      <DataTable
        tableLabel="Alpha records"
        data={[{ id: '26', name: 'Alpha' }]}
        columns={[{ id: 'name', header: 'Name', accessor: 'name' }]}
        getRowId={row => row.id}
        onRowClick={onRowClick}
        getRowActionLabel={row => `Open ${row.name} details`}
        actions={() => <button type="button" onClick={onNestedAction}>Archive Alpha</button>}
      />,
    );

    const rowAction = screen.getByRole('button', { name: 'Open Alpha details' });
    expect(screen.getByRole('table', { name: 'Alpha records' })).toBeInTheDocument();
    rowAction.focus();
    expect(rowAction).toHaveFocus();
    await user.keyboard('{Enter}');
    expect(onRowClick).toHaveBeenCalledTimes(1);

    screen.getByRole('button', { name: 'Archive Alpha' }).focus();
    await user.keyboard('{Enter}');
    expect(onRowClick).toHaveBeenCalledTimes(1);
    expect(onNestedAction).toHaveBeenCalledTimes(1);
    expect((await axe.run(result.container, { rules: { 'color-contrast': { enabled: false } } })).violations).toEqual([]);
  });

  it('delegates search, sort, and page changes without slicing the supplied server page', () => {
    const onPageChange = vi.fn();
    const onSearchChange = vi.fn();
    const onSortChange = vi.fn();

    render(
      <DataTable
        tableLabel="Server targets"
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

    const nameHeader = screen.getByRole('columnheader', { name: 'Name' });
    expect(nameHeader).toHaveAttribute('aria-sort', 'ascending');
    fireEvent.click(screen.getByRole('button', { name: 'Name' }));
    expect(onSortChange).toHaveBeenCalledWith('targetName', 'desc');
    expect(screen.queryByRole('button', { name: 'Filters' })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Page 3' }));
    expect(onPageChange).toHaveBeenCalledWith(3);
  });
});
