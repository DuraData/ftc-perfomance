/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, ReactNode, useEffect, useCallback } from 'react';
import type {
  UserRole,
  UserProfile,
  LoginResponse,
  MenuItem,
  TenantContextDto,
} from '../types';
import {
  getCurrentMunicipalityId,
  getMyMenu,
  getMyPermissions,
  getMyTenantContexts,
  login as apiLogin,
  completeEnterpriseLogin,
  isAuthenticated,
  logout as apiLogout,
  setCurrentMunicipalityId,
} from '../api/api';

type ToastType = 'success' | 'error' | 'info';
interface ToastItem {
  id: string;
  type: ToastType;
  message: string;
}


interface AppContextType {
  userProfile: UserProfile | null;
  isAuthenticated: boolean;
  roles: string[];
  permissions: string[];
  menuItems: MenuItem[];
  tenantContexts: TenantContextDto[];
  currentMunicipalityId: number | null;
  switchMunicipality: (municipalityId: number) => Promise<boolean>;
  login: (email: string, password: string, twoFactorCode?: string, recoveryCode?: string) => Promise<'success' | 'mfa_required' | 'mfa_enrollment_required' | 'password_change_required' | 'failed'>;
  resumeEnterpriseLogin: () => Promise<'success' | 'failed'>;
  logout: () => void;
  sidebarCollapsed: boolean;
  expandedSidebarGroups: string[];
  darkMode: boolean;
  toggleSidebar: () => void;
  toggleSidebarGroup: (label: string) => void;
  expandSidebarGroup: (label: string) => void;
  toggleDarkMode: () => void;
  switchRole: (role: UserRole) => void;
  currentPath: string;
  setCurrentPath: (path: string) => void;
  toasts: ToastItem[];
  pushToast: (type: ToastType, message: string) => void;
}

const AppContext = createContext<AppContextType | undefined>(undefined);

export function AppProvider({ children }: { children: ReactNode }) {
  const safeSetItem = (key: string, value: string) => {
    try { localStorage.setItem(key, value); } catch { /* ignore */ }
  };
  const safeRemoveItem = (key: string) => {
    try { localStorage.removeItem(key); } catch { /* ignore */ }
  };

  const [userProfile, setUserProfile] = useState<UserProfile | null>(null);
  const [roles, setRoles] = useState<string[]>([]);
  const [permissions, setPermissions] = useState<string[]>([]);
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [tenantContexts, setTenantContexts] = useState<TenantContextDto[]>([]);
  const [currentMunicipalityIdState, setCurrentMunicipalityIdState] = useState<number | null>(getCurrentMunicipalityId());
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [expandedSidebarGroups, setExpandedSidebarGroups] = useState<string[]>([]);
  const [darkMode, setDarkMode] = useState(false);
  const normalizePath = (path: string) => {
    if (path.startsWith('/admin/users')) return '/system-administration/users';
    if (path.startsWith('/admin/roles')) return '/system-administration/roles';
    if (path.startsWith('/admin/permissions')) return '/system-administration/permissions';
    if (path.startsWith('/admin/audit')) return '/system-administration/audit-logs';
    if (path.startsWith('/system-administration/role-implementation-audit')) return '/system-administration/role-implementation-audit';
    if (path.startsWith('/system-administration/role-permission-crud-audit')) return '/system-administration/role-permission-crud-audit';
    return path;
  };

  const [currentPath, setCurrentPathState] = useState(isAuthenticated() ? normalizePath(window.location.pathname || '/dashboard') : '/login');
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const refreshTenantAccess = useCallback(async (municipalityId: number) => {
    setCurrentMunicipalityId(municipalityId);
    setCurrentMunicipalityIdState(municipalityId);
    const [permissionsResult, menuResult] = await Promise.all([getMyPermissions(), getMyMenu()]);
    if (!permissionsResult.success || !permissionsResult.data || !menuResult.success || !menuResult.data) {
      setPermissions([]);
      setMenuItems([]);
      safeRemoveItem('permissions');
      safeRemoveItem('menu_items');
      return false;
    }
    setPermissions(permissionsResult.data);
    setMenuItems(menuResult.data);
    safeSetItem('permissions', JSON.stringify(permissionsResult.data));
    safeSetItem('menu_items', JSON.stringify(menuResult.data));
    return true;
  }, []);

  const loadTenantContexts = useCallback(async () => {
    const result = await getMyTenantContexts();
    if (!result.success || !result.data) {
      setTenantContexts([]);
      setPermissions([]);
      setMenuItems([]);
      return false;
    }
    setTenantContexts(result.data);
    const storedId = getCurrentMunicipalityId();
    const selectedId = result.data.some(item => item.id === storedId)
      ? storedId
      : result.data.length === 1 ? result.data[0].id : null;
    if (selectedId === null) {
      setCurrentMunicipalityId(null);
      setCurrentMunicipalityIdState(null);
      setPermissions([]);
      setMenuItems([]);
      safeRemoveItem('permissions');
      safeRemoveItem('menu_items');
      return result.data.length === 0;
    }
    return refreshTenantAccess(selectedId);
  }, [refreshTenantAccess]);

  const switchMunicipality = useCallback(async (municipalityId: number) => {
    if (!tenantContexts.some(item => item.id === municipalityId)) return false;
    return refreshTenantAccess(municipalityId);
  }, [refreshTenantAccess, tenantContexts]);

  useEffect(() => {
    let storedUser: string | null = null;
    let storedRoles: string | null = null;
    let storedPermissions: string | null = null;
    let storedMenu: string | null = null;
    let storedExpanded: string | null = null;
    let storedSidebarCollapsed: string | null = null;
    try {
      storedUser = localStorage.getItem('user_profile');
      storedRoles = localStorage.getItem('roles');
      storedPermissions = localStorage.getItem('permissions');
      storedMenu = localStorage.getItem('menu_items');
      storedExpanded = localStorage.getItem('sidebar_expanded_groups');
      storedSidebarCollapsed = localStorage.getItem('sidebar_collapsed');
    } catch {
      // ignore
    }

    if (storedExpanded) {
      try { setExpandedSidebarGroups(JSON.parse(storedExpanded)); } catch { setExpandedSidebarGroups([]); }
    }
    if (storedSidebarCollapsed) {
      setSidebarCollapsed(storedSidebarCollapsed === 'true');
    }

    if (storedUser) {
      setUserProfile(JSON.parse(storedUser));
      const parsedRoles = storedRoles ? JSON.parse(storedRoles) : [];
      setRoles(parsedRoles);
      setPermissions(storedPermissions ? JSON.parse(storedPermissions) : []);
      setMenuItems(storedMenu ? JSON.parse(storedMenu) as MenuItem[] : []);
      setCurrentPathState(normalizePath(window.location.pathname || '/dashboard'));
      void loadTenantContexts();
    }
  }, [loadTenantContexts]);

  useEffect(() => {
    safeSetItem('sidebar_collapsed', String(sidebarCollapsed));
  }, [sidebarCollapsed]);

  useEffect(() => {
    safeSetItem('sidebar_expanded_groups', JSON.stringify(expandedSidebarGroups));
  }, [expandedSidebarGroups]);

  useEffect(() => {
    const onPopState = () => setCurrentPathState(normalizePath(window.location.pathname || '/dashboard'));
    window.addEventListener('popstate', onPopState);
    return () => window.removeEventListener('popstate', onPopState);
  }, []);

  const setCurrentPath = (path: string) => {
    const nextPath = normalizePath(path);
    if (nextPath === currentPath) return;
    window.history.pushState({}, '', nextPath);
    setCurrentPathState(nextPath);
  };

  const login = async (email: string, password: string, twoFactorCode?: string, recoveryCode?: string): Promise<'success' | 'mfa_required' | 'mfa_enrollment_required' | 'password_change_required' | 'failed'> => {
    const result = await apiLogin({ email, password, twoFactorCode, recoveryCode });
    if (result.success && result.data) {
      const data = result.data as LoginResponse;
      setUserProfile(data.user);
      setRoles(data.roles ?? []);
      setCurrentMunicipalityId(null);
      setCurrentMunicipalityIdState(null);
      setPermissions([]);
      setMenuItems([]);
      safeSetItem('user_profile', JSON.stringify(data.user));
      safeSetItem('roles', JSON.stringify(data.roles ?? []));
      safeRemoveItem('permissions');
      safeRemoveItem('menu_items');
      if (data.user.mustChangePassword) {
        safeSetItem('settings_active_tab', 'security');
        setCurrentPath('/settings');
        return 'password_change_required';
      }
      if (data.mfaEnrollmentRequired) {
        safeSetItem('settings_active_tab', 'security');
        setCurrentPath('/settings');
        return 'mfa_enrollment_required';
      }
      await loadTenantContexts();
      setCurrentPath('/dashboard');
      return 'success';
    }
    return result.message === 'MFA_REQUIRED' ? 'mfa_required' : 'failed';
  };

  const resumeEnterpriseLogin = useCallback(async (): Promise<'success' | 'failed'> => {
    const result = await completeEnterpriseLogin();
    if (!result.success || !result.data) return 'failed';
    const data = result.data;
    setUserProfile(data.user);
    setRoles(data.roles ?? []);
    setCurrentMunicipalityId(null);
    setCurrentMunicipalityIdState(null);
    setPermissions([]);
    setMenuItems([]);
    safeSetItem('user_profile', JSON.stringify(data.user));
    safeSetItem('roles', JSON.stringify(data.roles ?? []));
    safeRemoveItem('permissions');
    safeRemoveItem('menu_items');
    await loadTenantContexts();
    window.history.replaceState({}, '', '/dashboard');
    setCurrentPathState('/dashboard');
    return 'success';
  }, [loadTenantContexts]);

  const logout = () => {
    apiLogout();
    setUserProfile(null);
    setRoles([]);
    setPermissions([]);
    setMenuItems([]);
    setTenantContexts([]);
    setCurrentMunicipalityIdState(null);
    setCurrentMunicipalityId(null);
    safeRemoveItem('user_profile');
    safeRemoveItem('roles');
    safeRemoveItem('permissions');
    safeRemoveItem('menu_items');
    window.history.pushState({}, '', '/login');
    setCurrentPathState('/login');
  };

  const toggleSidebar = () => {
    setSidebarCollapsed(prev => !prev);
  };

  const toggleSidebarGroup = (label: string) => {
    setExpandedSidebarGroups(prev => {
      return prev.includes(label) ? prev.filter(x => x !== label) : [...prev, label];
    });
  };

  const expandSidebarGroup = (label: string) => {
    setExpandedSidebarGroups(prev => {
      if (prev.includes(label)) return prev;
      return [...prev, label];
    });
  };

  const pushToast = useCallback((type: ToastType, message: string) => {
    const id = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    const toast: ToastItem = { id, type, message };
    setToasts(prev => [...prev, toast]);
    window.setTimeout(() => {
      setToasts(prev => prev.filter(t => t.id !== id));
    }, 3000);
  }, []);

  const toggleDarkMode = () => {
    setDarkMode(prev => !prev);
    if (!darkMode) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  };

  const switchRole = (role: UserRole) => {
    void role;
    // Role switching to be implemented later
  };

  return (
    <AppContext.Provider
      value={{
        userProfile,
        isAuthenticated: isAuthenticated() && !!userProfile,
        roles,
        permissions,
        menuItems,
        tenantContexts,
        currentMunicipalityId: currentMunicipalityIdState,
        switchMunicipality,
        login,
        resumeEnterpriseLogin,
        logout,
        sidebarCollapsed,
        expandedSidebarGroups,
        darkMode,
        toggleSidebar,
        toggleSidebarGroup,
        expandSidebarGroup,
        toggleDarkMode,
        switchRole,
        currentPath,
        setCurrentPath,
        toasts,
        pushToast,
      }}
    >
      {children}
    </AppContext.Provider>
  );
}

export function useApp() {
  const context = useContext(AppContext);
  if (context === undefined) {
    throw new Error('useApp must be used within an AppProvider');
  }
  return context;
}
