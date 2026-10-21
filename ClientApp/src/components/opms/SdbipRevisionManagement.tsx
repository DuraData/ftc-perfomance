import { useCallback, useEffect, useState } from 'react';
import { History, ListFilter } from 'lucide-react';
import { getOpmsTargetsPage } from '../../api/api';
import { useApp } from '../../context/AppContext';
import type { OPMSTarget } from '../../types';
import { DataTable } from '../common/DataTable';
import { AppShell } from '../layout/AppShell';
import { KpiDefinitionRevisionEditor } from '../targets/KpiDefinitionRevisionEditor';
import { KpiOrderingEditor } from '../targets/KpiOrderingEditor';
import { PerformancePeriodTargetEditor } from '../targets/PerformancePeriodTargetEditor';
import { Badge, Button, Card } from '../ui';

type RevisionArea = 'definition' | 'ordering' | 'periods';

const revisionAreas: Array<{ id: RevisionArea; label: string }> = [
  { id: 'definition', label: 'KPI fields' },
  { id: 'ordering', label: 'Period ordering' },
  { id: 'periods', label: 'Period targets' },
];

export function SdbipRevisionManagement() {
  const { currentFinancialYearPublicId, pushToast } = useApp();
  const [targets, setTargets] = useState<OPMSTarget[]>([]);
  const [selectedTarget, setSelectedTarget] = useState<OPMSTarget | null>(null);
  const [activeArea, setActiveArea] = useState<RevisionArea>('definition');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('indicatorNumber');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPage(1);
      setSearch(searchInput.trim());
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  const loadTargets = useCallback(async () => {
    if (!currentFinancialYearPublicId) {
      setTargets([]);
      setTotalCount(0);
      setSelectedTarget(null);
      setLoading(false);
      return;
    }

    setLoading(true);
    const result = await getOpmsTargetsPage({
      page,
      pageSize: 10,
      search,
      sortBy,
      sortDirection,
      municipalityFinancialYearPublicId: currentFinancialYearPublicId,
    });
    if (!result.success || !result.data) {
      setTargets([]);
      setTotalCount(0);
      pushToast('error', result.message ?? 'SDBIP targets could not be loaded for revision management.');
      setLoading(false);
      return;
    }

    setTargets(result.data.items);
    setTotalCount(result.data.totalCount);
    setSelectedTarget(current => current
      ? result.data!.items.find(item => item.id === current.id) ?? current
      : null);
    setLoading(false);
  }, [currentFinancialYearPublicId, page, pushToast, search, sortBy, sortDirection]);

  useEffect(() => { void loadTargets(); }, [loadTargets]);

  const updateSelectedTarget = (updated: OPMSTarget) => {
    setSelectedTarget(updated);
    setTargets(current => current.map(item => item.id === updated.id ? updated : item));
  };

  const columns = [
    {
      id: 'indicator',
      sortKey: 'indicatorNumber',
      header: 'KPI',
      sortable: true,
      accessor: (target: OPMSTarget) => <div>
        <p className="font-medium text-secondary-900 dark:text-white">{target.indicatorNumber}</p>
        <p className="text-xs text-secondary-500 dark:text-secondary-400">{target.targetName}</p>
      </div>,
    },
    {
      id: 'layer',
      header: 'SDBIP layer',
      accessor: (target: OPMSTarget) => target.sdbipLayer?.name ?? 'Unassigned',
    },
    {
      id: 'department',
      sortKey: 'department',
      header: 'Department',
      sortable: true,
      accessor: (target: OPMSTarget) => target.department?.name ?? 'Unassigned',
    },
    {
      id: 'order',
      header: 'Original / revised order',
      accessor: (target: OPMSTarget) => `${target.originalOrderNumber} / ${target.revisedOrderNumber}`,
    },
    {
      id: 'state',
      header: 'Revision state',
      accessor: (target: OPMSTarget) => target.isWithdrawn
        ? <Badge variant="error">Withdrawn</Badge>
        : target.isRevised
          ? <Badge variant="warning">Revised</Badge>
          : <Badge variant="success">Original</Badge>,
    },
  ];

  return <AppShell title="SDBIP Revision Management" subtitle="Record externally approved SDBIP changes with immutable field, ordering and period history">
    <div className="space-y-6">
      <Card className="p-4">
        <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <div className="flex items-center gap-2">
              <History className="h-5 w-5 text-primary-600" aria-hidden="true" />
              <h2 className="font-semibold text-secondary-900 dark:text-white">Approved source-document revisions</h2>
            </div>
            <p className="mt-1 text-sm text-secondary-600 dark:text-secondary-300">
              OPMS records an approved municipal SDBIP revision; it does not introduce another approval workflow.
              Q1, Q2 and Mid-Term retain original values and order, while Q3, Q4 and Annual resolve independently revised fields and sequence.
            </p>
          </div>
          <Badge variant="primary">{totalCount} scoped KPIs</Badge>
        </div>
      </Card>

      <DataTable
        tableLabel="SDBIP KPIs available for revision"
        data={targets}
        columns={columns}
        onRowClick={target => { setSelectedTarget(target); setActiveArea('definition'); }}
        getRowActionLabel={target => `Manage approved revisions for ${target.indicatorNumber}: ${target.targetName}`}
        actions={target => <Button size="sm" variant={selectedTarget?.id === target.id ? 'primary' : 'outline'} onClick={() => { setSelectedTarget(target); setActiveArea('definition'); }}>
          {selectedTarget?.id === target.id ? 'Selected' : 'Manage'}
        </Button>}
        searchable
        searchPlaceholder="Search KPI number, target or department..."
        serverState={{
          page,
          pageSize: 10,
          totalCount,
          search: searchInput,
          sortBy,
          sortDirection,
          onPageChange: setPage,
          onSearchChange: setSearchInput,
          onSortChange: (nextSort, nextDirection) => { setPage(1); setSortBy(nextSort); setSortDirection(nextDirection); },
        }}
        emptyMessage={loading ? 'Loading SDBIP targets...' : 'No SDBIP targets are available for the active Financial Year.'}
        getRowId={target => target.id}
      />

      {!selectedTarget ? <Card className="p-8 text-center">
        <ListFilter className="mx-auto h-8 w-8 text-secondary-400" aria-hidden="true" />
        <h2 className="mt-3 font-semibold text-secondary-900 dark:text-white">Select a KPI to manage its revisions</h2>
        <p className="mt-1 text-sm text-secondary-500">The governed editors and complete immutable histories will appear here.</p>
      </Card> : <section aria-labelledby="selected-revision-kpi" className="space-y-4">
        <Card className="p-4">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-wide text-primary-600">Selected KPI</p>
              <h2 id="selected-revision-kpi" className="text-lg font-semibold text-secondary-900 dark:text-white">{selectedTarget.indicatorNumber} — {selectedTarget.targetName}</h2>
              <p className="text-sm text-secondary-500">{selectedTarget.department?.name ?? 'Unassigned department'} · {selectedTarget.sdbipLayer?.name ?? 'Unassigned SDBIP layer'}</p>
            </div>
            <div role="tablist" aria-label="SDBIP revision areas" className="flex flex-wrap gap-2">
              {revisionAreas.map(area => <Button
                key={area.id}
                role="tab"
                aria-selected={activeArea === area.id}
                aria-controls={`revision-panel-${area.id}`}
                id={`revision-tab-${area.id}`}
                size="sm"
                variant={activeArea === area.id ? 'primary' : 'outline'}
                onClick={() => setActiveArea(area.id)}
              >{area.label}</Button>)}
            </div>
          </div>
        </Card>

        <div role="tabpanel" id={`revision-panel-${activeArea}`} aria-labelledby={`revision-tab-${activeArea}`}>
          {activeArea === 'definition' ? <KpiDefinitionRevisionEditor
            key={`definition-${selectedTarget.id}`}
            kind="opms"
            target={selectedTarget}
            onUpdated={target => updateSelectedTarget(target as OPMSTarget)}
          /> : null}
          {activeArea === 'ordering' ? <KpiOrderingEditor
            key={`ordering-${selectedTarget.id}`}
            kind="opms"
            targetId={selectedTarget.id}
            originalOrderNumber={selectedTarget.originalOrderNumber}
            revisedOrderNumber={selectedTarget.revisedOrderNumber}
            rowVersion={selectedTarget.rowVersion ?? ''}
            onUpdated={value => updateSelectedTarget({ ...selectedTarget, ...value })}
          /> : null}
          {activeArea === 'periods' ? <PerformancePeriodTargetEditor key={`periods-${selectedTarget.id}`} kind={1} targetPublicId={selectedTarget.id} /> : null}
        </div>
      </section>}
    </div>
  </AppShell>;
}
