import { useEffect, useState } from 'react';
import {
  getDepartments,
  getMunicipalEmployees,
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
  const [lookups, setLookups] = useState<PerformanceLookupsDto>(emptyLookups);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setIsLoading(true);
      setError(null);
      const [departmentResult, unitResult, employeeResult, lookupResult] = await Promise.all([
        getDepartments(),
        includePeople ? getUnits() : Promise.resolve(null),
        includePeople ? getMunicipalEmployees() : Promise.resolve(null),
        getPerformanceLookups(),
      ]);
      if (cancelled) return;

      const failed = [departmentResult, unitResult, employeeResult, lookupResult]
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
      setEmployees((employeeResult?.data ?? []).filter(item => item.isActive));
      setLookups(lookupResult.data ?? emptyLookups);
      setIsLoading(false);
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [includePeople]);

  return { departments, units, employees, lookups, isLoading, error };
}
