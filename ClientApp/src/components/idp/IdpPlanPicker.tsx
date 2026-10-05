import { useEffect, useMemo, useRef, useState } from 'react';
import { getIdpPlansPage } from '../../api/api';
import type { IdpPlanSummary } from '../../types';
import { Button } from '../ui';
import { Input, Select } from '../common/Form';

type Props = {
  label: string;
  value: string;
  onChange: (value: string, plan?: IdpPlanSummary) => void;
  valueField?: 'id' | 'publicId';
  emptyLabel?: string;
  autoSelectFirst?: boolean;
  disabled?: boolean;
  refreshKey?: string | number;
};

const optionValue = (plan: IdpPlanSummary, field: 'id' | 'publicId') => field === 'id' ? String(plan.id) : plan.publicId;
const optionLabel = (plan: IdpPlanSummary) => `${plan.planCode} - ${plan.planTitle}`;

export function IdpPlanPicker({
  label,
  value,
  onChange,
  valueField = 'id',
  emptyLabel = 'Select an IDP plan',
  autoSelectFirst = false,
  disabled,
  refreshKey,
}: Props) {
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<IdpPlanSummary[]>([]);
  const [selectedPlan, setSelectedPlan] = useState<IdpPlanSummary>();
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();
  const onChangeRef = useRef(onChange);

  useEffect(() => { onChangeRef.current = onChange; }, [onChange]);
  useEffect(() => {
    const nextSearch = searchInput.trim();
    if (nextSearch === search) return;
    const timeout = window.setTimeout(() => { setPage(1); setSearch(nextSearch); }, 300);
    return () => window.clearTimeout(timeout);
  }, [search, searchInput]);
  useEffect(() => { if (!value) setSelectedPlan(undefined); }, [value]);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      setLoading(true); setError(undefined);
      const result = await getIdpPlansPage({ page, pageSize: 25, search: search || undefined, sortBy: 'createdAt', sortDirection: 'desc' });
      if (cancelled) return;
      if (!result.success || !result.data) {
        setItems([]); setTotalCount(0); setTotalPages(0); setError(result.message ?? 'Unable to load IDP plan options.');
      } else {
        setItems(result.data.items); setTotalCount(result.data.totalCount); setTotalPages(result.data.totalPages);
      }
      setLoading(false);
    };
    void load();
    return () => { cancelled = true; };
  }, [page, refreshKey, search]);

  useEffect(() => {
    const matching = items.find(item => optionValue(item, valueField) === value);
    if (matching) setSelectedPlan(matching);
  }, [items, value, valueField]);

  useEffect(() => {
    if (!value && autoSelectFirst && items[0]) {
      const first = items[0];
      setSelectedPlan(first);
      onChangeRef.current(optionValue(first, valueField), first);
    }
  }, [autoSelectFirst, items, value, valueField]);

  const options = useMemo(() => {
    const result = items.map(item => ({ value: optionValue(item, valueField), label: optionLabel(item) }));
    if (value && !result.some(item => item.value === value)) {
      result.unshift({ value, label: selectedPlan ? optionLabel(selectedPlan) : 'Selected IDP plan' });
    }
    return [{ value: '', label: emptyLabel }, ...result];
  }, [emptyLabel, items, selectedPlan, value, valueField]);

  return <div className="min-w-72 space-y-2">
    <Input label={`${label} search`} value={searchInput} onChange={event => setSearchInput(event.target.value)} placeholder="Search IDP plans" disabled={disabled} />
    <Select label={label} value={value} options={options} disabled={disabled || loading} onChange={event => {
      const next = event.target.value;
      const plan = items.find(item => optionValue(item, valueField) === next) ?? (selectedPlan && optionValue(selectedPlan, valueField) === next ? selectedPlan : undefined);
      if (plan) setSelectedPlan(plan);
      onChange(next, plan);
    }} />
    <div className="flex items-center justify-between gap-2 text-xs text-secondary-500" aria-live="polite">
      <span>{error ?? (loading ? 'Loading IDP plans…' : `${totalCount} matching plan${totalCount === 1 ? '' : 's'}`)}</span>
      <span className="flex items-center gap-2"><Button type="button" size="sm" variant="ghost" disabled={disabled || loading || page <= 1} onClick={() => setPage(current => Math.max(1, current - 1))}>Previous plans</Button><span>Page {page} of {Math.max(totalPages, 1)}</span><Button type="button" size="sm" variant="ghost" disabled={disabled || loading || page >= totalPages} onClick={() => setPage(current => current + 1)}>Next plans</Button></span>
    </div>
  </div>;
}
