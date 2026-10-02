/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useMemo, type ReactNode } from 'react';
import { useApp } from './AppContext';

export interface SecurityCapabilities {
  canRead: (resourceCode: string) => boolean;
  canCreate: (resourceCode: string) => boolean;
  canUpdate: (resourceCode: string) => boolean;
  canDelete: (resourceCode: string) => boolean;
  canExport: (resourceCode: string) => boolean;
  canImport: (resourceCode: string) => boolean;
  canExecute: (actionCode: string) => boolean;
  canReadField: (resourceCode: string, memberCode: string) => boolean;
  canEditField: (resourceCode: string, memberCode: string) => boolean;
}

const SecurityContext = createContext<SecurityCapabilities | undefined>(undefined);

export function SecurityProvider({ children }: { children: ReactNode }) {
  const { permissions } = useApp();
  const permissionSet = useMemo(() => new Set(permissions.map(item => item.toUpperCase())), [permissions]);
  const value = useMemo<SecurityCapabilities>(() => ({
    canRead: resource => permissionSet.has(`${resource}.READ`.toUpperCase()),
    canCreate: resource => permissionSet.has(`${resource}.CREATE`.toUpperCase()),
    canUpdate: resource => permissionSet.has(`${resource}.UPDATE`.toUpperCase()),
    canDelete: resource => permissionSet.has(`${resource}.DELETE`.toUpperCase()),
    canExport: resource => permissionSet.has(`${resource}.EXPORT`.toUpperCase()),
    canImport: resource => permissionSet.has(`${resource}.IMPORT`.toUpperCase()),
    canExecute: action => permissionSet.has(action.toUpperCase()),
    canReadField: (resource, member) => permissionSet.has(`${resource}.${member}.READ`.toUpperCase()),
    canEditField: (resource, member) => permissionSet.has(`${resource}.${member}.UPDATE`.toUpperCase()),
  }), [permissionSet]);

  return <SecurityContext.Provider value={value}>{children}</SecurityContext.Provider>;
}

export function useSecurity() {
  const value = useContext(SecurityContext);
  if (!value) throw new Error('useSecurity must be used within SecurityProvider');
  return value;
}
