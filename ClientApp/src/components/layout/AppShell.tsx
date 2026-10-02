import React from 'react';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';
import { useApp } from '../../context/AppContext';

interface AppShellProps {
  children: React.ReactNode;
  title?: string;
  subtitle?: string;
}

export function AppShell({ children, title, subtitle }: AppShellProps) {
  const { sidebarCollapsed, toasts } = useApp();

  return (
    <div className="min-h-screen bg-secondary-50 dark:bg-secondary-950">
      <a
        href="#main-content"
        className="sr-only z-[100] rounded bg-primary-700 px-4 py-2 text-white focus:not-sr-only focus:fixed focus:left-4 focus:top-4"
      >
        Skip to main content
      </a>
      <Sidebar />
      <div className="transition-[margin-left] duration-300" style={{ marginLeft: sidebarCollapsed ? 64 : 256 }}>
        <TopBar title={title} subtitle={subtitle} />
        <main id="main-content" tabIndex={-1} className="p-6 focus:outline-none">
          {children}
        </main>
      </div>

      <div className="fixed bottom-4 right-4 z-50 space-y-2" aria-label="Notifications">
        {toasts.map(t => (
          <div
            key={t.id}
            role={t.type === 'error' ? 'alert' : 'status'}
            aria-live={t.type === 'error' ? 'assertive' : 'polite'}
            className={`min-w-[280px] max-w-[420px] rounded-lg border px-3 py-2 text-sm shadow-lg ${
              t.type === 'success'
                ? 'border-success-200 bg-success-50 text-success-800 dark:border-success-800 dark:bg-success-900/20 dark:text-success-200'
                : t.type === 'error'
                ? 'border-error-200 bg-error-50 text-error-800 dark:border-error-800 dark:bg-error-900/20 dark:text-error-200'
                : 'border-secondary-200 bg-white text-secondary-800 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200'
            }`}
          >
            {t.message}
          </div>
        ))}
      </div>
    </div>
  );
}
