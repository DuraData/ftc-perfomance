import { useEffect, useMemo, useState } from 'react';
import { Plus, Download, Eye, Edit2, Trash2, Copy, Library } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { useApp } from '../../context/AppContext';
import {
  createOpmsTarget as createOpmsTargetApi,
  deleteOpmsTarget as deleteOpmsTargetApi,
  getOpmsTargets as getOpmsTargetsApi,
} from '../../api/api';
import { mockDepartments, mockPeriods } from '../../data/mockData';
import type { OPMSTarget, OpmsTargetTemplate, SaveOpmsTargetPayload } from '../../types';
import { OpmsTemplateSelectionModal } from '../library/TargetLibraries';

function buildPayloadFromTarget(target: OPMSTarget): SaveOpmsTargetPayload {
  return {
    indicatorNumber: target.indicatorNumber,
    targetName: target.targetName,
    kpiDescription: target.kpiDescription,
    departmentId: target.department?.id ? Number(target.department.id) : null,
    unitId: target.unit?.id ? Number(target.unit.id) : null,
    assignedUserId: target.assignedTo?.id ?? null,
    kpiId: undefined,
    sourceTemplateId: target.sourceTemplateId ?? null,
    sourceTemplateVersion: target.sourceTemplateVersion ?? null,
    baseline: target.baseline,
    annualTarget: target.annualTarget,
    weight: target.weight,
    isArchived: target.isWithdrawn,
  };
}

function OPMSTargetFilters({ onFilterChange }: { onFilterChange: (filters: Record<string, string>) => void }) {
  const [filters, setFilters] = useState({
    department: '',
    period: '',
    status: '',
    kpa: '',
  });

  const handleChange = (key: string, value: string) => {
    const newFilters = { ...filters, [key]: value };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  return (
    <Card className="mb-4">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div>
          <label className="block text-xs font-medium text-secondary-600 dark:text-secondary-400 mb-1">
            Department
          </label>
          <select
            value={filters.department}
            onChange={(e) => handleChange('department', e.target.value)}
            className="w-full px-3 py-2 text-sm border border-secondary-200 dark:border-secondary-700 rounded-lg bg-white dark:bg-secondary-800 focus:ring-2 focus:ring-primary-500"
          >
            <option value="">All Departments</option>
            {mockDepartments.map(d => (
              <option key={d.id} value={d.id}>{d.name}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-secondary-600 dark:text-secondary-400 mb-1">
            Period
          </label>
          <select
            value={filters.period}
            onChange={(e) => handleChange('period', e.target.value)}
            className="w-full px-3 py-2 text-sm border border-secondary-200 dark:border-secondary-700 rounded-lg bg-white dark:bg-secondary-800 focus:ring-2 focus:ring-primary-500"
          >
            <option value="">All Periods</option>
            {mockPeriods.map(p => (
              <option key={p.id} value={p.id}>{p.name}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-secondary-600 dark:text-secondary-400 mb-1">
            KPA
          </label>
          <select
            value={filters.kpa}
            onChange={(e) => handleChange('kpa', e.target.value)}
            className="w-full px-3 py-2 text-sm border border-secondary-200 dark:border-secondary-700 rounded-lg bg-white dark:bg-secondary-800 focus:ring-2 focus:ring-primary-500"
          >
            <option value="">All KPAs</option>
            <option value="bsd">Basic Service Delivery</option>
            <option value="gg">Good Governance</option>
            <option value="led">Local Economic Development</option>
            <option value="fv">Financial Viability</option>
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-secondary-600 dark:text-secondary-400 mb-1">
            Status
          </label>
          <select
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
  const [opmsTargets, setOpmsTargets] = useState<OPMSTarget[]>([]);
  const [showLibraryModal, setShowLibraryModal] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState<Record<string, string>>({});

  const loadTargets = async () => {
    setIsLoading(true);
    const result = await getOpmsTargetsApi();
    if (result.success && result.data) {
      setOpmsTargets(result.data);
    } else {
      pushToast('error', result.message ?? 'Failed to load OPMS targets');
    }
    setIsLoading(false);
  };

  useEffect(() => {
    void loadTargets();
  }, []);

  const handleRowClick = (row: OPMSTarget) => {
    setCurrentPath(`/opms/targets/${row.id}`);
  };

  const openCreateFromTemplate = (template: OpmsTargetTemplate) => {
    localStorage.setItem('pending_opms_template_id', template.id);
    setCurrentPath('/opms/targets/new');
  };

  const createMultipleFromTemplates = async (templates: OpmsTargetTemplate[]) => {
    const results = await Promise.all(
      templates.map(template =>
        createOpmsTargetApi({
          indicatorNumber: template.indicatorNumber,
          targetName: template.targetName,
          kpiDescription: template.kpiDescription,
          departmentId: template.department?.id ? Number(template.department.id) : null,
          unitId: null,
          assignedUserId: null,
          kpiId: null,
          sourceTemplateId: template.id,
          sourceTemplateVersion: template.version,
          baseline: template.baseline,
          annualTarget: template.annualTarget,
          weight: template.weight,
          isArchived: false,
        }),
      ),
    );
    const createdCount = results.filter(result => result.success).length;
    if (createdCount > 0) {
      pushToast('success', `${createdCount} OPMS target${createdCount === 1 ? '' : 's'} created from library`);
      await loadTargets();
    }
  };

  const filteredTargets = useMemo(
    () =>
      opmsTargets.filter(target => {
        if (filters.department && target.department.id !== filters.department) return false;
        if (filters.period && target.period.id !== filters.period) return false;
        if (
          filters.kpa &&
          !target.nationalKPA.toLowerCase().includes(filters.kpa.toLowerCase()) &&
          !target.municipalKPA.toLowerCase().includes(filters.kpa.toLowerCase())
        ) {
          return false;
        }
        if (filters.status) {
          const status = target.isWithdrawn ? 'withdrawn' : target.isRevised ? 'revised' : 'active';
          if (status !== filters.status) return false;
        }
        return true;
      }),
    [filters, opmsTargets],
  );

  const columns = [
    {
      id: 'indicator',
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
          void (async () => {
            const result = await deleteOpmsTargetApi(row.id);
            if (result.success) {
              pushToast('success', 'OPMS target deleted');
              await loadTargets();
            } else {
              pushToast('error', result.message ?? 'Failed to delete OPMS target');
            }
          })();
        }}
        className="p-1.5 rounded-lg hover:bg-error-50 dark:hover:bg-error-900/20 transition-colors"
        title="Delete"
      >
        <Trash2 className="w-4 h-4 text-error-500" />
      </button>
    </div>
  );

  return (
    <AppShell title="OPMS Targets" subtitle="Organizational Performance Management System targets">
      <div className="space-y-6">
        {/* Header actions */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Badge variant="primary">{filteredTargets.length} targets</Badge>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" icon={<Download className="w-4 h-4" />}>
              Export
            </Button>
            <Button variant="outline" icon={<Library className="w-4 h-4" />} onClick={() => setShowLibraryModal(true)}>
              Create From OPMS Library
            </Button>
            <Button variant="primary" icon={<Plus className="w-4 h-4" />} onClick={() => setCurrentPath('/opms/targets/new')}>
              New Target
            </Button>
          </div>
        </div>

        {/* Filters */}
        <OPMSTargetFilters onFilterChange={setFilters} />

        {/* Data Table */}
        <DataTable
          data={filteredTargets}
          columns={columns}
          onRowClick={handleRowClick}
          actions={actions}
          searchPlaceholder="Search targets..."
          emptyMessage={isLoading ? 'Loading OPMS targets...' : 'No OPMS targets found'}
          getRowId={(row) => row.id}
        />

        <OpmsTemplateSelectionModal
          isOpen={showLibraryModal}
          onClose={() => setShowLibraryModal(false)}
          onSelect={openCreateFromTemplate}
          onCreateMultiple={(templates) => { void createMultipleFromTemplates(templates); }}
        />
      </div>
    </AppShell>
  );
}
