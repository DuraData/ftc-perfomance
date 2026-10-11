import { useEffect, useState } from 'react';
import {
  getMunicipalEmployeesPage,
} from '../api/api';
import type {
  MunicipalEmployeeDto,
} from '../types';

export function usePerformanceReferenceData(includePeople = true) {
  const [employees, setEmployees] = useState<MunicipalEmployeeDto[]>([]);
  const [employeePage, setEmployeePage] = useState(1);
  const [employeeTotalPages, setEmployeeTotalPages] = useState(0);
  const [employeeSearch, setEmployeeSearchState] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!includePeople) { setEmployees([]); setEmployeeTotalPages(0); setError(null); return; }
    let cancelled = false;
    setError(null);
    void getMunicipalEmployeesPage({ page: employeePage, pageSize: 25, search: employeeSearch, sortBy: 'name', sortDirection: 'asc' }, true).then(result => {
      if (cancelled) return;
      if (!result.success) { setError(result.message ?? 'Failed to load employees.'); return; }
      setEmployees(result.data?.items ?? []);
      setEmployeeTotalPages(result.data?.totalPages ?? 0);
    });
    return () => { cancelled = true; };
  }, [employeePage, employeeSearch, includePeople]);

  const setEmployeeSearch = (value: string) => { setEmployeeSearchState(value); setEmployeePage(1); };

  return { employees, employeePage, employeeTotalPages, employeeSearch, setEmployeePage, setEmployeeSearch, isLoading: false, error };
}
