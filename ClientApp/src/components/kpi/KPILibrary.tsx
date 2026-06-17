import { Library } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { EmptyState } from '../ui';
import { useApp } from '../../context/AppContext';

export function KPILibrary() {
  const { setCurrentPath } = useApp();

  return (
    <AppShell title="KPI Library" subtitle="Replaced by dedicated OPMS and IPMS target libraries">
      <EmptyState
        icon={<Library className="h-6 w-6" />}
        title="KPI Library has been split"
        description="Use the OPMS Target Library for reusable OPMS templates and the IPMS Target Library for reusable IPMS templates."
        action={
          <div className="flex gap-2">
            <button className="rounded bg-primary-600 px-3 py-1.5 text-xs font-medium text-white" onClick={() => setCurrentPath('/opms/library')}>
              Open OPMS Target Library
            </button>
            <button className="rounded border border-secondary-300 px-3 py-1.5 text-xs font-medium text-secondary-700" onClick={() => setCurrentPath('/ipms/library')}>
              Open IPMS Target Library
            </button>
          </div>
        }
      />
    </AppShell>
  );
}
