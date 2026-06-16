import React, { useState, useMemo } from 'react';
import {
  ChevronUp,
  ChevronDown,
  ChevronsUpDown,
  ChevronLeft,
  ChevronRight,
  Search,
  Filter,
} from 'lucide-react';
import { Button } from '../ui';

interface Column<T> {
  id: string;
  header: string;
  accessor: keyof T | ((row: T) => React.ReactNode);
  sortable?: boolean;
  className?: string;
  headerClassName?: string;
}

interface DataTableProps<T> {
  data: T[];
  columns: Column<T>[];
  pageSize?: number;
  searchable?: boolean;
  searchPlaceholder?: string;
  onRowClick?: (row: T) => void;
  actions?: (row: T) => React.ReactNode;
  emptyMessage?: string;
  getRowId?: (row: T) => string;
}

type SortDirection = 'asc' | 'desc' | null;

export function DataTable<T>({
  data,
  columns,
  pageSize = 10,
  searchable = false,
  searchPlaceholder = 'Search...',
  onRowClick,
  actions,
  emptyMessage = 'No data available',
  getRowId,
}: DataTableProps<T>) {
  const [currentPage, setCurrentPage] = useState(1);
  const [searchQuery, setSearchQuery] = useState('');
  const [sortColumn, setSortColumn] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<SortDirection>(null);

  const handleSort = (columnId: string) => {
    if (sortColumn === columnId) {
      if (sortDirection === 'asc') setSortDirection('desc');
      else if (sortDirection === 'desc') { setSortColumn(null); setSortDirection(null); }
    } else { setSortColumn(columnId); setSortDirection('asc'); }
  };

  const filteredData = useMemo(() => {
    let result = [...data];
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      result = result.filter(row => columns.some(col => {
        const value = typeof col.accessor === 'function' ? col.accessor(row) : row[col.accessor as keyof T];
        return String(value).toLowerCase().includes(query);
      }));
    }
    if (sortColumn && sortDirection) {
      const column = columns.find(c => c.id === sortColumn);
      if (column) {
        result.sort((a, b) => {
          const aVal = typeof column.accessor === 'function' ? column.accessor(a) : a[column.accessor as keyof T];
          const bVal = typeof column.accessor === 'function' ? column.accessor(b) : b[column.accessor as keyof T];
          const aStr = String(aVal ?? '');
          const bStr = String(bVal ?? '');
          return sortDirection === 'asc' ? aStr.localeCompare(bStr, undefined, { numeric: true }) : bStr.localeCompare(aStr, undefined, { numeric: true });
        });
      }
    }
    return result;
  }, [data, searchQuery, sortColumn, sortDirection, columns]);

  const totalPages = Math.ceil(filteredData.length / pageSize);
  const startIndex = (currentPage - 1) * pageSize;
  const paginatedData = filteredData.slice(startIndex, startIndex + pageSize);

  const getSortIcon = (columnId: string) => {
    if (sortColumn !== columnId) return <ChevronsUpDown className="w-3 h-3 text-secondary-400" />;
    if (sortDirection === 'asc') return <ChevronUp className="w-3 h-3 text-primary-600" />;
    return <ChevronDown className="w-3 h-3 text-primary-600" />;
  };

  return (
    <div className="space-y-2">
      {searchable && (
        <div className="flex items-center gap-2">
          <div className="relative flex-1 max-w-xs">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-secondary-400" />
            <input
              type="text"
              placeholder={searchPlaceholder}
              value={searchQuery}
              onChange={(e) => { setSearchQuery(e.target.value); setCurrentPage(1); }}
              className="w-full pl-8 pr-3 py-1.5 text-xs bg-white dark:bg-secondary-800 border border-secondary-200 dark:border-secondary-700 rounded focus:outline-none focus:ring-1.5 focus:ring-primary-500"
            />
          </div>
          <Button variant="outline" size="sm" icon={<Filter className="w-3.5 h-3.5" />}>Filters</Button>
        </div>
      )}

      <div className="bg-white dark:bg-secondary-900 rounded-lg border border-secondary-200 dark:border-secondary-700 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="bg-secondary-50 dark:bg-secondary-800 border-b border-secondary-200 dark:border-secondary-700">
                {columns.map(column => (
                  <th key={column.id} className={`px-3 py-2 text-left text-[10px] font-semibold text-secondary-600 dark:text-secondary-400 uppercase tracking-wide ${column.headerClassName || ''}`}>
                    {column.sortable !== false ? (
                      <button onClick={() => handleSort(column.id)} className="flex items-center gap-1 hover:text-secondary-900 dark:hover:text-white transition-colors">
                        {column.header}
                        {getSortIcon(column.id)}
                      </button>
                    ) : column.header}
                  </th>
                ))}
                {actions && <th className="px-3 py-2 text-right text-[10px] font-semibold text-secondary-600 dark:text-secondary-400 uppercase">Actions</th>}
              </tr>
            </thead>
            <tbody className="divide-y divide-secondary-200 dark:divide-secondary-700">
              {paginatedData.length === 0 ? (
                <tr><td colSpan={columns.length + (actions ? 1 : 0)} className="px-3 py-8 text-center text-xs text-secondary-500">{emptyMessage}</td></tr>
              ) : (
                paginatedData.map((row, index) => (
                  <tr
                    key={getRowId ? getRowId(row) : index}
                    onClick={() => onRowClick?.(row)}
                    className={`transition-colors ${onRowClick ? 'cursor-pointer hover:bg-secondary-50 dark:hover:bg-secondary-800' : ''}`}
                  >
                    {columns.map(column => (
                      <td key={column.id} className={`px-3 py-2 text-xs text-secondary-700 dark:text-secondary-300 ${column.className || ''}`}>
                        {typeof column.accessor === 'function' ? column.accessor(row) : String(row[column.accessor as keyof T] ?? '')}
                      </td>
                    ))}
                    {actions && (
                      <td className="px-3 py-2 text-right" onClick={(e) => e.stopPropagation()}>
                        <div className="flex items-center justify-end gap-1">{actions(row)}</div>
                      </td>
                    )}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {totalPages > 1 && (
          <div className="flex items-center justify-between px-3 py-2 border-t border-secondary-200 dark:border-secondary-700">
            <p className="text-[10px] text-secondary-600">{startIndex + 1}-{Math.min(startIndex + pageSize, filteredData.length)} of {filteredData.length}</p>
            <div className="flex items-center gap-0.5">
              <button onClick={() => setCurrentPage(p => Math.max(1, p - 1))} disabled={currentPage === 1} className="p-1 rounded hover:bg-secondary-100 dark:hover:bg-secondary-800 disabled:opacity-50">
                <ChevronLeft className="w-3.5 h-3.5 text-secondary-600" />
              </button>
              {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                let pageNum = totalPages <= 5 ? i + 1 : currentPage <= 3 ? i + 1 : currentPage >= totalPages - 2 ? totalPages - 4 + i : currentPage - 2 + i;
                return (
                  <button key={pageNum} onClick={() => setCurrentPage(pageNum)} className={`w-6 h-6 rounded text-xs font-medium ${currentPage === pageNum ? 'bg-primary-600 text-white' : 'text-secondary-600 hover:bg-secondary-100'}`}>
                    {pageNum}
                  </button>
                );
              })}
              <button onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages} className="p-1 rounded hover:bg-secondary-100 dark:hover:bg-secondary-800 disabled:opacity-50">
                <ChevronRight className="w-3.5 h-3.5 text-secondary-600" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
