import { useEffect, useMemo, useState } from 'react';
import {
  getFinancialYearMastersPage,
  getMunicipalityFinancialYearMastersPage,
  getReportingPeriodMastersPage,
  getSdbipLayerMastersPage,
  type CalendarMasterPageQuery,
} from '../../api/api';
import type {
  FinancialYearMasterDto,
  MunicipalityFinancialYearMasterDto,
  ReportingPeriodMasterDto,
  SdbipLayerMasterDto,
} from '../../types';
import { Button } from '../ui';
import { Input, Select } from './Form';

export type CalendarMasterKind = 'financial-year' | 'municipality-financial-year' | 'reporting-period' | 'sdbip-layer';
export type CalendarMasterOption = FinancialYearMasterDto | MunicipalityFinancialYearMasterDto | ReportingPeriodMasterDto | SdbipLayerMasterDto;

type Props = {
  kind: CalendarMasterKind;
  label: string;
  value: string;
  onChange: (value: string, option?: CalendarMasterOption) => void;
  emptyLabel?: string;
  required?: boolean;
  disabled?: boolean;
  activeOnly?: boolean;
  currentOnly?: boolean;
  municipalityFinancialYearId?: string;
  reportingPeriodType?: 1 | 2 | 3 | 4 | 5 | 6;
  selectedLabel?: string;
  excludedValues?: string[];
  refreshKey?: string | number;
};

const configuration: Record<CalendarMasterKind, { sortBy: string; noun: string }> = {
  'financial-year': { sortBy: 'startDate', noun: 'financial year' },
  'municipality-financial-year': { sortBy: 'startDate', noun: 'municipality financial year' },
  'reporting-period': { sortBy: 'sequence', noun: 'reporting period' },
  'sdbip-layer': { sortBy: 'displayOrder', noun: 'SDBIP layer' },
};

function labelFor(kind: CalendarMasterKind, item: CalendarMasterOption) {
  if (kind === 'sdbip-layer') {
    const layer = item as SdbipLayerMasterDto;
    return `${layer.financialYearCode} · ${layer.code} · ${layer.name}`;
  }
  if (kind === 'municipality-financial-year') {
    const year = item as MunicipalityFinancialYearMasterDto;
    return `${year.code} · ${year.name}${year.isCurrent ? ' · Current' : ''}`;
  }
  const record = item as FinancialYearMasterDto | ReportingPeriodMasterDto;
  return `${record.code} · ${record.name}`;
}

async function loadPage(kind: CalendarMasterKind, query: CalendarMasterPageQuery) {
  switch (kind) {
    case 'financial-year': return getFinancialYearMastersPage(query);
    case 'municipality-financial-year': return getMunicipalityFinancialYearMastersPage(query);
    case 'reporting-period': return getReportingPeriodMastersPage(query);
    case 'sdbip-layer': return getSdbipLayerMastersPage(query);
  }
}

export function CalendarMasterPicker({
  kind,
  label,
  value,
  onChange,
  emptyLabel = 'Select one',
  required,
  disabled,
  activeOnly = true,
  currentOnly,
  municipalityFinancialYearId,
  reportingPeriodType,
  selectedLabel,
  excludedValues = [],
  refreshKey,
}: Props) {
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<CalendarMasterOption[]>([]);
  const [selectedOption, setSelectedOption] = useState<CalendarMasterOption>();
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string>();
  const settings = configuration[kind];
  const excludedKey = excludedValues.join('|');

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPage(1);
      setSearch(searchInput.trim());
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [activeOnly, currentOnly, kind, municipalityFinancialYearId, reportingPeriodType]);

  useEffect(() => {
    if (!value) setSelectedOption(undefined);
  }, [value]);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      setIsLoading(true);
      setError(undefined);
      const result = await loadPage(kind, {
        page,
        pageSize: 25,
        search: search || undefined,
        sortBy: settings.sortBy,
        sortDirection: kind === 'financial-year' || kind === 'municipality-financial-year' ? 'desc' : 'asc',
        active: activeOnly ? true : undefined,
        current: currentOnly,
        municipalityFinancialYearId,
        reportingPeriodType,
      });
      if (cancelled) return;
      if (!result.success || !result.data) {
        setItems([]);
        setTotalCount(0);
        setTotalPages(0);
        setError(result.message ?? `Unable to load ${settings.noun} options.`);
      } else {
        const excluded = new Set(excludedKey ? excludedKey.split('|') : []);
        const nextItems = (result.data.items as CalendarMasterOption[]).filter(item => item.publicId === value || !excluded.has(item.publicId));
        setItems(nextItems);
        setTotalCount(result.data.totalCount);
        setTotalPages(result.data.totalPages);
        const matching = nextItems.find(item => item.publicId === value);
        if (matching) setSelectedOption(matching);
      }
      setIsLoading(false);
    };
    void load();
    return () => { cancelled = true; };
  }, [activeOnly, currentOnly, excludedKey, kind, municipalityFinancialYearId, page, refreshKey, reportingPeriodType, search, settings.noun, settings.sortBy, value]);

  const options = useMemo(() => {
    const values = items.map(item => ({ value: item.publicId, label: labelFor(kind, item) }));
    if (value && !values.some(item => item.value === value)) {
      const retainedLabel = selectedOption?.publicId === value ? labelFor(kind, selectedOption) : selectedLabel;
      values.unshift({ value, label: retainedLabel || `Selected ${settings.noun}` });
    }
    return [{ value: '', label: emptyLabel }, ...values];
  }, [emptyLabel, items, kind, selectedLabel, selectedOption, settings.noun, value]);

  return <div className="space-y-2">
    <Input label={`${label} search`} value={searchInput} onChange={event => setSearchInput(event.target.value)} placeholder={`Search ${settings.noun}s`} disabled={disabled} />
    <Select
      label={label}
      value={value}
      required={required}
      disabled={disabled || isLoading}
      options={options}
      onChange={event => {
        const next = event.target.value;
        const option = items.find(item => item.publicId === next) ?? (selectedOption?.publicId === next ? selectedOption : undefined);
        if (option) setSelectedOption(option);
        onChange(next, option);
      }}
    />
    <div className="flex items-center justify-between gap-2 text-xs text-secondary-500" aria-live="polite">
      <span>{error ?? (isLoading ? `Loading ${settings.noun}s…` : `${totalCount} matching ${settings.noun}${totalCount === 1 ? '' : 's'}`)}</span>
      <span className="flex items-center gap-2">
        <Button type="button" size="sm" variant="ghost" disabled={disabled || isLoading || page <= 1} onClick={() => setPage(current => Math.max(1, current - 1))}>Previous</Button>
        <span>Page {page} of {Math.max(totalPages, 1)}</span>
        <Button type="button" size="sm" variant="ghost" disabled={disabled || isLoading || page >= totalPages} onClick={() => setPage(current => current + 1)}>Next</Button>
      </span>
    </div>
  </div>;
}
