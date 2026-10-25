import React, { createContext, useCallback, useContext, useEffect, useLayoutEffect, useRef, useState } from 'react';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';
import { useApp } from '../../context/AppContext';

interface AppShellProps {
  children: React.ReactNode;
  title?: string;
  subtitle?: string;
}

interface PageChrome {
  title?: string;
  subtitle?: string;
}

const PageChromeContext = createContext<((chrome: PageChrome) => void) | null>(null);

function AppShellFrame({ children, title, subtitle }: AppShellProps) {

  const { sidebarCollapsed, toasts } = useApp();
  const [mobileNavigationOpen, setMobileNavigationOpen] = useState(false);
  const mobileNavigationRef = useRef<HTMLDivElement>(null);
  const mobileNavigationTriggerRef = useRef<HTMLButtonElement>(null);

  const closeMobileNavigation = useCallback(() => setMobileNavigationOpen(false), []);

  useEffect(() => {
    if (!mobileNavigationOpen) return undefined;

    const trigger = mobileNavigationTriggerRef.current;
    const focusableSelector = [
      'a[href]',
      'button:not([disabled]):not([tabindex="-1"])',
      'input:not([disabled]):not([tabindex="-1"])',
      'select:not([disabled]):not([tabindex="-1"])',
      'textarea:not([disabled]):not([tabindex="-1"])',
      '[tabindex]:not([tabindex="-1"])',
    ].join(',');
    const focusFirstControl = () => {
      const focusable = mobileNavigationRef.current?.querySelector<HTMLElement>(focusableSelector);
      (focusable ?? mobileNavigationRef.current)?.focus();
    };
    const animationFrame = window.requestAnimationFrame(focusFirstControl);
    const previousBodyOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        closeMobileNavigation();
        return;
      }

      if (event.key !== 'Tab') return;
      const panel = mobileNavigationRef.current;
      if (!panel) return;
      const focusable = Array.from(panel.querySelectorAll<HTMLElement>(focusableSelector));
      if (focusable.length === 0) {
        event.preventDefault();
        panel.focus();
        return;
      }

      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && (document.activeElement === first || !panel.contains(document.activeElement))) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && (document.activeElement === last || !panel.contains(document.activeElement))) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => {
      window.cancelAnimationFrame(animationFrame);
      document.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = previousBodyOverflow;
      if (trigger?.isConnected) trigger.focus();
    };
  }, [closeMobileNavigation, mobileNavigationOpen]);

  useEffect(() => {
    document.title = title ? `${title} | OPMS` : 'OPMS | Municipal Performance Management';
  }, [title]);

  return (
    <div className="min-h-screen bg-secondary-50 dark:bg-secondary-950">
      <a
        href="#main-content"
        aria-hidden={mobileNavigationOpen ? 'true' : undefined}
        tabIndex={mobileNavigationOpen ? -1 : undefined}
        className="sr-only z-[100] rounded bg-primary-700 px-4 py-2 text-white focus:not-sr-only focus:fixed focus:left-4 focus:top-4"
      >
        Skip to main content
      </a>
      {mobileNavigationOpen && (
        <button type="button" aria-hidden="true" tabIndex={-1} className="fixed inset-0 z-20 bg-secondary-950/50 md:hidden" onClick={closeMobileNavigation} />
      )}
      <Sidebar mobileOpen={mobileNavigationOpen} onMobileClose={closeMobileNavigation} mobileNavigationRef={mobileNavigationRef} />
      <div aria-hidden={mobileNavigationOpen ? 'true' : undefined} className={`transition-[margin-left] duration-300 ${sidebarCollapsed ? 'md:ml-16' : 'md:ml-64'}`}>
        <TopBar
          title={title}
          subtitle={subtitle}
          navigationOpen={mobileNavigationOpen}
          navigationTriggerRef={mobileNavigationTriggerRef}
          onOpenNavigation={() => setMobileNavigationOpen(true)}
        />
        <main
          id="main-content"
          tabIndex={-1}
          aria-labelledby={title ? 'page-title' : undefined}
          aria-label={title ? undefined : 'Application workspace'}
          className="p-4 focus:outline-none sm:p-6"
        >
          {children}
        </main>
      </div>

      <div className="fixed bottom-4 right-4 z-50 space-y-2" role="region" aria-label="Notifications" aria-hidden={mobileNavigationOpen ? 'true' : undefined}>
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

/**
 * Owns the application chrome for the lifetime of an authenticated session.
 * Route components still declare their title through AppShell, but only their
 * work-area content is replaced during navigation.
 */
export function WorkspaceShell({ children }: { children: React.ReactNode }) {
  const [chrome, setChrome] = useState<PageChrome>({});
  const updateChrome = useCallback((next: PageChrome) => {
    setChrome(current => current.title === next.title && current.subtitle === next.subtitle ? current : next);
  }, []);

  return (
    <PageChromeContext.Provider value={updateChrome}>
      <AppShellFrame title={chrome.title} subtitle={chrome.subtitle}>{children}</AppShellFrame>
    </PageChromeContext.Provider>
  );
}

export function AppShell({ children, title, subtitle }: AppShellProps) {
  const updateChrome = useContext(PageChromeContext);

  useLayoutEffect(() => {
    updateChrome?.({ title, subtitle });
  }, [subtitle, title, updateChrome]);

  if (updateChrome) return <>{children}</>;
  return <AppShellFrame title={title} subtitle={subtitle}>{children}</AppShellFrame>;
}
