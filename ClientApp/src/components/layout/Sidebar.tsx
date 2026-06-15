import React, { useState } from 'react';
import {
  LayoutDashboard,
  Target,
  FileText,
  Users,
  Settings,
  ChevronDown,
  ChevronRight,
  Menu,
  CheckSquare,
  BarChart3,
  FolderKanban,
  UserCheck,
  Building2,
  MapPin,
  Clock,
  LogOut,
  Briefcase,
} from 'lucide-react';
import { useApp } from '../../context/AppContext';
import type { User } from '../../types';

interface NavItem {
  id: string;
  label: string;
  icon: React.ReactNode;
  path?: string;
  children?: NavItem[];
  roles?: User['role'][];
}

const navigation: NavItem[] = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    icon: <LayoutDashboard className="w-5 h-5" />,
    path: '/dashboard',
  },
  {
    id: 'opms',
    label: 'OPMS',
    icon: <Target className="w-5 h-5" />,
    children: [
      { id: 'opms-targets', label: 'OPMS Targets', icon: <Target className="w-4 h-4" />, path: '/opms/targets' },
      { id: 'opms-submissions', label: 'OPMS Submissions', icon: <FileText className="w-4 h-4" />, path: '/opms/submissions' },
      { id: 'vote-numbers', label: 'Vote Numbers', icon: <BarChart3 className="w-4 h-4" />, path: '/opms/vote-numbers' },
    ],
  },
  {
    id: 'ipms',
    label: 'IPMS',
    icon: <FolderKanban className="w-5 h-5" />,
    children: [
      { id: 'ipms-targets', label: 'IPMS Targets', icon: <FolderKanban className="w-4 h-4" />, path: '/ipms/targets' },
      { id: 'ipms-submissions', label: 'IPMS Submissions', icon: <FileText className="w-4 h-4" />, path: '/ipms/submissions' },
    ],
  },
  {
    id: 'kpi-library',
    label: 'KPI Library',
    icon: <BarChart3 className="w-5 h-5" />,
    path: '/kpi-library',
  },
  {
    id: 'workflow',
    label: 'Workflow',
    icon: <Clock className="w-5 h-5" />,
    children: [
      { id: 'my-queue', label: 'My Work Queue', icon: <Briefcase className="w-4 h-4" />, path: '/workflow/my-queue' },
      { id: 'verification', label: 'Verification', icon: <UserCheck className="w-4 h-4" />, path: '/workflow/verification' },
      { id: 'approval', label: 'Approval', icon: <CheckSquare className="w-4 h-4" />, path: '/workflow/approval' },
      { id: 'pms-review', label: 'PMS Review', icon: <FileText className="w-4 h-4" />, path: '/workflow/pms-review' },
      { id: 'auditor-review', label: 'Auditor Review', icon: <FileText className="w-4 h-4" />, path: '/workflow/auditor-review' },
    ],
  },
  {
    id: 'hr',
    label: 'Human Resources',
    icon: <Users className="w-5 h-5" />,
    children: [
      { id: 'employees', label: 'Employees', icon: <Users className="w-4 h-4" />, path: '/hr/employees' },
      { id: 'departments', label: 'Departments', icon: <Building2 className="w-4 h-4" />, path: '/hr/departments' },
      { id: 'units', label: 'Department Units', icon: <Building2 className="w-4 h-4" />, path: '/hr/units' },
      { id: 'positions', label: 'Positions', icon: <Briefcase className="w-4 h-4" />, path: '/hr/positions' },
      { id: 'contacts', label: 'Contacts', icon: <Users className="w-4 h-4" />, path: '/hr/contacts' },
      { id: 'resumes', label: 'Resumes', icon: <FileText className="w-4 h-4" />, path: '/hr/resumes' },
    ],
  },
  {
    id: 'tasks',
    label: 'Tasks',
    icon: <CheckSquare className="w-5 h-5" />,
    path: '/tasks',
  },
  {
    id: 'admin',
    label: 'Administration',
    icon: <Settings className="w-5 h-5" />,
    children: [
      { id: 'periods', label: 'Periods', icon: <Clock className="w-4 h-4" />, path: '/admin/periods' },
      { id: 'organisations', label: 'Organisations', icon: <Building2 className="w-4 h-4" />, path: '/admin/organisations' },
      { id: 'approval-setup', label: 'Approval Setup', icon: <CheckSquare className="w-4 h-4" />, path: '/admin/approval-setup' },
      { id: 'budget-types', label: 'Budget Types', icon: <BarChart3 className="w-4 h-4" />, path: '/admin/budget-types' },
      { id: 'strategic-goals', label: 'Strategic Goals', icon: <Target className="w-4 h-4" />, path: '/admin/strategic-goals' },
      { id: 'strategic-objectives', label: 'Strategic Objectives', icon: <Target className="w-4 h-4" />, path: '/admin/strategic-objectives' },
      { id: 'units-measure', label: 'Units of Measure', icon: <BarChart3 className="w-4 h-4" />, path: '/admin/units-measure' },
      { id: 'kpas', label: 'KPAs (National)', icon: <BarChart3 className="w-4 h-4" />, path: '/admin/kpas' },
      { id: 'municipal-kpas', label: 'KPAs (Municipal)', icon: <BarChart3 className="w-4 h-4" />, path: '/admin/municipal-kpas' },
      { id: 'departmental-objectives', label: 'Dept Objectives', icon: <Target className="w-4 h-4" />, path: '/admin/departmental-objectives' },
      { id: 'outputs', label: 'Outputs', icon: <FileText className="w-4 h-4" />, path: '/admin/outputs' },
      { id: 'performance-objectives', label: 'Perf Objectives', icon: <Target className="w-4 h-4" />, path: '/admin/performance-objectives' },
      { id: 'priority-issues', label: 'Priority Issues', icon: <FileText className="w-4 h-4" />, path: '/admin/priority-issues' },
      { id: 'occupations', label: 'Occupations', icon: <Briefcase className="w-4 h-4" />, path: '/admin/occupations' },
      { id: 'industries', label: 'Industries', icon: <Building2 className="w-4 h-4" />, path: '/admin/industries' },
      { id: 'lookups', label: 'Lookup Tables', icon: <Settings className="w-4 h-4" />, path: '/admin/lookups' },
    ],
  },
  {
    id: 'location',
    label: 'Location Setup',
    icon: <MapPin className="w-5 h-5" />,
    children: [
      { id: 'countries', label: 'Countries', icon: <MapPin className="w-4 h-4" />, path: '/location/countries' },
      { id: 'provinces', label: 'Provinces', icon: <MapPin className="w-4 h-4" />, path: '/location/provinces' },
      { id: 'cities', label: 'Cities', icon: <MapPin className="w-4 h-4" />, path: '/location/cities' },
      { id: 'suburbs', label: 'Suburbs', icon: <MapPin className="w-4 h-4" />, path: '/location/suburbs' },
      { id: 'addresses', label: 'Addresses', icon: <MapPin className="w-4 h-4" />, path: '/location/addresses' },
    ],
  },
  {
    id: 'reports',
    label: 'Reports',
    icon: <BarChart3 className="w-5 h-5" />,
    path: '/reports',
  },
  {
    id: 'settings',
    label: 'Settings',
    icon: <Settings className="w-5 h-5" />,
    path: '/settings',
  },
];

interface SidebarProps {
  className?: string;
}

export function Sidebar({ className = '' }: SidebarProps) {
  const { sidebarCollapsed, toggleSidebar, setCurrentPath, currentPath, user, logout } = useApp();
  const [expandedItems, setExpandedItems] = useState<string[]>(['opms', 'ipms']);

  const toggleExpanded = (id: string) => {
    setExpandedItems(prev =>
      prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
    );
  };

  const handleNavClick = (item: NavItem) => {
    if (item.children) {
      toggleExpanded(item.id);
    } else if (item.path) {
      setCurrentPath(item.path);
    }
  };

  const renderNavItem = (item: NavItem, depth = 0) => {
    const isActive = currentPath === item.path;
    const isExpanded = expandedItems.includes(item.id);
    const hasChildren = item.children && item.children.length > 0;

    return (
      <div key={item.id}>
        <button
          onClick={() => handleNavClick(item)}
          className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-left transition-all duration-200 ${
            depth > 0 ? 'ml-4 pl-6' : ''
          } ${
            isActive
              ? 'bg-primary-600 text-white shadow-md'
              : 'text-secondary-600 hover:bg-secondary-100 dark:text-secondary-300 dark:hover:bg-secondary-800'
          }`}
        >
          {item.icon}
          {!sidebarCollapsed && (
            <>
              <span className="flex-1 text-sm font-medium">{item.label}</span>
              {hasChildren && (
                <span className={`transition-transform duration-200 ${isExpanded ? 'rotate-0' : '-rotate-90'}`}>
                  <ChevronDown className="w-4 h-4" />
                </span>
              )}
            </>
          )}
        </button>
        {!sidebarCollapsed && hasChildren && isExpanded && (
          <div className="mt-1 space-y-1">
            {item.children!.map(child => renderNavItem(child, depth + 1))}
          </div>
        )}
      </div>
    );
  };

  return (
    <aside
      className={`fixed left-0 top-0 h-full bg-white dark:bg-secondary-900 border-r border-secondary-200 dark:border-secondary-700 transition-all duration-300 z-30 flex flex-col ${
        sidebarCollapsed ? 'w-16' : 'w-64'
      } ${className}`}
    >
      {/* Logo */}
      <div className="h-16 flex items-center justify-between px-4 border-b border-secondary-200 dark:border-secondary-700">
        {!sidebarCollapsed && (
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 bg-primary-600 rounded-lg flex items-center justify-center">
              <Target className="w-5 h-5 text-white" />
            </div>
            <span className="font-bold text-lg text-secondary-900 dark:text-white">PMS</span>
          </div>
        )}
        <button
          onClick={toggleSidebar}
          className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors"
        >
          <Menu className="w-5 h-5 text-secondary-500" />
        </button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto p-3 space-y-1">
        {navigation.map(item => renderNavItem(item))}
      </nav>

      {/* User section */}
      {user && (
        <div className="p-3 border-t border-secondary-200 dark:border-secondary-700">
          {!sidebarCollapsed ? (
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-primary-100 dark:bg-primary-900 flex items-center justify-center overflow-hidden">
                  {user.avatarUrl ? (
                    <img src={user.avatarUrl} alt={user.displayName} className="w-full h-full object-cover" />
                  ) : (
                    <span className="text-sm font-medium text-primary-700 dark:text-primary-300">
                      {user.firstName[0]}{user.lastName[0]}
                    </span>
                  )}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-secondary-900 dark:text-white truncate">
                    {user.displayName}
                  </p>
                  <p className="text-xs text-secondary-500 dark:text-secondary-400 truncate">
                    {user.role.replace('_', ' ')}
                  </p>
                </div>
              </div>
              <button
                onClick={logout}
                className="w-full flex items-center gap-2 px-3 py-2 text-sm text-error-600 hover:bg-error-50 dark:text-error-400 dark:hover:bg-error-900/20 rounded-lg transition-colors"
              >
                <LogOut className="w-4 h-4" />
                Sign Out
              </button>
            </div>
          ) : (
            <div className="flex justify-center">
              <div className="w-10 h-10 rounded-full bg-primary-100 dark:bg-primary-900 flex items-center justify-center overflow-hidden">
                {user.avatarUrl ? (
                  <img src={user.avatarUrl} alt={user.displayName} className="w-full h-full object-cover" />
                ) : (
                  <span className="text-sm font-medium text-primary-700">
                    {user.firstName[0]}{user.lastName[0]}
                  </span>
                )}
              </div>
            </div>
          )}
        </div>
      )}
    </aside>
  );
}
