import { useCallback, useEffect, useState } from 'react';
import { Plus, Download, Eye, Edit2, Link2, Trash2, Library } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { useApp } from '../../context/AppContext';
import {
  createIpmsTarget as createIpmsTargetApi,
  deleteIpmsTarget as deleteIpmsTargetApi,
  getIpmsTargets as getIpmsTargetsApi,
} from '../../api/api';
import type { IPMSTarget, IpmsTargetTemplate, SaveIpmsTargetPayload } from '../../types';
import { IpmsTemplateSelectionModal } from '../library/TargetLibraries';

function buildPayloadFromTarget(target: IPMSTarget): SaveIpmsTargetPayload {
  return {
    indicatorNumber: target.indicatorNumber,
    targetName: target.targetName,
    kpiDescription: target.kpiDescription,
    nationalKpa: target.nationalKPA,
    municipalKpa: target.municipalKPA,
    performanceObjective: target.performanceObjective,
    departmentId: target.department?.id ? Number(target.department.id) : null,
    unitId: target.unit?.id ? Number(target.unit.id) : null,
    assignedUserId: target.assignedTo?.id ?? null,
    relatedOpmsTargetId: target.relatedOPMSTarget?.id ?? null,
    sourceTemplateId: target.sourceTemplateId ?? null,
    sourceTemplateVersion: target.sourceTemplateVersion ?? null,
    baseline: target.baseline,
    annualTarget: target.annualTarget,
    annualTargetDescription: target.annualTargetDescription,
    budgetSourceId: target.budgetSource?.id ? Number(target.budgetSource.id) : null,
    budgetTypeId: target.budgetType?.id ? Number(target.budgetType.id) : null,
    unitOfMeasureId: target.unitOfMeasure?.id ? Number(target.unitOfMeasure.id) : null,
    weight: target.weight,
    kpiType: target.kpiType,
    indicatorType: target.indicatorType,
    functionalArea: target.functionalArea ?? null,
    idpReference: target.idpReference ?? null,
    internalReference: target.internalReference ?? null,
    isRevised: target.isRevised,
    targetUnitType: target.targetUnitType,
  };
}

export function IPMSTargetList() {
  const {
    setCurrentPath,
    pushToast,
  } = useApp();
  const [ipmsTargets, setIpmsTargets] = useState<IPMSTarget[]>([]);
  const [showLibraryModal, setShowLibraryModal] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  const loadTargets = useCallback(async () => {
    setIsLoading(true);
    const result = await getIpmsTargetsApi();
    if (result.success && result.data) {
      setIpmsTargets(result.data);
    } else {
      pushToast('error', result.message ?? 'Failed to load IPMS targets');
    }
    setIsLoading(false);
  }, [pushToast]);

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

  const createMultipleFromTemplates = async (templates: IpmsTargetTemplate[]) => {
    const results = await Promise.all(
      templates.map(template =>
        createIpmsTargetApi({
          indicatorNumber: template.templateCode,
          targetName: template.targetName,
          kpiDescription: template.kpiDescription,
          nationalKpa: '',
          municipalKpa: '',
          performanceObjective: '',
          departmentId: template.department?.id ? Number(template.department.id) : null,
          unitId: null,
          assignedUserId: null,
          relatedOpmsTargetId: null,
          sourceTemplateId: template.id,
          sourceTemplateVersion: template.version,
          baseline: 0,
          annualTarget: template.annualTarget,
          annualTargetDescription: template.annualTargetDescription,
          budgetSourceId: null,
          budgetTypeId: null,
          unitOfMeasureId: template.unitOfMeasure?.id ? Number(template.unitOfMeasure.id) : null,
          weight: template.weight,
          kpiType: 'quantitative',
          indicatorType: 'output',
          functionalArea: template.functionalArea ?? null,
          idpReference: null,
          internalReference: null,
          isRevised: false,
          targetUnitType: template.targetUnitType,
        }),
      ),
    );
    const createdCount = results.filter(result => result.success).length;
    if (createdCount > 0) {
      pushToast('success', `${createdCount} IPMS target${createdCount === 1 ? '' : 's'} created from library`);
      await loadTargets();
    }
  };

  const columns = [
    {
      id: 'indicator',
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
          void (async () => {
            const result = await deleteIpmsTargetApi(row.id);
            if (result.success) {
              pushToast('success', 'IPMS target deleted');
              await loadTargets();
            } else {
              pushToast('error', result.message ?? 'Failed to delete IPMS target');
            }
          })();
        }}
        className="p-1.5 rounded-lg hover:bg-error-50 dark:hover:bg-error-900/20"
        title="Delete"
      >
        <Trash2 className="w-4 h-4 text-error-500" />
      </button>
    </div>
  );

  return (
    <AppShell title="IPMS Targets" subtitle="Individual Performance Management System targets">
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{ipmsTargets.length} targets</Badge>
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
        </div>

        <Card>
          <DataTable
            data={ipmsTargets}
            columns={columns}
            onRowClick={handleRowClick}
            actions={actions}
            searchPlaceholder="Search IPMS targets..."
          emptyMessage={isLoading ? 'Loading IPMS targets...' : 'No IPMS targets found'}
            getRowId={(row) => row.id}
          />
        </Card>

        <IpmsTemplateSelectionModal
          isOpen={showLibraryModal}
          onClose={() => setShowLibraryModal(false)}
          onSelect={openCreateFromTemplate}
          onCreateMultiple={(templates) => { void createMultipleFromTemplates(templates); }}
        />
      </div>
    </AppShell>
  );
}
