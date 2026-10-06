import { ArrowRight, Database } from 'lucide-react';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import { AppShell } from '../layout/AppShell';
import { Button, Card } from '../ui';

const registers = [
  ['National KPAs', '/admin/kpas', 'NATIONAL_KPA'],
  ['Municipal KPAs', '/admin/municipal-kpas', 'MUNICIPAL_KPA'],
  ['Back-to-Basics Pillars', '/admin/back-to-basics-pillars', 'BACK_TO_BASICS_PILLAR'],
  ['Strategic Goals', '/admin/strategic-goals', 'STRATEGIC_GOAL'],
  ['Strategic Interventions', '/admin/strategic-interventions', 'STRATEGIC_INTERVENTION'],
  ['Strategic Objectives', '/admin/strategic-objectives', 'STRATEGIC_OBJECTIVE'],
  ['Performance Objectives', '/admin/performance-objectives', 'PERFORMANCE_OBJECTIVE'],
  ['Budget Types', '/admin/budget-types', 'BUDGET_TYPE'],
  ['Budget Sources', '/admin/budget-sources', 'BUDGET_SOURCE'],
  ['KPI Types', '/admin/kpi-types', 'KPI_TYPE'],
  ['Indicator Types', '/admin/indicator-types', 'INDICATOR_TYPE'],
  ['Functional Areas', '/admin/functional-areas', 'FUNCTIONAL_AREA'],
  ['Standard Classifications', '/admin/standard-classifications', 'STANDARD_CLASSIFICATION'],
  ['KPI Units of Measure', '/admin/units-measure', 'KPI_UNIT_OF_MEASURE'],
] as const;

export function LookupTables() {
  const { setCurrentPath } = useApp();
  const security = useSecurity();
  const available = registers.filter(([, , resource]) => security.canRead(resource));
  return (
    <AppShell title="Governed Configuration" subtitle="Maintain municipality-scoped, effective-dated planning masters">
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {available.map(([label, route]) => <Card key={route} className="flex items-center justify-between gap-3 p-4">
          <div className="flex items-center gap-3"><Database className="h-5 w-5 text-primary-600" /><span className="font-medium">{label}</span></div>
          <Button size="sm" variant="outline" icon={<ArrowRight className="h-4 w-4" />} onClick={() => setCurrentPath(route)}>Open</Button>
        </Card>)}
        {!available.length && <Card className="p-4 text-sm text-secondary-500">No governed configuration registers are available to your current role.</Card>}
      </div>
    </AppShell>
  );
}
