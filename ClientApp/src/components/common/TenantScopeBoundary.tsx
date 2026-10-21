import { Fragment, type ReactNode } from 'react';

interface TenantScopeBoundaryProps {
  municipalityPublicId: string | null;
  municipalityFinancialYearPublicId?: string | null;
  children: ReactNode;
}

export function TenantScopeBoundary({ municipalityPublicId, municipalityFinancialYearPublicId, children }: TenantScopeBoundaryProps) {
  const tenantScope = municipalityPublicId ?? 'system-scope';
  const financialYearScope = municipalityFinancialYearPublicId ?? 'no-financial-year';
  return <Fragment key={`${tenantScope}:${financialYearScope}`}>{children}</Fragment>;
}
