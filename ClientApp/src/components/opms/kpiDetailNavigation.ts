export type KpiDetailTab = 'kpi-details' | 'strategic-alignment' | 'tid' | 'circular-88' | 'reporting' | 'poe' | 'workflow' | 'pms-review' | 'internal-audit' | 'history';

const kpiDetailTabs = new Set<KpiDetailTab>(['kpi-details', 'strategic-alignment', 'tid', 'circular-88', 'reporting', 'poe', 'workflow', 'pms-review', 'internal-audit', 'history']);

export function readKpiDetailContext(search = window.location.search): { tab: KpiDetailTab; reportingPeriodPublicId?: string } {
  const parameters = new URLSearchParams(search);
  const requestedTab = parameters.get('tab') as KpiDetailTab | null;
  return {
    tab: requestedTab && kpiDetailTabs.has(requestedTab) ? requestedTab : 'kpi-details',
    reportingPeriodPublicId: parameters.get('period') || undefined,
  };
}
