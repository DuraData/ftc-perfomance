import { useEffect, useState } from 'react';
import {
  getMunicipalEmployeesPage,
  getPerformanceLookups,
} from '../api/api';
import type {
  MunicipalEmployeeDto,
  PerformanceLookupsDto,
} from '../types';

const emptyLookups: PerformanceLookupsDto = {
  periods: [],
  strategicGoals: [],
  strategicObjectives: [],
  budgetSources: [],
  budgetTypes: [],
  unitsOfMeasure: [],
};

export function usePerformanceReferenceData(includePeople = true) {
  const [employees, setEmployees] = useState<MunicipalEmployeeDto[]>([]);
  const [employeePage, setEmployeePage] = useState(1);
  const [employeeTotalPages, setEmployeeTotalPages] = useState(0);
  const [employeeSearch, setEmployeeSearchState] = useState('');
  const [lookups, setLookups] = useState<PerformanceLookupsDto>(emptyLookups);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setIsLoading(true);
      setError(null);
      const lookupResult = await getPerformanceLookups();
      if (cancelled) return;

      if (!lookupResult.success) {
        setError(lookupResult.message ?? 'Failed to load performance reference data.');
        setIsLoading(false);
        return;
      }

      setLookups(lookupResult.data ?? emptyLookups);
      setIsLoading(false);
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [includePeople]);

  useEffect(() => {
    if (!includePeople) { setEmployees([]); setEmployeeTotalPages(0); return; }
    let cancelled = false;
    void getMunicipalEmployeesPage({ page: employeePage, pageSize: 25, search: employeeSearch, sortBy: 'name', sortDirection: 'asc' }, true).then(result => {
      if (cancelled) return;
      if (!result.success) { setError(result.message ?? 'Failed to load employees.'); return; }
      setEmployees(result.data?.items ?? []);
      setEmployeeTotalPages(result.data?.totalPages ?? 0);
    });
    return () => { cancelled = true; };
  }, [employeePage, employeeSearch, includePeople]);

  const setEmployeeSearch = (value: string) => { setEmployeeSearchState(value); setEmployeePage(1); };

  return { employees, employeePage, employeeTotalPages, employeeSearch, setEmployeePage, setEmployeeSearch, lookups, isLoading, error };
}
