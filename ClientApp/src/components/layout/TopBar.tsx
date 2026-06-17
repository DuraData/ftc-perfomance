import { useEffect, useMemo, useState } from 'react';
import {
  Search,
  Bell,
  Moon,
  Sun,
  ChevronDown,
  User,
  Settings,
  LogOut,
} from 'lucide-react';
import { useApp } from '../../context/AppContext';
import { getNotifications, markNotificationRead } from '../../api/api';
import type { NotificationDto } from '../../types';
import { Button } from '../ui';

interface TopBarProps {
  title?: string;
  subtitle?: string;
}

type SettingsTabId = 'profile' | 'notifications' | 'appearance' | 'security';

export function TopBar({ title, subtitle }: TopBarProps) {
  const { userProfile, darkMode, toggleDarkMode, logout, setCurrentPath, pushToast } = useApp();
  const [showUserMenu, setShowUserMenu] = useState(false);
  const [showNotifications, setShowNotifications] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [loadingNotifications, setLoadingNotifications] = useState(false);

  const loadNotifications = async () => {
    setLoadingNotifications(true);
    const result = await getNotifications();
    if (result.success && result.data) {
      setNotifications(result.data);
    } else {
      pushToast('error', result.message ?? 'Failed to load notifications');
    }
    setLoadingNotifications(false);
  };

  useEffect(() => {
    void loadNotifications();
  }, []);

  const unreadCount = notifications.filter(n => !n.isRead).length;

  const recentNotifications = useMemo(
    () => notifications
      .slice()
      .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
      .slice(0, 8),
    [notifications],
  );

  const formatNotificationTime = (value: string) => {
    const diffMs = Date.now() - new Date(value).getTime();
    const diffMinutes = Math.max(1, Math.floor(diffMs / 60000));
    if (diffMinutes < 60) return `${diffMinutes} min ago`;
    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours < 24) return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;
    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`;
  };

  const openSettings = (tab: SettingsTabId = 'profile') => {
    try {
      localStorage.setItem('settings_active_tab', tab);
    } catch {
      // ignore storage issues and continue with navigation
    }
    setShowUserMenu(false);
    setShowNotifications(false);
    setCurrentPath('/settings');
  };

  const toggleNotifications = () => {
    setShowUserMenu(false);
    setShowNotifications(prev => !prev);
  };

  const toggleUserMenu = () => {
    setShowNotifications(false);
    setShowUserMenu(prev => !prev);
  };

  return (
    <header className="h-16 bg-white dark:bg-secondary-900 border-b border-secondary-200 dark:border-secondary-700 flex items-center justify-between px-6">
      {/* Left section - Title */}
      <div className="flex items-center gap-4">
        {title && (
          <div>
            <h1 className="text-lg font-semibold text-secondary-900 dark:text-white">{title}</h1>
            {subtitle && (
              <p className="text-sm text-secondary-500 dark:text-secondary-400">{subtitle}</p>
            )}
          </div>
        )}
      </div>

      {/* Right section */}
      <div className="flex items-center gap-4">
        {/* Search */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-secondary-400" />
          <input
            type="text"
            placeholder="Search..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-64 pl-10 pr-4 py-2 text-sm bg-secondary-50 dark:bg-secondary-800 border border-secondary-200 dark:border-secondary-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent transition-all"
          />
        </div>

        {/* Dark mode toggle */}
        <button
          onClick={toggleDarkMode}
          className="p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors"
          title={darkMode ? 'Light mode' : 'Dark mode'}
        >
          {darkMode ? (
            <Sun className="w-5 h-5 text-secondary-500 dark:text-secondary-400" />
          ) : (
            <Moon className="w-5 h-5 text-secondary-500" />
          )}
        </button>

        {/* Notifications */}
        <div className="relative">
          <button
            className="p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors relative"
            onClick={toggleNotifications}
          >
            <Bell className="w-5 h-5 text-secondary-500 dark:text-secondary-400" />
            {unreadCount > 0 && (
              <span className="absolute top-1 right-1 w-4 h-4 bg-error-500 text-white text-xs rounded-full flex items-center justify-center">
                {unreadCount}
              </span>
            )}
          </button>
          {showNotifications && (
            <>
              <div className="fixed inset-0 z-10" onClick={() => setShowNotifications(false)} />
              <div className="absolute right-0 top-full z-20 mt-1 w-96 rounded-lg border border-secondary-200 bg-white shadow-lg dark:border-secondary-700 dark:bg-secondary-800">
                <div className="flex items-center justify-between border-b border-secondary-200 px-4 py-3 dark:border-secondary-700">
                  <div>
                    <p className="text-sm font-semibold text-secondary-900 dark:text-white">Notifications</p>
                    <p className="text-xs text-secondary-500 dark:text-secondary-400">{unreadCount} unread</p>
                  </div>
                  <Button variant="ghost" size="sm" onClick={() => { void loadNotifications(); }}>
                    Refresh
                  </Button>
                </div>
                <div className="max-h-96 overflow-y-auto">
                  {loadingNotifications ? (
                    <div className="px-4 py-6 text-sm text-secondary-500 dark:text-secondary-400">Loading notifications...</div>
                  ) : recentNotifications.length === 0 ? (
                    <div className="px-4 py-6 text-sm text-secondary-500 dark:text-secondary-400">No notifications yet.</div>
                  ) : recentNotifications.map(notification => (
                    <button
                      key={notification.id}
                      className="w-full border-b border-secondary-100 px-4 py-3 text-left hover:bg-secondary-50 dark:border-secondary-700 dark:hover:bg-secondary-700/50"
                      onClick={() => {
                        void (async () => {
                          if (!notification.isRead) {
                            const result = await markNotificationRead(notification.id);
                            if (result.success) {
                              setNotifications(prev => prev.map(item => item.id === notification.id ? { ...item, isRead: true } : item));
                            }
                          }
                          setShowNotifications(false);
                          if (notification.entityName?.toLowerCase().includes('submission')) {
                            setCurrentPath(notification.entityName.toLowerCase().includes('opms') ? '/opms/submissions' : '/ipms/submissions');
                          } else if (notification.entityName?.toLowerCase().includes('target')) {
                            setCurrentPath(notification.entityName.toLowerCase().includes('opms') ? '/opms/targets' : '/ipms/targets');
                          } else {
                            setCurrentPath('/system-administration/audit-logs');
                          }
                        })();
                      }}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <p className={`text-sm ${notification.isRead ? 'font-medium text-secondary-700 dark:text-secondary-200' : 'font-semibold text-secondary-900 dark:text-white'}`}>
                            {notification.title}
                          </p>
                          <p className="mt-1 text-xs text-secondary-500 dark:text-secondary-400">{notification.message}</p>
                        </div>
                        {!notification.isRead && <span className="mt-1 h-2.5 w-2.5 rounded-full bg-primary-600" />}
                      </div>
                      <p className="mt-2 text-[11px] text-secondary-400 dark:text-secondary-500">{formatNotificationTime(notification.createdAt)}</p>
                    </button>
                  ))}
                </div>
                <div className="border-t border-secondary-200 px-4 py-2 dark:border-secondary-700">
                  <button
                    className="text-xs font-medium text-primary-600 hover:text-primary-700 dark:text-primary-400"
                    onClick={() => openSettings('notifications')}
                  >
                    Open settings
                  </button>
                </div>
              </div>
            </>
          )}
        </div>

        {/* User menu */}
        {userProfile && (
          <div className="relative">
            <button
              onClick={toggleUserMenu}
              className="flex items-center gap-3 px-3 py-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors"
            >
              <div className="w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-900 flex items-center justify-center overflow-hidden">
                <span className="text-xs font-medium text-primary-700 dark:text-primary-300">
                  {userProfile.firstName[0]}{userProfile.lastName[0]}
                </span>
              </div>
              <div className="hidden sm:block text-left">
                <p className="text-sm font-medium text-secondary-900 dark:text-white">{userProfile.fullName}</p>
              </div>
              <ChevronDown className="w-4 h-4 text-secondary-400" />
            </button>
            {showUserMenu && (
              <>
                <div className="fixed inset-0 z-10" onClick={() => setShowUserMenu(false)} />
                <div className="absolute right-0 top-full mt-1 w-48 bg-white dark:bg-secondary-800 rounded-lg shadow-lg border border-secondary-200 dark:border-secondary-700 py-1 z-20">
                  <div className="px-4 py-3 border-b border-secondary-200 dark:border-secondary-700">
                    <p className="text-sm font-medium text-secondary-900 dark:text-white">{userProfile.fullName}</p>
                    <p className="text-xs text-secondary-500 dark:text-secondary-400">{userProfile.email}</p>
                  </div>
                  <button
                    onClick={() => openSettings('profile')}
                    className="w-full flex items-center gap-2 px-4 py-2 text-sm text-secondary-700 dark:text-secondary-300 hover:bg-secondary-50 dark:hover:bg-secondary-700 transition-colors"
                  >
                    <User className="w-4 h-4" />
                    Profile
                  </button>
                  <button
                    onClick={() => openSettings('security')}
                    className="w-full flex items-center gap-2 px-4 py-2 text-sm text-secondary-700 dark:text-secondary-300 hover:bg-secondary-50 dark:hover:bg-secondary-700 transition-colors"
                  >
                    <Settings className="w-4 h-4" />
                    Settings
                  </button>
                  <div className="border-t border-secondary-200 dark:border-secondary-700 mt-1 pt-1">
                    <button
                      onClick={() => {
                        logout();
                        setShowUserMenu(false);
                      }}
                      className="w-full flex items-center gap-2 px-4 py-2 text-sm text-error-600 dark:text-error-400 hover:bg-error-50 dark:hover:bg-error-900/20 transition-colors">
                      <LogOut className="w-4 h-4" />
                      Sign out
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>
        )}
      </div>
    </header>
  );
}
