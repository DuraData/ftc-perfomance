import { useEffect, useState } from 'react';
import {
  getDepartments,
  getMunicipalEmployeesPage,
  getPerformanceLookups,
  getUnits,
} from '../api/api';
import type {
  DepartmentLookupDto,
  MunicipalEmployeeDto,
  PerformanceLookupsDto,
  UnitLookupDto,
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
  const [departments, setDepartments] = useState<DepartmentLookupDto[]>([]);
  const [units, setUnits] = useState<UnitLookupDto[]>([]);
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
      const [departmentResult, unitResult, lookupResult] = await Promise.all([
        getDepartments(),
        includePeople ? getUnits() : Promise.resolve(null),
        getPerformanceLookups(),
      ]);
      if (cancelled) return;

      const failed = [departmentResult, unitResult, lookupResult]
        .filter(result => result && !result.success)
        .map(result => result?.message)
        .find(Boolean);
      if (failed) {
        setError(failed ?? 'Failed to load performance reference data.');
        setIsLoading(false);
        return;
      }

      setDepartments(departmentResult.data ?? []);
      setUnits(unitResult?.data ?? []);
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

  return { departments, units, employees, employeePage, employeeTotalPages, employeeSearch, setEmployeePage, setEmployeeSearch, lookups, isLoading, error };
}
