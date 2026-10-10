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
  getMyTenantContextsPage,
  login as apiLogin,
  completeEnterpriseLogin,
  isAuthenticated,
  logout as apiLogout,
  setCurrentMunicipalityId,
} from '../api/api';

type ToastType = 'success' | 'error' | 'info';
export type ThemePreference = 'light' | 'dark' | 'system';
interface ToastItem {
  id: string;
  type: ToastType;
  message: string;
}


interface AppContextType {
  userProfile: UserProfile | null;
  updateUserProfile: (profile: UserProfile) => void;
  isAuthenticated: boolean;
  roles: string[];
  permissions: string[];
  menuItems: MenuItem[];
  accessReady: boolean;
  authenticationGate: AuthenticationGate | null;
  tenantContexts: TenantContextDto[];
  tenantContextPage: number;
  tenantContextTotalPages: number;
  tenantContextTotalCount: number;
  tenantContextSearch: string;
  setTenantContextPage: (page: number) => void;
  setTenantContextSearch: (search: string) => void;
  currentMunicipalityId: number | null;
  switchMunicipality: (municipalityId: number) => Promise<boolean>;
  login: (email: string, password: string, twoFactorCode?: string, recoveryCode?: string) => Promise<'success' | 'mfa_required' | 'mfa_enrollment_required' | 'password_change_required' | 'failed'>;
  resumeEnterpriseLogin: () => Promise<'success' | 'failed'>;
  logout: (notifyServer?: boolean) => void;
  sidebarCollapsed: boolean;
  expandedSidebarGroups: string[];
  darkMode: boolean;
  themePreference: ThemePreference;
  toggleSidebar: () => void;
  toggleSidebarGroup: (label: string) => void;
  expandSidebarGroup: (label: string) => void;
  toggleDarkMode: () => void;
  setThemePreference: (preference: ThemePreference) => void;
  switchRole: (role: UserRole) => void;
  currentPath: string;
  setCurrentPath: (path: string) => void;
  toasts: ToastItem[];
  pushToast: (type: ToastType, message: string) => void;
}

const AppContext = createContext<AppContextType | undefined>(undefined);
type AuthenticationGate = 'password_change' | 'mfa_enrollment';
const AUTHENTICATION_GATE_STORAGE_KEY = 'authentication_gate';
const THEME_PREFERENCE_STORAGE_KEY = 'theme_preference';
export const RESTRICTED_ACCOUNT_MENU: MenuItem[] = [{
  label: 'Account Security',
  path: '/settings',
  icon: 'settings',
  isDivider: false,
  code: 'NAV.ACCOUNT.SECURITY',
}];

function readAuthenticationGate(): AuthenticationGate | null {
  try {
    const storedGate = localStorage.getItem(AUTHENTICATION_GATE_STORAGE_KEY);
    if (storedGate === 'password_change' || storedGate === 'mfa_enrollment') return storedGate;
    const storedUser = localStorage.getItem('user_profile');
    if (storedUser && (JSON.parse(storedUser) as UserProfile).mustChangePassword) return 'password_change';
  } catch {
    // A denied storage read must not grant normal application access.
  }
  return null;
}

function readThemePreference(): ThemePreference {
  try {
    const stored = localStorage.getItem(THEME_PREFERENCE_STORAGE_KEY);
    if (stored === 'light' || stored === 'dark' || stored === 'system') return stored;
  } catch {
    // ignore storage issues and retain the accessible light default
  }
  return 'light';
}

function systemPrefersDark() {
  return typeof window.matchMedia === 'function' && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

export function normalizeAppPath(path: string) {
  const routePath = path.split(/[?#]/, 1)[0];
  if (routePath.startsWith('/admin/users')) return '/system-administration/users';
  if (routePath.startsWith('/admin/roles')) return '/system-administration/roles';
  if (routePath.startsWith('/admin/permissions')) return '/system-administration/permissions';
  if (routePath.startsWith('/admin/audit')) return '/system-administration/audit-logs';
  if (routePath.startsWith('/system-administration/role-implementation-audit')) return '/system-administration/role-implementation-audit';
  if (routePath.startsWith('/system-administration/role-permission-crud-audit')) return '/system-administration/role-permission-crud-audit';
  return routePath;
}

export function resolveAppNavigation(path: string) {
  const rawPath = path.split(/[?#]/, 1)[0];
  const routePath = normalizeAppPath(rawPath);
  return { routePath, location: `${routePath}${path.slice(rawPath.length)}` };
}

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
  const [accessReady, setAccessReady] = useState(!isAuthenticated());
  const [tenantContexts, setTenantContexts] = useState<TenantContextDto[]>([]);
  const [tenantContextPage, setTenantContextPage] = useState(1);
  const [tenantContextTotalPages, setTenantContextTotalPages] = useState(0);
  const [tenantContextTotalCount, setTenantContextTotalCount] = useState(0);
  const [tenantContextSearch, setTenantContextSearchState] = useState('');
  const [currentMunicipalityIdState, setCurrentMunicipalityIdState] = useState<number | null>(getCurrentMunicipalityId());
  const [authenticationGate, setAuthenticationGate] = useState<AuthenticationGate | null>(() => readAuthenticationGate());
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [expandedSidebarGroups, setExpandedSidebarGroups] = useState<string[]>([]);
  const [themePreference, setThemePreferenceState] = useState<ThemePreference>(() => readThemePreference());
  const [darkMode, setDarkMode] = useState(() => {
    const preference = readThemePreference();
    return preference === 'dark' || (preference === 'system' && systemPrefersDark());
  });
  const [currentPath, setCurrentPathState] = useState(isAuthenticated() ? normalizeAppPath(window.location.pathname || '/dashboard') : '/login');
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const refreshTenantAccess = useCallback(async (municipalityId: number | null) => {
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

  const loadTenantContexts = useCallback(async (page = 1, search = '') => {
    const result = await getMyTenantContextsPage({ page, pageSize: 25, search, sortBy: 'name', sortDirection: 'asc' });
    if (!result.success || !result.data) {
      setTenantContexts([]);
      setTenantContextTotalPages(0);
      setTenantContextTotalCount(0);
      setPermissions([]);
      setMenuItems([]);
      return false;
    }
    let contexts = result.data.items;
    setTenantContextTotalPages(result.data.totalPages);
    setTenantContextTotalCount(result.data.totalCount);
    const storedId = getCurrentMunicipalityId();
    if (storedId && !contexts.some(item => item.id === storedId)) {
      const exact = await getMyTenantContextsPage({ page: 1, pageSize: 1, sortBy: 'name', sortDirection: 'asc' }, storedId);
      const selected = exact.data?.items[0];
      if (selected) contexts = [selected, ...contexts];
    }
    setTenantContexts(contexts);
    const selectedId = contexts.some(item => item.id === storedId)
      ? storedId
      : !search && result.data.totalCount === 1 ? result.data.items[0]?.id ?? null : null;
    if (selectedId === null) {
      return refreshTenantAccess(null);
    }
    return refreshTenantAccess(selectedId);
  }, [refreshTenantAccess]);

  const setTenantContextSearch = useCallback((search: string) => {
    setTenantContextSearchState(search);
    setTenantContextPage(1);
  }, []);

  useEffect(() => {
    if (userProfile && authenticationGate === null) {
      void loadTenantContexts(tenantContextPage, tenantContextSearch).finally(() => setAccessReady(true));
    }
  }, [authenticationGate, loadTenantContexts, tenantContextPage, tenantContextSearch, userProfile]);

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
      const parsedUser = JSON.parse(storedUser) as UserProfile;
      const gate = readAuthenticationGate();
      setUserProfile(parsedUser);
      const parsedRoles = storedRoles ? JSON.parse(storedRoles) : [];
      setRoles(parsedRoles);
      setAuthenticationGate(gate);
      if (gate) {
        setPermissions([]);
        setMenuItems(RESTRICTED_ACCOUNT_MENU);
        safeSetItem(AUTHENTICATION_GATE_STORAGE_KEY, gate);
        safeSetItem('menu_items', JSON.stringify(RESTRICTED_ACCOUNT_MENU));
        window.history.replaceState({}, '', '/settings');
        setCurrentPathState('/settings');
        setAccessReady(true);
      } else {
        setPermissions(storedPermissions ? JSON.parse(storedPermissions) : []);
        setMenuItems(storedMenu ? JSON.parse(storedMenu) as MenuItem[] : []);
        setCurrentPathState(normalizeAppPath(window.location.pathname || '/dashboard'));
      }
    }
  }, []);

  useEffect(() => {
    safeSetItem('sidebar_collapsed', String(sidebarCollapsed));
  }, [sidebarCollapsed]);

  useEffect(() => {
    const media = typeof window.matchMedia === 'function' ? window.matchMedia('(prefers-color-scheme: dark)') : null;
    const applyTheme = () => {
      const shouldUseDark = themePreference === 'dark' || (themePreference === 'system' && Boolean(media?.matches));
      setDarkMode(shouldUseDark);
      document.documentElement.classList.toggle('dark', shouldUseDark);
    };
    applyTheme();
    try { localStorage.setItem(THEME_PREFERENCE_STORAGE_KEY, themePreference); } catch { /* ignore */ }
    if (themePreference === 'system') media?.addEventListener?.('change', applyTheme);
    return () => media?.removeEventListener?.('change', applyTheme);
  }, [themePreference]);

  useEffect(() => {
    safeSetItem('sidebar_expanded_groups', JSON.stringify(expandedSidebarGroups));
  }, [expandedSidebarGroups]);

  useEffect(() => {
    const onPopState = () => setCurrentPathState(normalizeAppPath(window.location.pathname || '/dashboard'));
    window.addEventListener('popstate', onPopState);
    return () => window.removeEventListener('popstate', onPopState);
  }, []);

  const setCurrentPath = (path: string) => {
    const next = resolveAppNavigation(path);
    if (next.routePath === currentPath && `${window.location.pathname}${window.location.search}${window.location.hash}` === next.location) return;
    window.history.pushState({}, '', next.location);
    setCurrentPathState(next.routePath);
  };

  const login = async (email: string, password: string, twoFactorCode?: string, recoveryCode?: string): Promise<'success' | 'mfa_required' | 'mfa_enrollment_required' | 'password_change_required' | 'failed'> => {
    const result = await apiLogin({ email, password, twoFactorCode, recoveryCode });
    if (result.success && result.data) {
      const data = result.data as LoginResponse;
      setAccessReady(false);
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
        setAuthenticationGate('password_change');
        setMenuItems(RESTRICTED_ACCOUNT_MENU);
        safeSetItem(AUTHENTICATION_GATE_STORAGE_KEY, 'password_change');
        safeSetItem('menu_items', JSON.stringify(RESTRICTED_ACCOUNT_MENU));
        safeSetItem('settings_active_tab', 'security');
        setCurrentPath('/settings');
        setAccessReady(true);
        return 'password_change_required';
      }
      if (data.mfaEnrollmentRequired) {
        setAuthenticationGate('mfa_enrollment');
        setMenuItems(RESTRICTED_ACCOUNT_MENU);
        safeSetItem(AUTHENTICATION_GATE_STORAGE_KEY, 'mfa_enrollment');
        safeSetItem('menu_items', JSON.stringify(RESTRICTED_ACCOUNT_MENU));
        safeSetItem('settings_active_tab', 'security');
        setCurrentPath('/settings');
        setAccessReady(true);
        return 'mfa_enrollment_required';
      }
      setAuthenticationGate(null);
      safeRemoveItem(AUTHENTICATION_GATE_STORAGE_KEY);
      await loadTenantContexts();
      setAccessReady(true);
      setCurrentPath('/dashboard');
      return 'success';
    }
    return result.message === 'MFA_REQUIRED' ? 'mfa_required' : 'failed';
  };

  const resumeEnterpriseLogin = useCallback(async (): Promise<'success' | 'failed'> => {
    const result = await completeEnterpriseLogin();
    if (!result.success || !result.data) return 'failed';
    const data = result.data;
    setAccessReady(false);
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
    if (data.user.mustChangePassword || data.mfaEnrollmentRequired) {
      const gate: AuthenticationGate = data.user.mustChangePassword ? 'password_change' : 'mfa_enrollment';
      setAuthenticationGate(gate);
      setMenuItems(RESTRICTED_ACCOUNT_MENU);
      safeSetItem(AUTHENTICATION_GATE_STORAGE_KEY, gate);
      safeSetItem('menu_items', JSON.stringify(RESTRICTED_ACCOUNT_MENU));
      safeSetItem('settings_active_tab', 'security');
      window.history.replaceState({}, '', '/settings');
      setCurrentPathState('/settings');
      setAccessReady(true);
      return 'success';
    }
    setAuthenticationGate(null);
    safeRemoveItem(AUTHENTICATION_GATE_STORAGE_KEY);
    await loadTenantContexts();
    setAccessReady(true);
    window.history.replaceState({}, '', '/dashboard');
    setCurrentPathState('/dashboard');
    return 'success';
  }, [loadTenantContexts]);

  const logout = (notifyServer = true) => {
    if (notifyServer) void apiLogout();
    setUserProfile(null);
    setRoles([]);
    setPermissions([]);
    setMenuItems([]);
    setAccessReady(true);
    setTenantContexts([]);
    setTenantContextPage(1);
    setTenantContextTotalPages(0);
    setTenantContextTotalCount(0);
    setTenantContextSearchState('');
    setAuthenticationGate(null);
    setCurrentMunicipalityIdState(null);
    setCurrentMunicipalityId(null);
    safeRemoveItem('user_profile');
    safeRemoveItem('roles');
    safeRemoveItem('permissions');
    safeRemoveItem('menu_items');
    safeRemoveItem(AUTHENTICATION_GATE_STORAGE_KEY);
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

  const setThemePreference = (preference: ThemePreference) => setThemePreferenceState(preference);
  const toggleDarkMode = () => setThemePreferenceState(darkMode ? 'light' : 'dark');
  const updateUserProfile = (profile: UserProfile) => {
    setUserProfile(profile);
    safeSetItem('user_profile', JSON.stringify(profile));
  };

  const switchRole = (role: UserRole) => {
    void role;
    // Role switching to be implemented later
  };

  return (
    <AppContext.Provider
      value={{
        userProfile,
        updateUserProfile,
        isAuthenticated: isAuthenticated() && !!userProfile,
        roles,
        permissions,
        menuItems,
        accessReady,
        authenticationGate,
        tenantContexts,
        tenantContextPage,
        tenantContextTotalPages,
        tenantContextTotalCount,
        tenantContextSearch,
        setTenantContextPage,
        setTenantContextSearch,
        currentMunicipalityId: currentMunicipalityIdState,
        switchMunicipality,
        login,
        resumeEnterpriseLogin,
        logout,
        sidebarCollapsed,
        expandedSidebarGroups,
        darkMode,
        themePreference,
        toggleSidebar,
        toggleSidebarGroup,
        expandSidebarGroup,
        toggleDarkMode,
        setThemePreference,
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
