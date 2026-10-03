import { Database } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { EmptyState } from '../ui';

export function LookupTables() {
  return (
    <AppShell title="Lookup Tables" subtitle="Manage system lookup values">
      <EmptyState
        icon={<Database className="h-8 w-8" />}
        title="Governed lookup administration is not configured"
        description="Units of measure, budget classifications, strategic goals, and KPAs require a tenant-scoped persistence service before they can be administered here."
      />
    </AppShell>
  );
}
