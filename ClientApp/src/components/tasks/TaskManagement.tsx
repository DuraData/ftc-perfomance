import { ClipboardList } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { EmptyState } from '../ui';

export function TaskManagement() {
  return (
    <AppShell title="Tasks" subtitle="Task management">
      <EmptyState
        icon={<ClipboardList className="h-8 w-8" />}
        title="Task management is not enabled"
        description="No governed task service is configured for this municipality. Use the authorised workflow queues for OPMS and IPMS work items."
      />
    </AppShell>
  );
}
