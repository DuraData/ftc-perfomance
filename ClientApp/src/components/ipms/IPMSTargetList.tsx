import { useCallback, useEffect, useState } from 'react';
import { Plus, Download, Eye, Edit2, Link2, Ban, Library, FileText } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { useApp } from '../../context/AppContext';
import { useHasAnyPermission } from '../security/AccessControl';
import {
  createIpmsTarget as createIpmsTargetApi,
  withdrawIpmsTarget as withdrawIpmsTargetApi,
  getIpmsTargetsPage as getIpmsTargetsPageApi,
} from '../../api/api';
import type { IPMSTarget, IpmsTargetTemplate, SaveIpmsTargetPayload } from '../../types';
import { IpmsTemplateSelectionModal } from '../library/TargetLibraries';
import { GovernedWithdrawalDialog } from '../common/GovernedWithdrawalDialog';
import { canonicalSaveRows } from '../../lib/performanceTargetContract';

function buildPayloadFromTarget(target: IPMSTarget): SaveIpmsTargetPayload {
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
    relatedOpmsTargetId: target.relatedOPMSTarget?.id ?? null,
    sourceTemplateId: target.sourceTemplateId ?? null,
    sourceTemplateVersion: target.sourceTemplateVersion ?? null,
    periodId: target.period?.id ? Number(target.period.id) : null,
    strategicGoalId: target.strategicGoal?.id ? Number(target.strategicGoal.id) : null,
    strategicObjectiveId: target.strategicObjective?.id ? Number(target.strategicObjective.id) : null,
    baseline: target.baseline,
    budgetTypePublicId: target.budgetTypePublicId ?? null,
    budgetSources: (target.budgetSources ?? []).map(item => ({ budgetSourcePublicId: item.budgetSourcePublicId, amount: item.amount ?? null })),
    unitOfMeasureId: target.unitOfMeasure?.id ? Number(target.unitOfMeasure.id) : null,
    weight: target.weight,
    kpiType: target.kpiType,
    kpiTypePublicId: target.kpiTypePublicId!,
    indicatorType: target.indicatorType,
    indicatorTypePublicId: target.indicatorTypePublicId!,
    functionalArea: target.functionalArea ?? null,
    functionalAreaPublicId: target.functionalAreaPublicId ?? null,
    idpReference: target.idpReference ?? null,
    internalReference: target.internalReference ?? null,
    isRevised: target.isRevised,
    periodTargets: canonicalSaveRows(target.periodTargets),
  };
}

export function IPMSTargetList() {
  const {
    setCurrentPath,
    pushToast,
  } = useApp();
  const canManageTargets = useHasAnyPermission(['IPMS_KPI.CREATE', 'IPMS_KPI.UPDATE', 'IPMS_KPI.WITHDRAW']);
  const [ipmsTargets, setIpmsTargets] = useState<IPMSTarget[]>([]);
  const [showLibraryModal, setShowLibraryModal] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [withdrawalTarget, setWithdrawalTarget] = useState<IPMSTarget | null>(null);
  const [withdrawalBusy, setWithdrawalBusy] = useState(false);

  const loadTargets = useCallback(async () => {
    setIsLoading(true);
    const result = await getIpmsTargetsPageApi({ page, pageSize: 25, search, sortBy, sortDirection });
    if (result.success && result.data) {
      setIpmsTargets(result.data.items);
      setTotalCount(result.data.totalCount);
    } else {
      pushToast('error', result.message ?? 'Failed to load IPMS targets');
    }
    setIsLoading(false);
  }, [page, pushToast, search, sortBy, sortDirection]);

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

  const handleRowClick = (row: IPMSTarget) => {
    setCurrentPath(`/ipms/targets/${row.id}`);
  };

  const openCreateFromTemplate = (template: IpmsTargetTemplate) => {
    localStorage.setItem('pending_ipms_template_id', template.id);
    setCurrentPath('/ipms/targets/new');
  };

  const createMultipleFromTemplates = (templates: IpmsTargetTemplate[]) => {
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
      accessor: (row: IPMSTarget) => (
        <div>
          <p className="font-medium text-secondary-900 dark:text-white">{row.indicatorNumber}</p>
          <p className="text-xs text-secondary-500 dark:text-secondary-400">{row.targetName}</p>
        </div>
      ),
    },
    {
      id: 'department',
      header: 'Department',
      accessor: (row: IPMSTarget) => row.department.name,
    },
    {
      id: 'opms',
      header: 'Related OPMS',
      accessor: (row: IPMSTarget) => (
        row.relatedOPMSTarget ? (
          <Badge variant="info">{row.relatedOPMSTarget.indicatorNumber}</Badge>
        ) : (
          <span className="text-secondary-400">Not linked</span>
        )
      ),
    },
    {
      id: 'template',
      header: 'Library Source',
      accessor: (row: IPMSTarget) => (
        row.sourceTemplateId ? (
          <div>
            <p className="text-sm text-secondary-700 dark:text-secondary-300">{row.sourceTemplateId}</p>
            <p className="text-xs text-secondary-500 dark:text-secondary-400">v{row.sourceTemplateVersion ?? 1}</p>
          </div>
        ) : (
          <span className="text-secondary-400">Manual</span>
        )
      ),
    },
    {
      id: 'target',
      header: 'Annual Target',
      accessor: (row: IPMSTarget) => (
        <div className="text-right">
          <p className="font-medium">{row.annualTarget.toLocaleString()}</p>
          <p className="text-xs text-secondary-500">{row.unitOfMeasure.name}</p>
        </div>
      ),
    },
    {
      id: 'period',
      header: 'Period',
      accessor: (row: IPMSTarget) => row.period.fiscalYear,
    },
    {
      id: 'status',
      header: 'Status',
      accessor: (row: IPMSTarget) => (
        row.isRevised ? <Badge variant="warning">Revised</Badge> : <Badge variant="success">Active</Badge>
      ),
    },
  ];

  const actions = (row: IPMSTarget) => (
    <div className="flex items-center justify-end gap-1">
      <button
        onClick={(e) => {
          e.stopPropagation();
          handleRowClick(row);
        }}
        className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700"
        title="View"
      >
        <Eye className="w-4 h-4 text-secondary-400" />
      </button>
      {canManageTargets ? (
        <>
          <button
            onClick={(e) => {
              e.stopPropagation();
              setCurrentPath(`/ipms/targets/${row.id}/edit`);
            }}
            className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700"
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
                const result = await createIpmsTargetApi(buildPayloadFromTarget({
                  ...row,
                  id: '',
                  indicatorNumber: `${row.indicatorNumber}-COPY`,
                  targetName: `${row.targetName} (Copy)`,
                }));
                if (result.success) {
                  pushToast('success', 'IPMS target copied');
                  await loadTargets();
                } else {
                  pushToast('error', result.message ?? 'Failed to copy IPMS target');
                }
              })();
            }}
            className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700"
            title="Link to OPMS"
          >
            <Link2 className="w-4 h-4 text-secondary-400" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              setWithdrawalTarget(row);
            }}
            disabled={row.isWithdrawn}
            className="p-1.5 rounded-lg hover:bg-error-50 dark:hover:bg-error-900/20"
            title={row.isWithdrawn ? 'Already withdrawn' : 'Withdraw'}
          >
            <Ban className="w-4 h-4 text-error-500" />
          </button>
        </>
      ) : (
        <button
          onClick={(e) => {
            e.stopPropagation();
            setCurrentPath('/ipms/submissions');
          }}
          className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700"
          title="Start Submission"
        >
          <FileText className="w-4 h-4 text-secondary-400" />
        </button>
      )}
    </div>
  );

  return (
    <AppShell title="IPMS Targets" subtitle="Individual Performance Management System targets">
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Badge variant="primary">{totalCount} targets</Badge>
            {!canManageTargets ? <Badge variant="warning">Read Only</Badge> : null}
          </div>
          {canManageTargets ? (
            <div className="flex items-center gap-2">
              <Button variant="outline" icon={<Download className="w-4 h-4" />}>
                Export
              </Button>
              <Button variant="outline" icon={<Library className="w-4 h-4" />} onClick={() => setShowLibraryModal(true)}>
                Create From IPMS Library
              </Button>
              <Button variant="primary" icon={<Plus className="w-4 h-4" />} onClick={() => setCurrentPath('/ipms/targets/new')}>
                New IPMS Target
              </Button>
            </div>
          ) : null}
        </div>

        <Card>
          <DataTable
            data={ipmsTargets}
            columns={columns}
            onRowClick={handleRowClick}
            actions={actions}
            searchPlaceholder="Search IPMS targets..."
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
            emptyMessage={isLoading ? 'Loading IPMS targets...' : 'No IPMS targets found'}
            getRowId={(row) => row.id}
          />
        </Card>

        {canManageTargets ? (
          <IpmsTemplateSelectionModal
            isOpen={showLibraryModal}
            onClose={() => setShowLibraryModal(false)}
            onSelect={openCreateFromTemplate}
            onCreateMultiple={(templates) => { void createMultipleFromTemplates(templates); }}
          />
        ) : null}
        <GovernedWithdrawalDialog
          isOpen={Boolean(withdrawalTarget)}
          recordLabel="IPMS target"
          busy={withdrawalBusy}
          onClose={() => setWithdrawalTarget(null)}
          onConfirm={(reason) => {
            if (!withdrawalTarget?.rowVersion) {
              pushToast('error', 'Refresh the target before withdrawing it.');
              return;
            }
            void (async () => {
              setWithdrawalBusy(true);
              const result = await withdrawIpmsTargetApi(withdrawalTarget.id, { reason, rowVersion: withdrawalTarget.rowVersion! });
              if (result.success && result.data) {
                setIpmsTargets(prev => prev.map(item => item.id === result.data!.id ? result.data! : item));
                setWithdrawalTarget(null);
                pushToast('success', 'IPMS target withdrawn');
              } else pushToast('error', result.message ?? 'Failed to withdraw IPMS target');
              setWithdrawalBusy(false);
            })();
          }}
        />
      </div>
    </AppShell>
  );
}
