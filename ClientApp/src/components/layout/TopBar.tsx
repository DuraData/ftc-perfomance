import React, { useState } from 'react';
import {
  Search,
  Bell,
  Moon,
  Sun,
  ChevronDown,
  User,
  Settings,
  LogOut,
  Shield,
} from 'lucide-react';
import { useApp } from '../../context/AppContext';
import type { UserRole } from '../../types';

const roleOptions: { value: UserRole; label: string }[] = [
  { value: 'system_admin', label: 'System Administrator' },
  { value: 'pms_officer', label: 'PMS Officer' },
  { value: 'department_manager', label: 'Department Manager' },
  { value: 'submitter', label: 'Submitter' },
  { value: 'verifier', label: 'Verifier' },
  { value: 'approver', label: 'Approver' },
  { value: 'auditor', label: 'Auditor' },
  { value: 'hr_admin', label: 'HR Administrator' },
  { value: 'viewer', label: 'Viewer / Executive' },
];

interface TopBarProps {
  title?: string;
  subtitle?: string;
}

export function TopBar({ title, subtitle }: TopBarProps) {
  const { userProfile, darkMode, toggleDarkMode, logout } = useApp();
  const [showUserMenu, setShowUserMenu] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');

  const notifications = [
    { id: 1, message: 'Q2 submission pending verification', time: '5 min ago', read: false },
    { id: 2, message: 'New task assigned to you', time: '1 hour ago', read: false },
    { id: 3, message: 'Submission approved by manager', time: '2 hours ago', read: true },
  ];

  const unreadCount = notifications.filter(n => !n.read).length;

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
          <button className="p-2 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-800 transition-colors relative">
            <Bell className="w-5 h-5 text-secondary-500 dark:text-secondary-400" />
            {unreadCount > 0 && (
              <span className="absolute top-1 right-1 w-4 h-4 bg-error-500 text-white text-xs rounded-full flex items-center justify-center">
                {unreadCount}
              </span>
            )}
          </button>
        </div>

        {/* User menu */}
        {userProfile && (
          <div className="relative">
            <button
              onClick={() => setShowUserMenu(!showUserMenu)}
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
                  <button className="w-full flex items-center gap-2 px-4 py-2 text-sm text-secondary-700 dark:text-secondary-300 hover:bg-secondary-50 dark:hover:bg-secondary-700 transition-colors">
                    <User className="w-4 h-4" />
                    Profile
                  </button>
                  <button className="w-full flex items-center gap-2 px-4 py-2 text-sm text-secondary-700 dark:text-secondary-300 hover:bg-secondary-50 dark:hover:bg-secondary-700 transition-colors">
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
