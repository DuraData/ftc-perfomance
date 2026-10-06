import { useEffect, useMemo, useState } from 'react';
import { getStrategicClassificationPage, type StrategicClassificationKind } from '../../api/api';
import type { StrategicCatalogueItemDto } from '../../types';
import { Button } from '../ui';
import { Input, Select } from './Form';

type Props = {
  targetKind: 'opms' | 'ipms';
  classificationKind: StrategicClassificationKind;
  municipalityFinancialYearPublicId: string;
  label: string;
  value: string;
  onChange: (value: string, option?: StrategicCatalogueItemDto) => void;
  selectedLabel?: string | null;
  parentPublicId?: string;
  relationshipType?: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
};

const nouns: Record<StrategicClassificationKind, string> = {
  'national-kpas': 'national KPA',
  'municipal-kpas': 'municipal KPA',
  'back-to-basics-pillars': 'Back-to-Basics pillar',
  'strategic-goals': 'strategic goal',
  'strategic-interventions': 'strategic intervention',
  'strategic-objectives': 'strategic objective',
  'performance-objectives': 'performance objective',
  'budget-sources': 'budget source',
  'budget-types': 'budget type',
  'kpi-types': 'KPI type',
  'indicator-types': 'indicator type',
  'functional-areas': 'functional area',
  'standard-classifications': 'standard classification',
  'kpi-units-of-measure': 'unit of measure',
};

function optionLabel(item: StrategicCatalogueItemDto) {
  return `${item.code ? `${item.code} · ` : ''}${item.name}${item.symbol ? ` (${item.symbol})` : ''}`;
}

export function StrategicClassificationPicker({
  targetKind,
  classificationKind,
  municipalityFinancialYearPublicId,
  label,
  value,
  onChange,
  selectedLabel,
  parentPublicId,
  relationshipType,
  required,
  error: fieldError,
  disabled,
}: Props) {
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<StrategicCatalogueItemDto[]>([]);
  const [selectedOption, setSelectedOption] = useState<StrategicCatalogueItemDto>();
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState<string>();
  const noun = nouns[classificationKind];

  useEffect(() => {
    const timeout = window.setTimeout(() => { setPage(1); setSearch(searchInput.trim()); }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => { setPage(1); }, [classificationKind, municipalityFinancialYearPublicId, parentPublicId, relationshipType, targetKind]);
  useEffect(() => { if (!value) setSelectedOption(undefined); }, [value]);

  useEffect(() => {
    let cancelled = false;
    if (!municipalityFinancialYearPublicId || (relationshipType && !parentPublicId)) {
      setItems([]); setTotalCount(0); setTotalPages(0); setLoadError(undefined); setIsLoading(false);
      return () => { cancelled = true; };
    }
    const load = async () => {
      setIsLoading(true);
      setLoadError(undefined);
      const result = await getStrategicClassificationPage(
        targetKind,
        municipalityFinancialYearPublicId,
        classificationKind,
        { page, pageSize: 25, search: search || undefined, sortBy: 'name', sortDirection: 'asc' },
        { parentPublicId, relationshipType },
      );
      if (cancelled) return;
      if (!result.success || !result.data) {
        setItems([]); setTotalCount(0); setTotalPages(0); setLoadError(result.message ?? `Unable to load ${noun} options.`);
      } else {
        setItems(result.data.items); setTotalCount(result.data.totalCount); setTotalPages(result.data.totalPages);
        const matching = result.data.items.find(item => item.publicId === value);
        if (matching) setSelectedOption(matching);
      }
      setIsLoading(false);
    };
    void load();
    return () => { cancelled = true; };
  }, [classificationKind, municipalityFinancialYearPublicId, noun, page, parentPublicId, relationshipType, search, targetKind, value]);

  const options = useMemo(() => {
    const values = items.map(item => ({ value: item.publicId, label: optionLabel(item) }));
    if (value && !values.some(item => item.value === value)) {
      const retainedLabel = selectedOption?.publicId === value ? optionLabel(selectedOption) : selectedLabel;
      values.unshift({ value, label: retainedLabel || `Selected ${noun}` });
    }
    return [{ value: '', label: `Select ${noun}` }, ...values];
  }, [items, noun, selectedLabel, selectedOption, value]);

  const hierarchyParentMissing = Boolean(relationshipType && !parentPublicId);
  return <div className="space-y-2">
    <Input label={`${label} search`} value={searchInput} onChange={event => setSearchInput(event.target.value)} placeholder={`Search ${noun}s`} disabled={disabled || !municipalityFinancialYearPublicId || hierarchyParentMissing} />
    <Select label={label} value={value} required={required} error={fieldError} disabled={disabled || isLoading || !municipalityFinancialYearPublicId || hierarchyParentMissing} options={options} onChange={event => {
      const next = event.target.value;
      const option = items.find(item => item.publicId === next) ?? (selectedOption?.publicId === next ? selectedOption : undefined);
      if (option) setSelectedOption(option);
      onChange(next, option);
    }} />
    <div className="flex items-center justify-between gap-2 text-xs text-secondary-500" aria-live="polite">
      <span>{hierarchyParentMissing ? `Select the parent before choosing a ${noun}.` : loadError ?? (isLoading ? `Loading ${noun}s…` : `${totalCount} matching ${noun}${totalCount === 1 ? '' : 's'}`)}</span>
      <span className="flex items-center gap-2">
        <Button type="button" size="sm" variant="ghost" disabled={disabled || isLoading || page <= 1} onClick={() => setPage(current => Math.max(1, current - 1))}>Previous</Button>
        <span>Page {page} of {Math.max(totalPages, 1)}</span>
        <Button type="button" size="sm" variant="ghost" disabled={disabled || isLoading || page >= totalPages} onClick={() => setPage(current => current + 1)}>Next</Button>
      </span>
    </div>
  </div>;
}
