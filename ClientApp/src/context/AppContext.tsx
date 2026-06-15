import React, { createContext, useContext, useState, ReactNode } from 'react';
import type { User, UserRole } from '../types';
import { mockUsers } from '../data/mockData';

interface AppContextType {
  user: User | null;
  isAuthenticated: boolean;
  sidebarCollapsed: boolean;
  darkMode: boolean;
  login: (email: string, password: string) => boolean;
  logout: () => void;
  toggleSidebar: () => void;
  toggleDarkMode: () => void;
  switchRole: (role: UserRole) => void;
  currentPath: string;
  setCurrentPath: (path: string) => void;
}

const AppContext = createContext<AppContextType | undefined>(undefined);

export function AppProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(mockUsers[1]);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [darkMode, setDarkMode] = useState(false);
  const [currentPath, setCurrentPath] = useState('/dashboard');

  const login = (email: string, _password: string): boolean => {
    const foundUser = mockUsers.find(u => u.email.toLowerCase() === email.toLowerCase());
    if (foundUser) {
      setUser(foundUser);
      return true;
    }
    return false;
  };

  const logout = () => {
    setUser(null);
    setCurrentPath('/login');
  };

  const toggleSidebar = () => {
    setSidebarCollapsed(prev => !prev);
  };

  const toggleDarkMode = () => {
    setDarkMode(prev => !prev);
    if (!darkMode) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  };

  const switchRole = (role: UserRole) => {
    if (user) {
      setUser({ ...user, role });
    }
  };

  return (
    <AppContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        sidebarCollapsed,
        darkMode,
        login,
        logout,
        toggleSidebar,
        toggleDarkMode,
        switchRole,
        currentPath,
        setCurrentPath,
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
