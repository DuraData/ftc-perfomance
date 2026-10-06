import { useEffect, useMemo, useState } from 'react';
import { getStrategicPlanningMastersPage, type StrategicPlanningMasterKind } from '../../api/api';
import type { StrategicPlanningMasterDto } from '../../types';
import { Button } from '../ui';
import { Input, Select } from './Form';

export type StrategicRelationshipMasterKind = Extract<StrategicPlanningMasterKind,
  'municipal-kpas' | 'strategic-goals' | 'strategic-interventions' | 'strategic-objectives' | 'performance-objectives'>;

type Props = {
  kind: StrategicRelationshipMasterKind;
  label: string;
  value: string;
  onChange: (value: string, option?: StrategicPlanningMasterDto) => void;
  selectedLabel?: string;
  disabled?: boolean;
};

const nouns: Record<StrategicRelationshipMasterKind, string> = {
  'municipal-kpas': 'municipal KPA',
  'strategic-goals': 'strategic goal',
  'strategic-interventions': 'strategic intervention',
  'strategic-objectives': 'strategic objective',
  'performance-objectives': 'performance objective',
};

function optionLabel(item: StrategicPlanningMasterDto) {
  return `${item.code ? `${item.code} · ` : ''}${item.name}`;
}

export function StrategicPlanningMasterPicker({ kind, label, value, onChange, selectedLabel, disabled }: Props) {
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<StrategicPlanningMasterDto[]>([]);
  const [selectedOption, setSelectedOption] = useState<StrategicPlanningMasterDto>();
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string>();
  const noun = nouns[kind];

  useEffect(() => {
    const timeout = window.setTimeout(() => { setPage(1); setSearch(searchInput.trim()); }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => { setPage(1); }, [kind]);
  useEffect(() => { if (!value) setSelectedOption(undefined); }, [value]);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      setIsLoading(true);
      setError(undefined);
      const result = await getStrategicPlanningMastersPage(
        kind,
        { page, pageSize: 25, search: search || undefined, sortBy: 'name', sortDirection: 'asc' },
      );
      if (cancelled) return;
      if (!result.success || !result.data) {
        setItems([]); setTotalCount(0); setTotalPages(0); setError(result.message ?? `Unable to load ${noun} options.`);
      } else {
        setItems(result.data.items); setTotalCount(result.data.totalCount); setTotalPages(result.data.totalPages);
        const matching = result.data.items.find(item => item.publicId === value);
        if (matching) setSelectedOption(matching);
      }
      setIsLoading(false);
    };
    void load();
    return () => { cancelled = true; };
  }, [kind, noun, page, search, value]);

  const options = useMemo(() => {
    const values = items.map(item => ({ value: item.publicId, label: optionLabel(item) }));
    if (value && !values.some(item => item.value === value)) {
      const retainedLabel = selectedOption?.publicId === value ? optionLabel(selectedOption) : selectedLabel;
      values.unshift({ value, label: retainedLabel || `Selected ${noun}` });
    }
    return [{ value: '', label: `Select ${noun}` }, ...values];
  }, [items, noun, selectedLabel, selectedOption, value]);

  return <div className="space-y-2">
    <Input label={`${label} search`} value={searchInput} onChange={event => setSearchInput(event.target.value)} placeholder={`Search ${noun}s`} disabled={disabled} />
    <Select label={label} value={value} disabled={disabled || isLoading} options={options} onChange={event => {
      const next = event.target.value;
      const option = items.find(item => item.publicId === next) ?? (selectedOption?.publicId === next ? selectedOption : undefined);
      if (option) setSelectedOption(option);
      onChange(next, option);
    }} />
    <div className="flex items-center justify-between gap-2 text-xs text-secondary-500" aria-live="polite">
      <span>{error ?? (isLoading ? `Loading ${noun}s…` : `${totalCount} matching ${noun}${totalCount === 1 ? '' : 's'}`)}</span>
      <span className="flex items-center gap-2">
        <Button type="button" size="sm" variant="ghost" disabled={disabled || isLoading || page <= 1} onClick={() => setPage(current => Math.max(1, current - 1))}>Previous {label.toLowerCase()}</Button>
        <span>Page {page} of {Math.max(totalPages, 1)}</span>
        <Button type="button" size="sm" variant="ghost" disabled={disabled || isLoading || page >= totalPages} onClick={() => setPage(current => current + 1)}>Next {label.toLowerCase()}</Button>
      </span>
    </div>
  </div>;
}
