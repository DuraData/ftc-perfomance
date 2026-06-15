import React, { useState, useRef, useEffect, useCallback } from 'react';
import { ChevronDown, ChevronLeft, ChevronRight, MoreHorizontal } from 'lucide-react';

interface Tab {
  id: string;
  label: string;
  icon?: React.ReactNode;
  badge?: number | string;
  disabled?: boolean;
}

interface TabsProps {
  tabs: Tab[];
  activeTab: string;
  onChange: (tabId: string) => void;
  variant?: 'default' | 'pills' | 'underline' | 'compact';
  size?: 'sm' | 'md' | 'lg';
}

export function Tabs({ tabs, activeTab, onChange, variant = 'default', size = 'md' }: TabsProps) {
  const sizeStyles = {
    sm: 'text-xs',
    md: 'text-sm',
    lg: 'text-base',
  };

  if (variant === 'pills') {
    return (
      <div className="flex flex-wrap gap-1.5">
        {tabs.map(tab => (
          <button
            key={tab.id}
            onClick={() => !tab.disabled && onChange(tab.id)}
            disabled={tab.disabled}
            className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full font-medium transition-all ${
              activeTab === tab.id
                ? 'bg-primary-600 text-white shadow-sm'
                : 'bg-secondary-100 text-secondary-600 hover:bg-secondary-200 dark:bg-secondary-800 dark:text-secondary-400 dark:hover:bg-secondary-700'
            } ${tab.disabled ? 'opacity-50 cursor-not-allowed' : ''} ${sizeStyles[size]}`}
          >
            {tab.icon && <span className="w-3.5 h-3.5">{tab.icon}</span>}
            <span>{tab.label}</span>
            {tab.badge !== undefined && (
              <span
                className={`px-1.5 py-0.5 rounded-full text-[10px] font-medium ${
                  activeTab === tab.id
                    ? 'bg-white/20 text-white'
                    : 'bg-secondary-200 text-secondary-600 dark:bg-secondary-700 dark:text-secondary-400'
                }`}
              >
                {tab.badge}
              </span>
            )}
          </button>
        ))}
      </div>
    );
  }

  if (variant === 'underline') {
    return (
      <div className="border-b border-secondary-200 dark:border-secondary-700">
        <div className="flex gap-0.5">
          {tabs.map(tab => (
            <button
              key={tab.id}
              onClick={() => !tab.disabled && onChange(tab.id)}
              disabled={tab.disabled}
              className={`relative inline-flex items-center gap-1.5 px-3 py-2 font-medium transition-colors ${
                activeTab === tab.id
                  ? 'text-primary-600 dark:text-primary-400'
                  : 'text-secondary-600 hover:text-secondary-900 dark:text-secondary-400 dark:hover:text-white'
              } ${tab.disabled ? 'opacity-50 cursor-not-allowed' : ''} ${sizeStyles[size]}`}
            >
              {tab.icon && <span className="w-3.5 h-3.5">{tab.icon}</span>}
              <span>{tab.label}</span>
              {tab.badge !== undefined && (
                <span
                  className={`px-1.5 py-0.5 rounded-full text-[10px] font-medium ${
                    activeTab === tab.id
                      ? 'bg-primary-100 text-primary-700 dark:bg-primary-900 dark:text-primary-300'
                      : 'bg-secondary-100 text-secondary-600 dark:bg-secondary-800 dark:text-secondary-400'
                  }`}
                >
                  {tab.badge}
                </span>
              )}
              {activeTab === tab.id && (
                <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-primary-600 dark:bg-primary-400" />
              )}
            </button>
          ))}
        </div>
      </div>
    );
  }

  if (variant === 'compact') {
    return <CompactScrollableTabs tabs={tabs} activeTab={activeTab} onChange={onChange} />;
  }

  // Default variant
  return (
    <div className="bg-secondary-100 dark:bg-secondary-800 p-0.5 rounded-md inline-flex">
      {tabs.map(tab => (
        <button
          key={tab.id}
          onClick={() => !tab.disabled && onChange(tab.id)}
          disabled={tab.disabled}
          className={`relative inline-flex items-center gap-1.5 px-3 py-1.5 rounded text-sm font-medium transition-all ${
            activeTab === tab.id
              ? 'bg-white dark:bg-secondary-700 text-secondary-900 dark:text-white shadow-sm'
              : 'text-secondary-600 hover:text-secondary-900 dark:text-secondary-400 dark:hover:text-white'
          } ${tab.disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
        >
          {tab.icon && <span className="w-3.5 h-3.5">{tab.icon}</span>}
          <span>{tab.label}</span>
          {tab.badge !== undefined && (
            <span
              className={`px-1 py-0.5 rounded text-[10px] font-medium ${
                activeTab === tab.id
                  ? 'bg-primary-100 text-primary-700 dark:bg-primary-900 dark:text-primary-300'
                  : 'bg-secondary-200 text-secondary-600 dark:bg-secondary-700 dark:text-secondary-400'
              }`}
            >
              {tab.badge}
            </span>
          )}
        </button>
      ))}
    </div>
  );
}

// Compact scrollable tabs with navigation arrows
function CompactScrollableTabs({ tabs, activeTab, onChange }: { tabs: Tab[]; activeTab: string; onChange: (id: string) => void }) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);
  const [showMoreMenu, setShowMoreMenu] = useState(false);

  const checkScroll = useCallback(() => {
    if (containerRef.current) {
      const { scrollLeft, scrollWidth, clientWidth } = containerRef.current;
      setCanScrollLeft(scrollLeft > 0);
      setCanScrollRight(scrollLeft < scrollWidth - clientWidth - 1);
    }
  }, []);

  useEffect(() => {
    checkScroll();
    const el = containerRef.current;
    if (el) {
      el.addEventListener('scroll', checkScroll);
      window.addEventListener('resize', checkScroll);
      return () => {
        el.removeEventListener('scroll', checkScroll);
        window.removeEventListener('resize', checkScroll);
      };
    }
  }, [checkScroll]);

  // Scroll active tab into view
  useEffect(() => {
    if (containerRef.current) {
      const activeEl = containerRef.current.querySelector(`[data-tab-id="${activeTab}"]`);
      if (activeEl) {
        activeEl.scrollIntoView({ behavior: 'smooth', inline: 'nearest', block: 'nearest' });
      }
    }
  }, [activeTab]);

  const scroll = (direction: 'left' | 'right') => {
    if (containerRef.current) {
      const scrollAmount = containerRef.current.clientWidth * 0.7;
      containerRef.current.scrollBy({
        left: direction === 'left' ? -scrollAmount : scrollAmount,
        behavior: 'smooth',
      });
    }
  };

  return (
    <div className="flex items-center gap-0.5">
      {/* Left scroll button */}
      <button
        onClick={() => scroll('left')}
        className={`flex-shrink-0 p-1 rounded transition-all ${
          canScrollLeft
            ? 'bg-secondary-100 dark:bg-secondary-800 hover:bg-secondary-200 dark:hover:bg-secondary-700 text-secondary-600 dark:text-secondary-400'
            : 'opacity-0 pointer-events-none'
        }`}
        disabled={!canScrollLeft}
      >
        <ChevronLeft className="w-4 h-4" />
      </button>

      {/* Tabs container - no scrollbar */}
      <div
        ref={containerRef}
        className="flex items-center gap-0.5 overflow-x-auto scrollbar-hide"
        style={{
          scrollbarWidth: 'none',
          msOverflowStyle: 'none',
          WebkitOverflowScrolling: 'touch'
        }}
      >
        {tabs.map(tab => (
          <button
            key={tab.id}
            data-tab-id={tab.id}
            onClick={() => !tab.disabled && onChange(tab.id)}
            disabled={tab.disabled}
            className={`group relative flex-shrink-0 inline-flex items-center gap-1 px-2.5 py-1.5 rounded text-xs font-medium transition-all whitespace-nowrap ${
              activeTab === tab.id
                ? 'bg-primary-600 text-white shadow-sm'
                : 'text-secondary-600 hover:bg-secondary-100 dark:text-secondary-400 dark:hover:bg-secondary-800 hover:text-secondary-900 dark:hover:text-white'
            } ${tab.disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
          >
            {tab.icon && <span className="w-3.5 h-3.5 flex-shrink-0">{tab.icon}</span>}
            <span>{tab.label}</span>
            {tab.badge !== undefined && (
              <span
                className={`ml-0.5 px-1 py-0.5 rounded text-[10px] font-semibold ${
                  activeTab === tab.id
                    ? 'bg-white/20 text-white'
                    : 'bg-secondary-200 text-secondary-500 dark:bg-secondary-700 dark:text-secondary-400 group-hover:bg-secondary-300 dark:group-hover:bg-secondary-600'
                }`}
              >
                {tab.badge}
              </span>
            )}
          </button>
        ))}
      </div>

      {/* Right scroll button */}
      <button
        onClick={() => scroll('right')}
        className={`flex-shrink-0 p-1 rounded transition-all ${
          canScrollRight
            ? 'bg-secondary-100 dark:bg-secondary-800 hover:bg-secondary-200 dark:hover:bg-secondary-700 text-secondary-600 dark:text-secondary-400'
            : 'opacity-0 pointer-events-none'
        }`}
        disabled={!canScrollRight}
      >
        <ChevronRight className="w-4 h-4" />
      </button>
    </div>
  );
}

// Hide scrollbar CSS helper
export const hideScrollbarStyles = `
  .scrollbar-hide::-webkit-scrollbar {
    display: none;
  }
  .scrollbar-hide {
    -ms-overflow-style: none;
    scrollbar-width: none;
  }
`;

interface AccordionProps {
  items: {
    id: string;
    title: string;
    icon?: React.ReactNode;
    badge?: React.ReactNode;
    content: React.ReactNode;
    defaultOpen?: boolean;
  }[];
  className?: string;
}

export function Accordion({ items, className = '' }: AccordionProps) {
  const [openItems, setOpenItems] = useState<string[]>(() =>
    items.filter(item => item.defaultOpen).map(item => item.id)
  );

  const toggleItem = (id: string) => {
    setOpenItems(prev =>
      prev.includes(id) ? prev.filter(i => i !== id) : [...prev, id]
    );
  };

  return (
    <div className={`space-y-1 ${className}`}>
      {items.map(item => (
        <div
          key={item.id}
          className="border border-secondary-200 dark:border-secondary-700 rounded-lg overflow-hidden"
        >
          <button
            onClick={() => toggleItem(item.id)}
            className="w-full flex items-center justify-between px-3 py-2 bg-white dark:bg-secondary-900 hover:bg-secondary-50 dark:hover:bg-secondary-800 transition-colors"
          >
            <div className="flex items-center gap-2">
              {item.icon && (
                <span className="text-secondary-400 w-4 h-4">{item.icon}</span>
              )}
              <span className="text-sm font-medium text-secondary-900 dark:text-white">{item.title}</span>
              {item.badge}
            </div>
            <ChevronDown
              className={`w-4 h-4 text-secondary-400 transition-transform ${
                openItems.includes(item.id) ? 'rotate-180' : ''
              }`}
            />
          </button>
          {openItems.includes(item.id) && (
            <div className="px-3 py-2 bg-secondary-50 dark:bg-secondary-800 border-t border-secondary-200 dark:border-secondary-700">
              {item.content}
            </div>
          )}
        </div>
      ))}
    </div>
  );
}
