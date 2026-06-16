import React, { useEffect } from 'react';
import {
  Briefcase,
  FolderKanban,
  Globe,
  LayoutDashboard,
  Library,
  Target,
  Users,
  Settings,
  ChevronDown,
  Menu,
  Clock,
  LogOut,
  ClipboardList,
  BarChart3,
  Layers3,
} from 'lucide-react';
import { useApp } from '../../context/AppContext';
import type { MenuItem } from '../../types';

interface SidebarProps {
  className?: string;
}

const iconMap: Record<string, React.ReactNode> = {
  'dashboard': <LayoutDashboard className="w-5 h-5" />,
  'users': <Users className="w-5 h-5" />,
  'users-group': <Users className="w-5 h-5" />,
  'key': <Settings className="w-5 h-5" />,
  'history': <Clock className="w-5 h-5" />,
  'settings': <Settings className="w-5 h-5" />,
  'workflow': <ClipboardList className="w-5 h-5" />,
  'layers': <Layers3 className="w-5 h-5" />,
  'globe': <Globe className="w-5 h-5" />,
  'reports': <BarChart3 className="w-5 h-5" />,
  'briefcase': <Briefcase className="w-5 h-5" />,
  'projects': <FolderKanban className="w-5 h-5" />,
  'library': <Library className="w-5 h-5" />,
};

export function Sidebar({ className = '' }: SidebarProps) {
  const {
    sidebarCollapsed,
    toggleSidebar,
    expandedSidebarGroups,
    toggleSidebarGroup,
    expandSidebarGroup,
    setCurrentPath,
    currentPath,
    userProfile,
    menuItems,
    logout,
  } = useApp();

  useEffect(() => {
    if (currentPath.startsWith('/admin/') || currentPath.startsWith('/system-administration/')) {
      expandSidebarGroup('System Administration');
    }
  }, [currentPath, expandSidebarGroup]);

  const getIcon = (_label: string, iconKey: string | undefined, depth: number) => {
    if (iconKey && iconMap[iconKey]) {
      return (
        <span className={depth > 0 ? '[&_svg]:w-4 [&_svg]:h-4' : '[&_svg]:w-5 [&_svg]:h-5'}>
          {iconMap[iconKey]}
        </span>
      );
    }
    return <Target className={depth > 0 ? 'w-4 h-4' : 'w-5 h-5'} />;
  };

  const handleNavClick = (item: MenuItem) => {
    if (item.children && item.children.length > 0) {
      toggleSidebarGroup(item.label);
    } else if (item.path) {
      setCurrentPath(item.path);
    }
  };

  const isPathActive = (path?: string) => {
    if (!path) return false;
    if (currentPath === path) return true;
    if (path !== '/' && currentPath.startsWith(path)) return true;
    return false;
  };

  const renderNavItem = (item: MenuItem, depth = 0) => {
    const isActive = isPathActive(item.path);
    const isExpanded = expandedSidebarGroups.includes(item.label);
    const hasChildren = item.children && item.children.length > 0;
    const hasActiveChild = hasChildren ? (item.children?.some(child => isPathActive(child.path)) ?? false) : false;
    const isGroup = !!hasChildren;
    const isLeaf = !hasChildren;
    const showSubtleActive = isGroup && (isExpanded || hasActiveChild);

    if (item.isDivider) {
      return <div key={item.label} className="border-t border-secondary-200 dark:border-secondary-700 my-2" />;
    }

    const baseButton =
      'w-full flex items-center gap-3 text-left transition-colors duration-150 rounded-md focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-1 focus:ring-offset-white dark:focus:ring-offset-secondary-900';

    const padding = depth > 0 ? 'px-3 py-2 ml-2 pl-9' : 'px-3 py-2.5';

    const leafActive = 'bg-primary-600 text-white';
    const leafInactive = 'text-secondary-700 hover:bg-secondary-100 dark:text-secondary-300 dark:hover:bg-secondary-800';

    const groupExpanded = 'bg-primary-50 text-primary-700 dark:bg-primary-900/20 dark:text-primary-300';
    const groupInactive = 'text-secondary-700 hover:bg-secondary-100 dark:text-secondary-300 dark:hover:bg-secondary-800';

    const buttonClassName = `${baseButton} ${padding} ${
      isLeaf ? (isActive ? leafActive : leafInactive) : (showSubtleActive ? groupExpanded : groupInactive)
    }`;

    return (
      <div key={item.label}>
        <button
          onClick={() => handleNavClick(item)}
          className={buttonClassName}
        >
          <span className={depth > 0 ? 'text-secondary-500 dark:text-secondary-400' : ''}>
            {getIcon(item.label, item.icon, depth)}
          </span>
          {!sidebarCollapsed && (
            <>
              <span className="flex-1 text-sm font-medium">{item.label}</span>
              {hasChildren && (
                <span
                  className={`transition-transform duration-200 ${isExpanded ? 'rotate-0' : '-rotate-90'} ${
                    showSubtleActive ? 'text-primary-700 dark:text-primary-300' : 'text-secondary-400'
                  }`}
                >
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
      className={`fixed left-0 top-0 h-full overflow-hidden bg-white dark:bg-secondary-900 border-r border-secondary-200 dark:border-secondary-700 transition-[width] duration-300 z-30 flex flex-col ${className}`}
      style={{ width: sidebarCollapsed ? 64 : 256 }}
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
        {menuItems.map(item => renderNavItem(item))}
      </nav>

      {/* User section */}
      {userProfile && (
        <div className="p-3 border-t border-secondary-200 dark:border-secondary-700">
          {!sidebarCollapsed ? (
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-primary-100 dark:bg-primary-900 flex items-center justify-center overflow-hidden">
                  <span className="text-sm font-medium text-primary-700 dark:text-primary-300">
                    {userProfile.firstName[0]}{userProfile.lastName[0]}
                  </span>
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-secondary-900 dark:text-white truncate">
                    {userProfile.fullName}
                  </p>
                  <p className="text-xs text-secondary-500 dark:text-secondary-400 truncate">
                    {userProfile.email}
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
                <span className="text-sm font-medium text-primary-700">
                  {userProfile.firstName[0]}{userProfile.lastName[0]}
                </span>
              </div>
            </div>
          )}
        </div>
      )}
    </aside>
  );
}
