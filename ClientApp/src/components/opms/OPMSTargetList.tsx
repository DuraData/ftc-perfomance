import { useCallback, useEffect, useState } from 'react';
import { Plus, Eye, Edit2, Ban, Copy, Library, FileText } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { OrganizationMasterPicker } from '../common/OrganizationMasterPicker';
import { useApp } from '../../context/AppContext';
import { useHasAnyPermission } from '../security/AccessControl';
import {
  createOpmsTarget as createOpmsTargetApi,
  withdrawOpmsTarget as withdrawOpmsTargetApi,
  getOpmsTargetsPage as getOpmsTargetsPageApi,
} from '../../api/api';
import type { OPMSTarget, OpmsTargetTemplate, SaveOpmsTargetPayload } from '../../types';
import { OpmsTemplateSelectionModal } from '../library/TargetLibraries';
import { GovernedWithdrawalDialog } from '../common/GovernedWithdrawalDialog';
import { canonicalSaveRows } from '../../lib/performanceTargetContract';

function buildPayloadFromTarget(target: OPMSTarget): SaveOpmsTargetPayload {
  return {
    indicatorNumber: target.indicatorNumber,
    originalOrderNumber: target.originalOrderNumber,
    targetName: target.targetName,
    kpiDescription: target.kpiDescription,
    nationalKpa: target.nationalKPA,
    municipalKpa: target.municipalKPA,
    nationalKpaPublicId: target.nationalKpaPublicId!,
    municipalKpaPublicId: target.municipalKpaPublicId!,
    backToBasicsPillarPublicId: target.backToBasicsPillarPublicId ?? null,
    strategicGoalPublicId: target.strategicGoalPublicId ?? null,
    strategicInterventionPublicId: target.strategicInterventionPublicId ?? null,
    strategicObjectivePublicId: target.strategicObjectivePublicId ?? null,
    performanceObjectivePublicId: target.performanceObjectivePublicId!,
    performanceObjective: target.performanceObjective,
    departmentId: null,
    departmentPublicId: target.department?.publicId ?? null,
    unitId: null,
    unitPublicId: target.unit?.publicId ?? null,
    assignedUserId: target.assignedTo?.id ?? null,
    sourceTemplateId: target.sourceTemplateId ?? null,
    sourceTemplateVersion: target.sourceTemplateVersion ?? null,
    periodId: target.period?.id ? Number(target.period.id) : null,
    wardIds: target.wardIds ?? [],
    additionalAssigneeIds: target.additionalAssigneeIds ?? [],
    voteNumberIds: target.voteNumberIds ?? [],
    strategicGoalId: target.strategicGoal?.id ? Number(target.strategicGoal.id) : null,
    strategicObjectiveId: target.strategicObjective?.id ? Number(target.strategicObjective.id) : null,
    baseline: target.baseline,
    baselineDescription: target.baselineDescription ?? null,
    budgetTypePublicId: target.budgetTypePublicId ?? null,
    budgetSources: (target.budgetSources ?? []).map(item => ({ budgetSourcePublicId: item.budgetSourcePublicId, amount: item.amount ?? null })),
    kpiUnitOfMeasurePublicId: target.kpiUnitOfMeasurePublicId ?? target.unitOfMeasure.id,
    weight: target.weight,
    kpiType: target.kpiType,
    kpiTypePublicId: target.kpiTypePublicId!,
    indicatorType: target.indicatorType,
    indicatorTypePublicId: target.indicatorTypePublicId!,
    functionalArea: target.functionalArea ?? null,
    functionalAreaPublicId: target.functionalAreaPublicId ?? null,
    standardClassification: target.standardClassification ?? null,
    standardClassificationPublicId: target.standardClassificationPublicId ?? null,
    idpReference: target.idpReference ?? null,
    internalReference: target.internalReference ?? null,
    fmsLink: target.fmsLink ?? null,
    isRevised: target.isRevised,
    periodTargets: canonicalSaveRows(target.periodTargets),
  };
}

export function OPMSTargetFilters({ onFilterChange }: { onFilterChange: (filters: Record<string, string>) => void }) {
  const [filters, setFilters] = useState({
    department: '',
    status: '',
  });

  const handleChange = (key: string, value: string) => {
    const newFilters = { ...filters, [key]: value };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  return (
    <Card className="mb-4">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <OrganizationMasterPicker kind="department" label="Department" value={filters.department} emptyLabel="All Departments" onChange={value => handleChange('department', value)} />
        <div>
          <label htmlFor="opms-status-filter" className="block text-xs font-medium text-secondary-600 dark:text-secondary-400 mb-1">
            Status
          </label>
          <select
            id="opms-status-filter"
            value={filters.status}
            onChange={(e) => handleChange('status', e.target.value)}
            className="w-full px-3 py-2 text-sm border border-secondary-200 dark:border-secondary-700 rounded-lg bg-white dark:bg-secondary-800 focus:ring-2 focus:ring-primary-500"
          >
            <option value="">All Statuses</option>
            <option value="active">Active</option>
            <option value="revised">Revised</option>
            <option value="withdrawn">Withdrawn</option>
          </select>
        </div>
      </div>
    </Card>
  );
}

export function OPMSTargetList() {
  const {
    setCurrentPath,
    pushToast,
  } = useApp();
  const canManageTargets = useHasAnyPermission(['OPMS_KPI.CREATE', 'OPMS_KPI.UPDATE', 'OPMS_KPI.WITHDRAW']);
  const [opmsTargets, setOpmsTargets] = useState<OPMSTarget[]>([]);
  const [showLibraryModal, setShowLibraryModal] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [filters, setFilters] = useState<Record<string, string>>({});
  const [withdrawalTarget, setWithdrawalTarget] = useState<OPMSTarget | null>(null);
  const [withdrawalBusy, setWithdrawalBusy] = useState(false);

  const loadTargets = useCallback(async () => {
    setIsLoading(true);
    const lifecycle = filters.status === 'active' || filters.status === 'revised' || filters.status === 'withdrawn' ? filters.status : undefined;
    const result = await getOpmsTargetsPageApi({ page, pageSize: 25, search, sortBy, sortDirection, departmentPublicId: filters.department || undefined, lifecycle });
    if (result.success && result.data) {
      setOpmsTargets(result.data.items);
      setTotalCount(result.data.totalCount);
    } else {
      pushToast('error', result.message ?? 'Failed to load OPMS targets');
    }
    setIsLoading(false);
  }, [filters.department, filters.status, page, pushToast, search, sortBy, sortDirection]);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPage(1);
      setSearch(searchInput.trim());
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => {
    void loadTargets();
  }, [loadTargets]);

  const handleRowClick = (row: OPMSTarget) => {
    setCurrentPath(`/opms/targets/${row.id}`);
  };

  const openCreateFromTemplate = (template: OpmsTargetTemplate) => {
    localStorage.setItem('pending_opms_template_id', template.id);
    setCurrentPath('/opms/targets/new');
  };

  const createMultipleFromTemplates = (templates: OpmsTargetTemplate[]) => {
    if (templates.length !== 1) {
      pushToast('error', 'Select one template at a time so its financial year, assignments and canonical period targets can be confirmed.');
      return;
    }
    openCreateFromTemplate(templates[0]);
  };

  const columns = [
    {
      id: 'indicator',
      sortKey: 'indicatorNumber',
      header: 'Indicator',
      accessor: (row: OPMSTarget) => (
        <div>
          <p className="font-medium text-secondary-900 dark:text-white">{row.indicatorNumber}</p>
          <p className="text-xs text-secondary-500 dark:text-secondary-400">{row.targetName}</p>
        </div>
      ),
      sortable: true,
    },
    {
      id: 'department',
      header: 'Department',
      accessor: (row: OPMSTarget) => row.department.name,
      sortable: true,
    },
    {
      id: 'kpa',
      header: 'KPA',
      accessor: (row: OPMSTarget) => (
        <div>
          <p className="text-sm text-secondary-700 dark:text-secondary-300">{row.nationalKPA}</p>
          <p className="text-xs text-secondary-500 dark:text-secondary-400">{row.municipalKPA}</p>
        </div>
      ),
    },
    {
      id: 'template',
      header: 'Library Source',
      accessor: (row: OPMSTarget) => (
        row.sourceTemplateId ? (
          <div>
            <p className="text-sm text-secondary-700 dark:text-secondary-300">{row.sourceTemplateId}</p>
            <p className="text-xs text-secondary-500 dark:text-secondary-400">v{row.sourceTemplateVersion ?? 1}</p>
          </div>
        ) : (
          <span className="text-xs text-secondary-400">Manual</span>
        )
      ),
    },
    {
      id: 'target',
      header: 'Target',
      accessor: (row: OPMSTarget) => (
        <div className="text-right">
          <p className="font-medium text-secondary-900 dark:text-white">{row.annualTarget.toLocaleString()}</p>
          <p className="text-xs text-secondary-500 dark:text-secondary-400">{row.unitOfMeasure.name}</p>
        </div>
      ),
      className: 'text-right',
    },
    {
      id: 'period',
      header: 'Period',
      accessor: (row: OPMSTarget) => row.period.fiscalYear,
      sortable: true,
    },
    {
      id: 'status',
      header: 'Status',
      accessor: (row: OPMSTarget) => (
        <div className="flex items-center gap-2">
          {row.isWithdrawn ? (
            <Badge variant="error">Withdrawn</Badge>
          ) : row.isRevised ? (
            <Badge variant="warning">Revised</Badge>
          ) : (
            <Badge variant="success">Active</Badge>
          )}
        </div>
      ),
    },
  ];

  const actions = (row: OPMSTarget) => (
    <div className="flex items-center justify-end gap-1">
      <button
        onClick={(e) => {
          e.stopPropagation();
          handleRowClick(row);
        }}
        className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700 transition-colors"
        title="View"
      >
        <Eye className="w-4 h-4 text-secondary-400" />
      </button>
      {canManageTargets ? (
        <>
          <button
            onClick={(e) => {
              e.stopPropagation();
              setCurrentPath(`/opms/targets/${row.id}/edit`);
            }}
            className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700 transition-colors"
            title="Edit"
          >
            <Edit2 className="w-4 h-4 text-secondary-400" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              void (async () => {
                if (!row.nationalKpaPublicId || !row.municipalKpaPublicId || !row.performanceObjectivePublicId) {
                  pushToast('error', 'Reconcile this legacy target to governed strategic classifications before copying it.');
                  return;
                }
                const result = await createOpmsTargetApi(buildPayloadFromTarget({
                  ...row,
                  id: '',
                  indicatorNumber: `${row.indicatorNumber}-COPY`,
                  targetName: `${row.targetName} (Copy)`,
                }));
                if (result.success) {
                  pushToast('success', 'OPMS target copied');
                  await loadTargets();
                } else {
                  pushToast('error', result.message ?? 'Failed to copy OPMS target');
                }
              })();
            }}
            className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700 transition-colors"
            title="Copy"
          >
            <Copy className="w-4 h-4 text-secondary-400" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              setWithdrawalTarget(row);
            }}
            disabled={row.isWithdrawn}
            className="p-1.5 rounded-lg hover:bg-error-50 dark:hover:bg-error-900/20 transition-colors"
            title={row.isWithdrawn ? 'Already withdrawn' : 'Withdraw'}
          >
            <Ban className="w-4 h-4 text-error-500" />
          </button>
        </>
      ) : (
        <button
          onClick={(e) => {
            e.stopPropagation();
            setCurrentPath('/opms/submissions');
          }}
          className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700 transition-colors"
          title="Start Submission"
        >
          <FileText className="w-4 h-4 text-secondary-400" />
        </button>
      )}
    </div>
  );

  return (
    <AppShell title="OPMS Targets" subtitle="Organizational Performance Management System targets">
      <div className="space-y-6">
        {/* Header actions */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Badge variant="primary">
              {`${totalCount} targets`}
            </Badge>
            {!canManageTargets ? <Badge variant="warning">Read Only</Badge> : null}
          </div>
          {canManageTargets ? (
            <div className="flex items-center gap-2">
              <Button variant="outline" icon={<Library className="w-4 h-4" />} onClick={() => setShowLibraryModal(true)}>
                Create From OPMS Library
              </Button>
              <Button variant="primary" icon={<Plus className="w-4 h-4" />} onClick={() => setCurrentPath('/opms/targets/new')}>
                New Target
              </Button>
            </div>
          ) : null}
        </div>

        {/* Filters */}
        <OPMSTargetFilters onFilterChange={next => { setPage(1); setFilters(next); }} />

        {/* Data Table */}
        <DataTable
          data={opmsTargets}
          columns={columns}
          onRowClick={handleRowClick}
          actions={actions}
          searchPlaceholder="Search targets..."
          searchable
          serverState={{
            page,
            pageSize: 25,
            totalCount,
            search: searchInput,
            sortBy,
            sortDirection,
            onPageChange: setPage,
            onSearchChange: setSearchInput,
            onSortChange: (nextSort, nextDirection) => { setPage(1); setSortBy(nextSort); setSortDirection(nextDirection); },
          }}
          emptyMessage={isLoading ? 'Loading OPMS targets...' : 'No OPMS targets found'}
          getRowId={(row) => row.id}
        />

        {canManageTargets ? (
          <OpmsTemplateSelectionModal
            isOpen={showLibraryModal}
            onClose={() => setShowLibraryModal(false)}
            onSelect={openCreateFromTemplate}
            onCreateMultiple={(templates) => { void createMultipleFromTemplates(templates); }}
          />
        ) : null}
        <GovernedWithdrawalDialog
          isOpen={Boolean(withdrawalTarget)}
          recordLabel="OPMS target"
          busy={withdrawalBusy}
          onClose={() => setWithdrawalTarget(null)}
          onConfirm={(reason) => {
            if (!withdrawalTarget?.rowVersion) {
              pushToast('error', 'Refresh the target before withdrawing it.');
              return;
            }
            void (async () => {
              setWithdrawalBusy(true);
              const result = await withdrawOpmsTargetApi(withdrawalTarget.id, { reason, rowVersion: withdrawalTarget.rowVersion! });
              if (result.success && result.data) {
                setOpmsTargets(prev => prev.map(item => item.id === result.data!.id ? result.data! : item));
                setWithdrawalTarget(null);
                pushToast('success', 'OPMS target withdrawn');
              } else pushToast('error', result.message ?? 'Failed to withdraw OPMS target');
              setWithdrawalBusy(false);
            })();
          }}
        />
      </div>
    </AppShell>
  );
}
