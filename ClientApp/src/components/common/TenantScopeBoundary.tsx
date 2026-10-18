import { Fragment, type ReactNode } from 'react';

interface TenantScopeBoundaryProps {
  municipalityPublicId: string | null;
  children: ReactNode;
}

export function TenantScopeBoundary({ municipalityPublicId, children }: TenantScopeBoundaryProps) {
  return <Fragment key={municipalityPublicId ?? 'system-scope'}>{children}</Fragment>;
}
