/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, ReactNode, useEffect, useCallback } from 'react';
import type {
  UserRole,
  UserProfile,
  LoginResponse,
  MenuItem,
} from '../types';
import { login as apiLogin, isAuthenticated, logout as apiLogout } from '../api/api';

type ToastType = 'success' | 'error' | 'info';
interface ToastItem {
  id: string;
  type: ToastType;
  message: string;
}

const isSuperAdminRole = (roles: string[]) =>
  roles.some(role =>
    ['Super Admin']
      .some(r => r.toLowerCase() === role.toLowerCase()),
  );

const buildFullMenu = (): MenuItem[] => [
  { label: 'Dashboard', path: '/dashboard', icon: 'dashboard', isDivider: false },
  {
    label: 'OPMS',
    icon: 'target',
    isDivider: false,
    children: [
      { label: 'OPMS Dashboard', path: '/opms/dashboard', icon: 'dashboard', isDivider: false },
      { label: 'OPMS Targets', path: '/opms/targets', icon: 'target', isDivider: false },
      { label: 'OPMS Submissions', path: '/opms/submissions', icon: 'target', isDivider: false },
      { label: 'Vote Numbers', path: '/opms/vote-numbers', icon: 'layers', isDivider: false },
    ],
  },
  {
    label: 'IPMS',
    icon: 'target',
    isDivider: false,
    children: [
      { label: 'IPMS Dashboard', path: '/ipms/dashboard', icon: 'dashboard', isDivider: false },
      { label: 'IPMS Targets', path: '/ipms/targets', icon: 'target', isDivider: false },
      { label: 'IPMS Submissions', path: '/ipms/submissions', icon: 'target', isDivider: false },
    ],
  },
  {
    label: 'IDP',
    icon: 'map',
    isDivider: false,
    children: [
      { label: 'IDP Dashboard', path: '/idp/dashboard', icon: 'dashboard', isDivider: false },
      { label: 'IDP Plans', path: '/idp/plans', icon: 'map', isDivider: false },
      { label: 'Planning Hierarchy', path: '/idp/hierarchy', icon: 'target', isDivider: false },
      { label: 'Community Participation', path: '/idp/community', icon: 'users', isDivider: false },
      { label: 'Alignment Matrix', path: '/idp/alignment', icon: 'layers', isDivider: false },
      { label: 'IDP Reports', path: '/idp/reports', icon: 'reports', isDivider: false },
    ],
  },
  {    label: 'Risk Management',
    icon: 'shield-alert',
    isDivider: false,
    children: [
      { label: 'Risk Dashboard', path: '/risk/dashboard', icon: 'shield-alert', isDivider: false },
      { label: 'Risk Register', path: '/risk/register', icon: 'clipboard-list', isDivider: false },
      { label: 'Assessments', path: '/risk/assessments', icon: 'file-search', isDivider: false },
      { label: 'Treatment Plans', path: '/risk/treatment-plans', icon: 'heart-pulse', isDivider: false },
      { label: 'Reviews', path: '/risk/reviews', icon: 'refresh-cw', isDivider: false },
      { label: 'Heatmap', path: '/risk/heatmap', icon: 'thermometer', isDivider: false },
      { label: 'Reports', path: '/risk/reports', icon: 'reports', isDivider: false },
    ],
  },
  {    label: 'Workflow',
    icon: 'workflow',
    isDivider: false,
    children: [
      { label: 'My Queue', path: '/workflow/my-queue', icon: 'workflow', isDivider: false },
      { label: 'Verification', path: '/workflow/verification', icon: 'workflow', isDivider: false },
      { label: 'Approval', path: '/workflow/approval', icon: 'workflow', isDivider: false },
      { label: 'PMS Review', path: '/workflow/pms-review', icon: 'workflow', isDivider: false },
      { label: 'Auditor Review', path: '/workflow/auditor-review', icon: 'workflow', isDivider: false },
    ],
  },
  {
    label: 'HR',
    icon: 'users',
    isDivider: false,
    children: [
      { label: 'Employees', path: '/hr/employees', icon: 'users', isDivider: false },
      { label: 'Departments', path: '/hr/departments', icon: 'users', isDivider: false },
      { label: 'Units', path: '/hr/units', icon: 'users', isDivider: false },
      { label: 'Positions', path: '/hr/positions', icon: 'users', isDivider: false },
      { label: 'Contacts', path: '/hr/contacts', icon: 'users', isDivider: false },
      { label: 'Resumes', path: '/hr/resumes', icon: 'users', isDivider: false },
    ],
  },
  { label: 'Projects & Tasks', icon: 'layers', isDivider: false, children: [{ label: 'Tasks', path: '/tasks', icon: 'layers', isDivider: false }] },
  {
    label: 'Location',
    icon: 'globe',
    isDivider: false,
    children: [
      { label: 'Countries', path: '/location/countries', icon: 'globe', isDivider: false },
      { label: 'Provinces', path: '/location/provinces', icon: 'globe', isDivider: false },
      { label: 'Cities', path: '/location/cities', icon: 'globe', isDivider: false },
      { label: 'Suburbs', path: '/location/suburbs', icon: 'globe', isDivider: false },
      { label: 'Addresses', path: '/location/addresses', icon: 'globe', isDivider: false },
    ],
  },
  { label: 'Reports', path: '/reports', icon: 'reports', isDivider: false },
  { label: 'Divider', isDivider: true },
  {
    label: 'System Administration',
    icon: 'settings',
    isDivider: false,
    children: [
      { label: 'Users', path: '/system-administration/users', icon: 'users', isDivider: false },
      { label: 'Roles', path: '/system-administration/roles', icon: 'users-group', isDivider: false },
      { label: 'Permissions', path: '/system-administration/permissions', icon: 'key', isDivider: false },
      { label: 'Audit Logs', path: '/system-administration/audit-logs', icon: 'history', isDivider: false },
      { label: 'Role Implementation Audit', path: '/system-administration/role-implementation-audit', icon: 'history', isDivider: false },
      { label: 'Role Access Matrix', path: '/system-administration/role-access-matrix', icon: 'history', isDivider: false },
      { label: 'Permission Simulation', path: '/system-administration/permission-simulation', icon: 'history', isDivider: false },
      { label: 'System Coverage Audit', path: '/system-administration/system-coverage-audit', icon: 'history', isDivider: false },
      { label: 'Role Permission & CRUD Audit', path: '/system-administration/role-permission-crud-audit', icon: 'history', isDivider: false },
    ],
  },
  {
    label: 'Configuration',
    icon: 'settings',
    isDivider: false,
    children: [
      { label: 'Periods', path: '/admin/periods', icon: 'settings', isDivider: false },
      { label: 'Organisations', path: '/admin/organisations', icon: 'settings', isDivider: false },
      { label: 'Approval Setup', path: '/admin/approval-setup', icon: 'settings', isDivider: false },
      { label: 'Lookup Tables', path: '/admin/lookups', icon: 'settings', isDivider: false },
      { label: 'Budget Types', path: '/admin/budget-types', icon: 'settings', isDivider: false },
      { label: 'Strategic Goals', path: '/admin/strategic-goals', icon: 'settings', isDivider: false },
      { label: 'Strategic Objectives', path: '/admin/strategic-objectives', icon: 'settings', isDivider: false },
      { label: 'Units of Measure', path: '/admin/units-measure', icon: 'settings', isDivider: false },
      { label: 'KPAs', path: '/admin/kpas', icon: 'settings', isDivider: false },
      { label: 'Municipal KPAs', path: '/admin/municipal-kpas', icon: 'settings', isDivider: false },
      { label: 'Departmental Objectives', path: '/admin/departmental-objectives', icon: 'settings', isDivider: false },
      { label: 'Outputs', path: '/admin/outputs', icon: 'settings', isDivider: false },
      { label: 'Performance Objectives', path: '/admin/performance-objectives', icon: 'settings', isDivider: false },
      { label: 'Priority Issues', path: '/admin/priority-issues', icon: 'settings', isDivider: false },
      { label: 'Occupations', path: '/admin/occupations', icon: 'settings', isDivider: false },
      { label: 'Industries', path: '/admin/industries', icon: 'settings', isDivider: false },
    ],
  },
  { label: 'Settings', path: '/settings', icon: 'settings', isDivider: false },
];

interface AppContextType {
  userProfile: UserProfile | null;
  isAuthenticated: boolean;
  roles: string[];
  isSuperAdmin: boolean;
  permissions: string[];
  menuItems: MenuItem[];
  login: (email: string, password: string) => Promise<boolean>;
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
  const resolveMenuItems = (incomingRoles: string[], incomingMenu: MenuItem[]) => {
    if (!isSuperAdminRole(incomingRoles)) return incomingMenu;
    if (!incomingMenu.length || !incomingMenu.some(item => item.label === 'Performance Management')) {
      return buildFullMenu();
    }
    return incomingMenu;
  };

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

    if (storedUser && storedPermissions && storedMenu) {
      setUserProfile(JSON.parse(storedUser));
      const parsedRoles = storedRoles ? JSON.parse(storedRoles) : [];
      setRoles(parsedRoles);
      setPermissions(JSON.parse(storedPermissions));
      setMenuItems(resolveMenuItems(parsedRoles, JSON.parse(storedMenu)));
      setCurrentPathState(normalizePath(window.location.pathname || '/dashboard'));
    }
  }, []);

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

  const login = async (email: string, password: string): Promise<boolean> => {
    const result = await apiLogin({ email, password });
    if (result.success && result.data) {
      const data = result.data as LoginResponse;
      setUserProfile(data.user);
      setRoles(data.roles ?? []);
      setPermissions(data.permissions);
      setMenuItems(resolveMenuItems(data.roles ?? [], data.menu));
      safeSetItem('user_profile', JSON.stringify(data.user));
      safeSetItem('roles', JSON.stringify(data.roles ?? []));
      safeSetItem('permissions', JSON.stringify(data.permissions));
      safeSetItem('menu_items', JSON.stringify(resolveMenuItems(data.roles ?? [], data.menu)));
      setCurrentPath('/dashboard');
      return true;
    }
    return false;
  };

  const logout = () => {
    apiLogout();
    setUserProfile(null);
    setRoles([]);
    setPermissions([]);
    setMenuItems([]);
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
        isSuperAdmin: isSuperAdminRole(roles),
        permissions,
        menuItems,
        login,
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
