import { useEffect, useMemo, useState } from 'react';
import {
  getDepartmentMastersPage,
  getPositionMastersPage,
  getUnitMastersPage,
  getVoteNumberMastersPage,
  getWardMastersPage,
  type OrganizationMasterPageQuery,
} from '../../api/api';
import type { DepartmentMasterDto, PositionMasterDto, UnitMasterDto, VoteNumberMasterDto, WardMasterDto } from '../../types';
import { Button } from '../ui';
import { Input, Select } from './Form';

export type OrganizationMasterKind = 'department' | 'unit' | 'position' | 'ward' | 'vote-number';
export type OrganizationMasterOption = DepartmentMasterDto | UnitMasterDto | PositionMasterDto | WardMasterDto | VoteNumberMasterDto;

type Props = {
  kind: OrganizationMasterKind;
  label: string;
  value: string;
  onChange: (value: string, option?: OrganizationMasterOption) => void;
  emptyLabel?: string;
  required?: boolean;
  disabled?: boolean;
  activeOnly?: boolean;
  departmentPublicId?: string;
  unitPublicId?: string;
  municipalityFinancialYearPublicId?: string;
  selectedLabel?: string;
  excludedValues?: string[];
  refreshKey?: string | number;
};

const configuration: Record<OrganizationMasterKind, { sortBy: string; noun: string }> = {
  department: { sortBy: 'name', noun: 'department' },
  unit: { sortBy: 'name', noun: 'unit' },
  position: { sortBy: 'name', noun: 'position' },
  ward: { sortBy: 'code', noun: 'ward' },
  'vote-number': { sortBy: 'code', noun: 'vote number' },
};

function labelFor(kind: OrganizationMasterKind, item: OrganizationMasterOption) {
  if (kind === 'vote-number') {
    const vote = item as VoteNumberMasterDto;
    return `${vote.code} · ${vote.number} · ${vote.name}`;
  }
  if (kind === 'unit') {
    const unit = item as UnitMasterDto;
    return `${unit.code} · ${unit.name} · ${unit.departmentName}`;
  }
  if (kind === 'position') {
    const position = item as PositionMasterDto;
    return `${position.code} · ${position.name}${position.unitName ? ` · ${position.unitName}` : ''}`;
  }
  return `${item.code} · ${item.name}`;
}

async function loadPage(kind: OrganizationMasterKind, query: OrganizationMasterPageQuery) {
  switch (kind) {
    case 'department': return getDepartmentMastersPage(query);
    case 'unit': return getUnitMastersPage(query);
    case 'position': return getPositionMastersPage(query);
    case 'ward': return getWardMastersPage(query);
    case 'vote-number': return getVoteNumberMastersPage(query);
  }
}

export function OrganizationMasterPicker({
  kind,
  label,
  value,
  onChange,
  emptyLabel = 'Select one',
  required,
  disabled,
  activeOnly = true,
  departmentPublicId,
  unitPublicId,
  municipalityFinancialYearPublicId,
  selectedLabel,
  excludedValues = [],
  refreshKey,
}: Props) {
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<OrganizationMasterOption[]>([]);
  const [selectedOption, setSelectedOption] = useState<OrganizationMasterOption>();
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
  }, [activeOnly, departmentPublicId, kind, municipalityFinancialYearPublicId, unitPublicId]);

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
        sortDirection: 'asc',
        active: activeOnly ? true : undefined,
        departmentPublicId,
        unitPublicId,
        municipalityFinancialYearPublicId,
      });
      if (cancelled) return;
      if (!result.success || !result.data) {
        setItems([]);
        setTotalCount(0);
        setTotalPages(0);
        setError(result.message ?? `Unable to load ${settings.noun} options.`);
      } else {
        const excluded = new Set(excludedKey ? excludedKey.split('|') : []);
        const nextItems = (result.data.items as OrganizationMasterOption[]).filter(item => item.publicId === value || !excluded.has(item.publicId));
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
  }, [activeOnly, departmentPublicId, excludedKey, kind, municipalityFinancialYearPublicId, page, refreshKey, search, settings.noun, settings.sortBy, unitPublicId, value]);

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
