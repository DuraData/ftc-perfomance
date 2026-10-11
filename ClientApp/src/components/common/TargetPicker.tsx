import { useEffect, useState } from 'react';
import { getIpmsTarget, getIpmsTargetOptions, getOpmsTarget, getOpmsTargetOptions } from '../../api/api';
import type { PerformanceTargetOptionDto } from '../../types';
import { Button } from '../ui';
import { Input, Select } from './Form';

type TargetPickerProps = {
  kind: 'opms' | 'ipms';
  label: string;
  value: string;
  onChange: (value: string, option?: PerformanceTargetOptionDto) => void;
  emptyLabel?: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
  relatedOpmsTargetPublicId?: string;
};

export function TargetPicker({
  kind,
  label,
  value,
  onChange,
  emptyLabel = 'Select target',
  required,
  error: validationError,
  disabled,
  relatedOpmsTargetPublicId,
}: TargetPickerProps) {
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<PerformanceTargetOptionDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPage(1);
      setSearch(searchInput.trim());
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      setIsLoading(true);
      setError(null);
      const query = { page, pageSize: 25, search, sortBy: 'indicatorNumber', sortDirection: 'asc' as const, relatedOpmsTargetPublicId };
      const result = kind === 'opms' ? await getOpmsTargetOptions(query) : await getIpmsTargetOptions(query);
      if (cancelled) return;
      if (!result.success || !result.data) {
        setItems([]);
        setTotalCount(0);
        setTotalPages(0);
        setError(result.message ?? `Unable to load ${kind.toUpperCase()} targets.`);
        setIsLoading(false);
        return;
      }

      const nextItems = [...result.data.items];
      if (value && !nextItems.some(item => item.publicId === value)) {
        const selected = kind === 'opms' ? await getOpmsTarget(value) : await getIpmsTarget(value);
        if (!cancelled && selected.success && selected.data && !selected.data.isWithdrawn) {
          nextItems.unshift({
            publicId: selected.data.id,
            indicatorNumber: selected.data.indicatorNumber,
            targetName: selected.data.targetName,
            departmentPublicId: selected.data.department?.publicId ?? null,
            departmentName: selected.data.department?.name ?? null,
          });
        }
      }
      if (cancelled) return;
      setItems(nextItems);
      setTotalCount(result.data.totalCount);
      setTotalPages(result.data.totalPages);
      setIsLoading(false);
    };
    void load();
    return () => { cancelled = true; };
  }, [kind, page, relatedOpmsTargetPublicId, search, value]);

  const selectOptions = [
    { value: '', label: emptyLabel },
    ...items.map(item => ({
      value: item.publicId,
      label: `${item.indicatorNumber} · ${item.targetName}${item.departmentName ? ` · ${item.departmentName}` : ''}`,
    })),
  ];

  return (
    <div className="space-y-2">
      <Input
        label={`${label} search`}
        value={searchInput}
        onChange={event => setSearchInput(event.target.value)}
        placeholder="Search indicator number, name or description"
        disabled={disabled}
      />
      <Select
        label={label}
        value={value}
        required={required}
        error={validationError}
        disabled={disabled || isLoading}
        options={selectOptions}
        onChange={event => {
          const selectedValue = event.target.value;
          onChange(selectedValue, items.find(item => item.publicId === selectedValue));
        }}
      />
      <div className="flex items-center justify-between gap-2 text-xs text-secondary-500" aria-live="polite">
        <span>{error ?? (isLoading ? 'Loading target options…' : `${totalCount} matching target${totalCount === 1 ? '' : 's'}`)}</span>
        <span className="flex items-center gap-2">
          <Button type="button" size="sm" variant="ghost" disabled={disabled || isLoading || page <= 1} onClick={() => setPage(current => Math.max(1, current - 1))}>Previous</Button>
          <span>Page {page} of {Math.max(totalPages, 1)}</span>
          <Button type="button" size="sm" variant="ghost" disabled={disabled || isLoading || page >= totalPages} onClick={() => setPage(current => current + 1)}>Next</Button>
        </span>
      </div>
    </div>
  );
}
