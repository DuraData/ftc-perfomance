import { useEffect, useState } from 'react';
import {
  getDepartments,
  getIpmsTargets,
  getMunicipalEmployees,
  getOpmsTargets,
  getPerformanceLookups,
  getUnits,
} from '../api/api';
import type {
  DepartmentLookupDto,
  IPMSTarget,
  MunicipalEmployeeDto,
  OPMSTarget,
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

export function usePerformanceReferenceData(includeTargets = false, includePeople = true) {
  const [departments, setDepartments] = useState<DepartmentLookupDto[]>([]);
  const [units, setUnits] = useState<UnitLookupDto[]>([]);
  const [employees, setEmployees] = useState<MunicipalEmployeeDto[]>([]);
  const [lookups, setLookups] = useState<PerformanceLookupsDto>(emptyLookups);
  const [opmsTargets, setOpmsTargets] = useState<OPMSTarget[]>([]);
  const [ipmsTargets, setIpmsTargets] = useState<IPMSTarget[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setIsLoading(true);
      setError(null);
      const [departmentResult, unitResult, employeeResult, lookupResult, opmsResult, ipmsResult] = await Promise.all([
        getDepartments(),
        includePeople ? getUnits() : Promise.resolve(null),
        includePeople ? getMunicipalEmployees() : Promise.resolve(null),
        getPerformanceLookups(),
        includeTargets ? getOpmsTargets() : Promise.resolve(null),
        includeTargets ? getIpmsTargets() : Promise.resolve(null),
      ]);
      if (cancelled) return;

      const failed = [departmentResult, unitResult, employeeResult, lookupResult, opmsResult, ipmsResult]
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
      setOpmsTargets(opmsResult?.data ?? []);
      setIpmsTargets(ipmsResult?.data ?? []);
      setIsLoading(false);
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [includePeople, includeTargets]);

  return { departments, units, employees, lookups, opmsTargets, ipmsTargets, isLoading, error };
}
