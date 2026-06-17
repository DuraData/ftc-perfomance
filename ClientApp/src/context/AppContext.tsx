import { createContext, useContext, useState, ReactNode, useEffect, useMemo } from 'react';
import type {
  UserRole,
  UserProfile,
  LoginResponse,
  MenuItem,
  OPMSTarget,
  IPMSTarget,
  OPMSSubmission,
  IPMSSubmission,
  Attachment,
} from '../types';
import { login as apiLogin, isAuthenticated, logout as apiLogout } from '../api/api';
import {
  mockOPMSTargets,
  mockIPMSTargets,
  mockOPMSSubmissions,
  mockIPMSSubmissions,
} from '../data/mockData';

type ToastType = 'success' | 'error' | 'info';
interface ToastItem {
  id: string;
  type: ToastType;
  message: string;
}

const isSystemAdministratorRole = (roles: string[]) =>
  roles.some(role =>
    ['Super Admin', 'System Admin', 'System Administrator', 'EPMS Admin', 'ICT Admin', 'ICT Sub-Admin']
      .some(r => r.toLowerCase() === role.toLowerCase()),
  );

const buildFullMenu = (): MenuItem[] => [
  { label: 'Dashboard', path: '/dashboard', icon: 'dashboard', isDivider: false },
  {
    label: 'Performance Management',
    icon: 'target',
    isDivider: false,
    children: [
      { label: 'OPMS Targets', path: '/opms/targets', icon: 'target', isDivider: false },
      { label: 'OPMS Submissions', path: '/opms/submissions', icon: 'target', isDivider: false },
      { label: 'Vote Numbers', path: '/opms/vote-numbers', icon: 'layers', isDivider: false },
      { label: 'IPMS Targets', path: '/ipms/targets', icon: 'target', isDivider: false },
      { label: 'IPMS Submissions', path: '/ipms/submissions', icon: 'target', isDivider: false },
      { label: 'KPI Library', path: '/kpi-library', icon: 'library', isDivider: false },
    ],
  },
  {
    label: 'Workflow',
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
  opmsTargets: OPMSTarget[];
  ipmsTargets: IPMSTarget[];
  opmsSubmissions: OPMSSubmission[];
  ipmsSubmissions: IPMSSubmission[];
  createOPMSTarget: (target: OPMSTarget) => OPMSTarget;
  updateOPMSTarget: (target: OPMSTarget) => void;
  deleteOPMSTarget: (id: string) => void;
  duplicateOPMSTarget: (id: string) => OPMSTarget | null;
  createIPMSTarget: (target: IPMSTarget) => IPMSTarget;
  updateIPMSTarget: (target: IPMSTarget) => void;
  deleteIPMSTarget: (id: string) => void;
  duplicateIPMSTarget: (id: string) => IPMSTarget | null;
  createOPMSSubmission: (submission: OPMSSubmission) => OPMSSubmission;
  updateOPMSSubmission: (submission: OPMSSubmission) => void;
  deleteOPMSSubmission: (id: string) => void;
  createIPMSSubmission: (submission: IPMSSubmission) => IPMSSubmission;
  updateIPMSSubmission: (submission: IPMSSubmission) => void;
  deleteIPMSSubmission: (id: string) => void;
  updateOPMSTargetAttachments: (targetId: string, attachments: Attachment[]) => void;
  updateIPMSTargetAttachments: (targetId: string, attachments: Attachment[]) => void;
  updateOPMSSubmissionAttachments: (submissionId: string, attachments: Attachment[]) => void;
  updateIPMSSubmissionAttachments: (submissionId: string, attachments: Attachment[]) => void;
}

const AppContext = createContext<AppContextType | undefined>(undefined);

const PERFORMANCE_STORAGE_KEYS = {
  opmsTargets: 'perf_opms_targets',
  ipmsTargets: 'perf_ipms_targets',
  opmsSubmissions: 'perf_opms_submissions',
  ipmsSubmissions: 'perf_ipms_submissions',
} as const;

function safeParseStoredValue<T>(key: string, fallback: T): T {
  try {
    const raw = localStorage.getItem(key);
    return raw ? JSON.parse(raw) as T : fallback;
  } catch {
    return fallback;
  }
}

function createEntityId(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function synchronizePerformanceData(
  opmsTargets: OPMSTarget[],
  ipmsTargets: IPMSTarget[],
  opmsSubmissions: OPMSSubmission[],
  ipmsSubmissions: IPMSSubmission[],
) {
  const opmsById = new Map(opmsTargets.map(target => [target.id, target]));
  const syncedIpmsTargets = ipmsTargets.map(target => ({
    ...target,
    relatedOPMSTarget: target.relatedOPMSTarget ? opmsById.get(target.relatedOPMSTarget.id) : undefined,
  }));

  const linkedIpmsByOpms = new Map<string, IPMSTarget[]>();
  syncedIpmsTargets.forEach(target => {
    const relatedId = target.relatedOPMSTarget?.id;
    if (!relatedId) return;
    const existing = linkedIpmsByOpms.get(relatedId) ?? [];
    linkedIpmsByOpms.set(relatedId, [...existing, target]);
  });

  const syncedOpmsTargets = opmsTargets.map(target => ({
    ...target,
    relatedIPMSTargets: linkedIpmsByOpms.get(target.id) ?? [],
  }));

  const syncedOpmsById = new Map(syncedOpmsTargets.map(target => [target.id, target]));
  const syncedIpmsById = new Map(syncedIpmsTargets.map(target => [target.id, target]));

  return {
    opmsTargets: syncedOpmsTargets,
    ipmsTargets: syncedIpmsTargets,
    opmsSubmissions: opmsSubmissions.map(submission => ({
      ...submission,
      target: syncedOpmsById.get(submission.target.id) ?? submission.target,
    })),
    ipmsSubmissions: ipmsSubmissions.map(submission => ({
      ...submission,
      target: syncedIpmsById.get(submission.target.id) ?? submission.target,
    })),
  };
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
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [expandedSidebarGroups, setExpandedSidebarGroups] = useState<string[]>([]);
  const [darkMode, setDarkMode] = useState(false);
  const [rawOpmsTargets, setRawOpmsTargets] = useState<OPMSTarget[]>(() => safeParseStoredValue(PERFORMANCE_STORAGE_KEYS.opmsTargets, mockOPMSTargets));
  const [rawIpmsTargets, setRawIpmsTargets] = useState<IPMSTarget[]>(() => safeParseStoredValue(PERFORMANCE_STORAGE_KEYS.ipmsTargets, mockIPMSTargets));
  const [rawOpmsSubmissions, setRawOpmsSubmissions] = useState<OPMSSubmission[]>(() => safeParseStoredValue(PERFORMANCE_STORAGE_KEYS.opmsSubmissions, mockOPMSSubmissions));
  const [rawIpmsSubmissions, setRawIpmsSubmissions] = useState<IPMSSubmission[]>(() => safeParseStoredValue(PERFORMANCE_STORAGE_KEYS.ipmsSubmissions, mockIPMSSubmissions));
  const normalizePath = (path: string) => {
    if (path.startsWith('/admin/users')) return '/system-administration/users';
    if (path.startsWith('/admin/roles')) return '/system-administration/roles';
    if (path.startsWith('/admin/permissions')) return '/system-administration/permissions';
    if (path.startsWith('/admin/audit')) return '/system-administration/audit-logs';
    return path;
  };

  const [currentPath, setCurrentPathState] = useState(isAuthenticated() ? normalizePath(window.location.pathname || '/dashboard') : '/login');
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const performanceData = useMemo(
    () => synchronizePerformanceData(rawOpmsTargets, rawIpmsTargets, rawOpmsSubmissions, rawIpmsSubmissions),
    [rawOpmsTargets, rawIpmsTargets, rawOpmsSubmissions, rawIpmsSubmissions],
  );
  const { opmsTargets, ipmsTargets, opmsSubmissions, ipmsSubmissions } = performanceData;
  const resolveMenuItems = (incomingRoles: string[], incomingMenu: MenuItem[]) => {
    if (!isSystemAdministratorRole(incomingRoles)) return incomingMenu;
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
    safeSetItem(PERFORMANCE_STORAGE_KEYS.opmsTargets, JSON.stringify(rawOpmsTargets));
  }, [rawOpmsTargets]);

  useEffect(() => {
    safeSetItem(PERFORMANCE_STORAGE_KEYS.ipmsTargets, JSON.stringify(rawIpmsTargets));
  }, [rawIpmsTargets]);

  useEffect(() => {
    safeSetItem(PERFORMANCE_STORAGE_KEYS.opmsSubmissions, JSON.stringify(rawOpmsSubmissions));
  }, [rawOpmsSubmissions]);

  useEffect(() => {
    safeSetItem(PERFORMANCE_STORAGE_KEYS.ipmsSubmissions, JSON.stringify(rawIpmsSubmissions));
  }, [rawIpmsSubmissions]);

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

  const pushToast = (type: ToastType, message: string) => {
    const id = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    const toast: ToastItem = { id, type, message };
    setToasts(prev => [...prev, toast]);
    window.setTimeout(() => {
      setToasts(prev => prev.filter(t => t.id !== id));
    }, 3000);
  };

  const toggleDarkMode = () => {
    setDarkMode(prev => !prev);
    if (!darkMode) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  };

  const switchRole = (_role: UserRole) => {
    // Role switching to be implemented later
  };

  const createOPMSTarget = (target: OPMSTarget) => {
    const nextTarget = { ...target, id: target.id || createEntityId('opms-target') };
    setRawOpmsTargets(prev => [...prev, nextTarget]);
    return nextTarget;
  };

  const updateOPMSTarget = (target: OPMSTarget) => {
    setRawOpmsTargets(prev => prev.map(item => item.id === target.id ? target : item));
    setRawOpmsSubmissions(prev => prev.map(item => item.target.id === target.id ? { ...item, target } : item));
  };

  const deleteOPMSTarget = (id: string) => {
    setRawOpmsTargets(prev => prev.filter(item => item.id !== id));
    setRawOpmsSubmissions(prev => prev.filter(item => item.target.id !== id));
    setRawIpmsTargets(prev => prev.map(item => item.relatedOPMSTarget?.id === id ? { ...item, relatedOPMSTarget: undefined } : item));
  };

  const duplicateOPMSTarget = (id: string) => {
    const source = opmsTargets.find(target => target.id === id);
    if (!source) return null;
    const duplicate = {
      ...source,
      id: createEntityId('opms-target'),
      indicatorNumber: `${source.indicatorNumber}-COPY`,
      targetName: `${source.targetName} (Copy)`,
      submissions: [],
      attachments: [...(source.attachments ?? [])],
      relatedIPMSTargets: [],
    };
    setRawOpmsTargets(prev => [...prev, duplicate]);
    return duplicate;
  };

  const createIPMSTarget = (target: IPMSTarget) => {
    const nextTarget = { ...target, id: target.id || createEntityId('ipms-target') };
    setRawIpmsTargets(prev => [...prev, nextTarget]);
    return nextTarget;
  };

  const updateIPMSTarget = (target: IPMSTarget) => {
    setRawIpmsTargets(prev => prev.map(item => item.id === target.id ? target : item));
    setRawIpmsSubmissions(prev => prev.map(item => item.target.id === target.id ? { ...item, target } : item));
  };

  const deleteIPMSTarget = (id: string) => {
    setRawIpmsTargets(prev => prev.filter(item => item.id !== id));
    setRawIpmsSubmissions(prev => prev.filter(item => item.target.id !== id));
  };

  const duplicateIPMSTarget = (id: string) => {
    const source = ipmsTargets.find(target => target.id === id);
    if (!source) return null;
    const duplicate = {
      ...source,
      id: createEntityId('ipms-target'),
      indicatorNumber: `${source.indicatorNumber}-COPY`,
      targetName: `${source.targetName} (Copy)`,
      submissions: [],
      attachments: [...(source.attachments ?? [])],
    };
    setRawIpmsTargets(prev => [...prev, duplicate]);
    return duplicate;
  };

  const createOPMSSubmission = (submission: OPMSSubmission) => {
    const nextSubmission = { ...submission, id: submission.id || createEntityId('opms-sub') };
    setRawOpmsSubmissions(prev => [...prev, nextSubmission]);
    return nextSubmission;
  };

  const updateOPMSSubmission = (submission: OPMSSubmission) => {
    setRawOpmsSubmissions(prev => prev.map(item => item.id === submission.id ? submission : item));
  };

  const deleteOPMSSubmission = (id: string) => {
    setRawOpmsSubmissions(prev => prev.filter(item => item.id !== id));
  };

  const createIPMSSubmission = (submission: IPMSSubmission) => {
    const nextSubmission = { ...submission, id: submission.id || createEntityId('ipms-sub') };
    setRawIpmsSubmissions(prev => [...prev, nextSubmission]);
    return nextSubmission;
  };

  const updateIPMSSubmission = (submission: IPMSSubmission) => {
    setRawIpmsSubmissions(prev => prev.map(item => item.id === submission.id ? submission : item));
  };

  const deleteIPMSSubmission = (id: string) => {
    setRawIpmsSubmissions(prev => prev.filter(item => item.id !== id));
  };

  const updateOPMSTargetAttachments = (targetId: string, attachments: Attachment[]) => {
    setRawOpmsTargets(prev => prev.map(item => item.id === targetId ? { ...item, attachments } : item));
  };

  const updateIPMSTargetAttachments = (targetId: string, attachments: Attachment[]) => {
    setRawIpmsTargets(prev => prev.map(item => item.id === targetId ? { ...item, attachments } : item));
  };

  const updateOPMSSubmissionAttachments = (submissionId: string, attachments: Attachment[]) => {
    setRawOpmsSubmissions(prev => prev.map(item => item.id === submissionId ? { ...item, attachments } : item));
  };

  const updateIPMSSubmissionAttachments = (submissionId: string, attachments: Attachment[]) => {
    setRawIpmsSubmissions(prev => prev.map(item => item.id === submissionId ? { ...item, attachments } : item));
  };

  return (
    <AppContext.Provider
      value={{
        userProfile,
        isAuthenticated: isAuthenticated() && !!userProfile,
        roles,
        isSuperAdmin: isSystemAdministratorRole(roles),
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
        opmsTargets,
        ipmsTargets,
        opmsSubmissions,
        ipmsSubmissions,
        createOPMSTarget,
        updateOPMSTarget,
        deleteOPMSTarget,
        duplicateOPMSTarget,
        createIPMSTarget,
        updateIPMSTarget,
        deleteIPMSTarget,
        duplicateIPMSTarget,
        createOPMSSubmission,
        updateOPMSSubmission,
        deleteOPMSSubmission,
        createIPMSSubmission,
        updateIPMSSubmission,
        deleteIPMSSubmission,
        updateOPMSTargetAttachments,
        updateIPMSTargetAttachments,
        updateOPMSSubmissionAttachments,
        updateIPMSSubmissionAttachments,
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
