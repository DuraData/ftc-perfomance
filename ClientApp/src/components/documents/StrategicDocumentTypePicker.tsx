import { useEffect, useMemo, useState } from 'react';
import { getStrategicDocumentTypesPage } from '../../api/api';
import type { StrategicDocumentType } from '../../types';
import { Button } from '../ui';
import { Input, Select } from '../common/Form';

type Props = {
  value: string;
  onChange: (value: string, option?: StrategicDocumentType) => void;
  selectedLabel?: string;
  disabled?: boolean;
};

const optionLabel = (item: StrategicDocumentType) => `${item.code} · ${item.name}`;

export function StrategicDocumentTypePicker({ value, onChange, selectedLabel, disabled }: Props) {
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<StrategicDocumentType[]>([]);
  const [selectedOption, setSelectedOption] = useState<StrategicDocumentType>();
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string>();

  useEffect(() => {
    const timeout = window.setTimeout(() => { setPage(1); setSearch(searchInput.trim()); }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => { if (!value) setSelectedOption(undefined); }, [value]);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      setIsLoading(true);
      setError(undefined);
      const result = await getStrategicDocumentTypesPage(
        { page, pageSize: 25, search: search || undefined, sortBy: 'displayOrder', sortDirection: 'asc' },
        { active: true },
      );
      if (cancelled) return;
      if (!result.success || !result.data) {
        setItems([]); setTotalCount(0); setTotalPages(0);
        setError(result.message ?? 'Unable to load strategic-document type options.');
      } else {
        setItems(result.data.items); setTotalCount(result.data.totalCount); setTotalPages(result.data.totalPages);
        const matching = result.data.items.find(item => item.publicId === value);
        if (matching) setSelectedOption(matching);
        else if (!value && result.data.items[0]) {
          setSelectedOption(result.data.items[0]);
          onChange(result.data.items[0].publicId, result.data.items[0]);
        }
      }
      setIsLoading(false);
    };
    void load();
    return () => { cancelled = true; };
  }, [onChange, page, search, value]);

  useEffect(() => {
    if (!value || selectedOption?.publicId === value || items.some(item => item.publicId === value)) return;
    let cancelled = false;
    const hydrate = async () => {
      const result = await getStrategicDocumentTypesPage(
        { page: 1, pageSize: 1, sortBy: 'displayOrder', sortDirection: 'asc' },
        { active: true, publicId: value },
      );
      const option = result.data?.items[0];
      if (!cancelled && result.success && option) {
        setSelectedOption(option);
        onChange(value, option);
      }
    };
    void hydrate();
    return () => { cancelled = true; };
  }, [items, onChange, selectedOption, value]);

  const options = useMemo(() => {
    const values = items.map(item => ({ value: item.publicId, label: optionLabel(item) }));
    if (value && !values.some(item => item.value === value)) {
      const retainedLabel = selectedOption?.publicId === value ? optionLabel(selectedOption) : selectedLabel;
      values.unshift({ value, label: retainedLabel || 'Selected strategic-document type' });
    }
    return [{ value: '', label: 'Select strategic-document type' }, ...values];
  }, [items, selectedLabel, selectedOption, value]);

  return <div className="space-y-2">
    <Input label="Strategic document type search" value={searchInput} onChange={event => setSearchInput(event.target.value)} placeholder="Search document types" disabled={disabled} />
    <Select label="Strategic document type" value={value} disabled={disabled || isLoading} options={options} onChange={event => {
      const next = event.target.value;
      const option = items.find(item => item.publicId === next) ?? (selectedOption?.publicId === next ? selectedOption : undefined);
      setSelectedOption(option);
      onChange(next, option);
    }} />
    <div className="flex items-center justify-between gap-2 text-xs text-secondary-500" aria-live="polite">
      <span>{error ?? (isLoading ? 'Loading strategic-document types…' : `${totalCount} matching document type${totalCount === 1 ? '' : 's'}`)}</span>
      <span className="flex items-center gap-2">
        <Button type="button" size="sm" variant="ghost" disabled={disabled || isLoading || page <= 1} onClick={() => setPage(current => Math.max(1, current - 1))}>Previous document types</Button>
        <span>Page {page} of {Math.max(totalPages, 1)}</span>
        <Button type="button" size="sm" variant="ghost" disabled={disabled || isLoading || page >= totalPages} onClick={() => setPage(current => current + 1)}>Next document types</Button>
      </span>
    </div>
  </div>;
}
