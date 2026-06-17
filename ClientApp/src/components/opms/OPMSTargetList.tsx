import { useEffect, useMemo, useState } from 'react';
import { Plus, Download, Eye, Edit2, Trash2, Copy, Building2, CalendarRange, BarChart3, FileText, Target, Library } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Input, Select, Textarea, FormRow, FormPanel } from '../common/Form';
import { useApp } from '../../context/AppContext';
import {
  createOpmsTarget as createOpmsTargetApi,
  deleteOpmsTarget as deleteOpmsTargetApi,
  getOpmsTargetTemplate as getOpmsTargetTemplateApi,
  getOpmsTargets as getOpmsTargetsApi,
  updateOpmsTarget as updateOpmsTargetApi,
} from '../../api/api';
import { mockDepartments, mockPeriods, mockUnitsOfMeasure, mockEmployees, mockDepartmentUnits, mockWards } from '../../data/mockData';
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
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showLibraryModal, setShowLibraryModal] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState<Record<string, string>>({});
  const [editingTarget, setEditingTarget] = useState<OPMSTarget | null>(null);
  const [form, setForm] = useState({
    sourceTemplateId: '',
    sourceTemplateVersion: '',
    periodId: '',
    departmentId: '',
    unitId: '',
    assignedToId: '',
    wardIds: '',
    indicatorNumber: '',
    targetName: '',
    annualTarget: '0',
    baseline: '0',
    weight: '0',
    unitOfMeasureId: '',
    kpiDescription: '',
  });

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

  const openCreateModal = () => {
    setEditingTarget(null);
    setForm({
      sourceTemplateId: '',
      sourceTemplateVersion: '',
      periodId: mockPeriods[0]?.id ?? '',
      departmentId: mockDepartments[0]?.id ?? '',
      unitId: '',
      assignedToId: '',
      wardIds: '',
      indicatorNumber: '',
      targetName: '',
      annualTarget: '0',
      baseline: '0',
      weight: '0',
      unitOfMeasureId: mockUnitsOfMeasure[0]?.id ?? '',
      kpiDescription: '',
    });
    setShowCreateModal(true);
  };

  const openCreateFromTemplate = (template: OpmsTargetTemplate) => {
    setEditingTarget(null);
    setForm({
      sourceTemplateId: template.id,
      sourceTemplateVersion: String(template.version),
      periodId: mockPeriods[0]?.id ?? '',
      departmentId: template.department?.id ?? mockDepartments[0]?.id ?? '',
      unitId: '',
      assignedToId: '',
      wardIds: '',
      indicatorNumber: template.indicatorNumber,
      targetName: template.targetName,
      annualTarget: String(template.annualTarget),
      baseline: String(template.baseline),
      weight: String(template.weight),
      unitOfMeasureId: template.unitOfMeasure.id,
      kpiDescription: template.kpiDescription,
    });
    setShowCreateModal(true);
  };

  const openEditModal = (target: OPMSTarget) => {
    setEditingTarget(target);
    setForm({
      sourceTemplateId: target.sourceTemplateId ?? '',
      sourceTemplateVersion: target.sourceTemplateVersion ? String(target.sourceTemplateVersion) : '',
      periodId: target.period.id,
      departmentId: target.department.id,
      unitId: target.unit?.id ?? '',
      assignedToId: target.assignedTo?.id ?? '',
      wardIds: target.wards?.map(ward => ward.id).join(',') ?? '',
      indicatorNumber: target.indicatorNumber,
      targetName: target.targetName,
      annualTarget: String(target.annualTarget),
      baseline: String(target.baseline),
      weight: String(target.weight),
      unitOfMeasureId: target.unitOfMeasure.id,
      kpiDescription: target.kpiDescription,
    });
    setShowCreateModal(true);
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

  useEffect(() => {
    const pendingTemplateId = localStorage.getItem('pending_opms_template_id');
    if (!pendingTemplateId) return;
    localStorage.removeItem('pending_opms_template_id');
    const loadTemplate = async () => {
      const result = await getOpmsTargetTemplateApi(pendingTemplateId);
      if (result.success && result.data) {
        openCreateFromTemplate(result.data);
        return;
      }
      setShowLibraryModal(true);
    };
    void loadTemplate();
  }, []);

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

  const handleSaveTarget = async () => {
    const template = editingTarget ?? opmsTargets[0];
    const department = mockDepartments.find(item => item.id === form.departmentId) ?? template?.department ?? mockDepartments[0];
    const period = mockPeriods.find(item => item.id === form.periodId) ?? template?.period ?? mockPeriods[0];
    const unitOfMeasure = mockUnitsOfMeasure.find(item => item.id === form.unitOfMeasureId) ?? template?.unitOfMeasure ?? mockUnitsOfMeasure[0];
    const assignedEmployee = mockEmployees.find(item => item.id === form.assignedToId) ?? mockEmployees.find(item => item.department?.id === department.id) ?? template?.assignedTo;
    const unit = mockDepartmentUnits.find(item => item.id === form.unitId);
    const wards = form.wardIds
      .split(',')
      .map(value => value.trim())
      .filter(Boolean)
      .map(value => mockWards.find(ward => ward.id === value || ward.name.toLowerCase() === value.toLowerCase()))
      .filter((item): item is typeof mockWards[number] => !!item);

    const nextTarget: OPMSTarget = {
      ...(template ?? opmsTargets[0]),
      id: editingTarget?.id ?? '',
      sourceTemplateId: form.sourceTemplateId || undefined,
      sourceTemplateVersion: form.sourceTemplateVersion ? Number(form.sourceTemplateVersion) : undefined,
      department,
      period,
      unit,
      wards,
      unitOfMeasure,
      assignedTo: assignedEmployee,
      indicatorNumber: form.indicatorNumber,
      targetName: form.targetName,
      annualTarget: Number(form.annualTarget || 0),
      baseline: Number(form.baseline || 0),
      weight: Number(form.weight || 0),
      kpiDescription: form.kpiDescription,
    };

    if (editingTarget) {
      const result = await updateOpmsTargetApi(editingTarget.id, buildPayloadFromTarget(nextTarget));
      if (result.success) {
        pushToast('success', 'OPMS target updated');
        await loadTargets();
      } else {
        pushToast('error', result.message ?? 'Failed to update OPMS target');
        return;
      }
    } else {
      const result = await createOpmsTargetApi(buildPayloadFromTarget(nextTarget));
      if (result.success) {
        pushToast('success', 'OPMS target created');
        await loadTargets();
      } else {
        pushToast('error', result.message ?? 'Failed to create OPMS target');
        return;
      }
    }

    setShowCreateModal(false);
    setEditingTarget(null);
  };

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
          openEditModal(row);
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
            <Button variant="primary" icon={<Plus className="w-4 h-4" />} onClick={openCreateModal}>
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

        {/* Create Modal */}
        <Modal
          isOpen={showCreateModal}
          onClose={() => { setShowCreateModal(false); setEditingTarget(null); }}
          title={editingTarget ? 'Edit OPMS Target' : 'Create New OPMS Target'}
          size="full"
        >
          <div className="space-y-5">
            <div className="rounded-2xl border border-primary-100 bg-gradient-to-r from-primary-50 to-white px-5 py-4 dark:border-primary-900/40 dark:from-primary-950/30 dark:to-secondary-900">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-primary-700 dark:text-primary-300">
                    Performance Target Setup
                  </p>
                  <h3 className="mt-1 text-lg font-semibold text-secondary-900 dark:text-white">
                    {editingTarget ? 'Update OPMS target details' : 'Create a new OPMS target'}
                  </h3>
                  <p className="mt-1 max-w-2xl text-sm text-secondary-600 dark:text-secondary-400">
                    Capture planning, ownership, and measurement information in a structured layout similar to the enterprise form style.
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Badge variant="info">{editingTarget ? 'Edit Mode' : 'New Record'}</Badge>
                  <Badge variant="default">OPMS</Badge>
                  {form.sourceTemplateId && <Badge variant="primary">Linked To Template</Badge>}
                </div>
              </div>
            </div>

            <div className="grid gap-4 xl:grid-cols-[0.95fr_1.35fr]">
              <FormPanel
                title="Planning Setup"
                description="Define the cycle and responsible municipal structure for the target."
                icon={<CalendarRange className="h-5 w-5" />}
              >
                <div className="rounded-xl border border-primary-100 bg-primary-50/80 px-4 py-3 dark:border-primary-900/40 dark:bg-primary-950/20">
                  <p className="text-xs font-semibold uppercase tracking-wide text-primary-700 dark:text-primary-300">Template Source</p>
                  <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">
                    {form.sourceTemplateId ? `${form.sourceTemplateId} (v${form.sourceTemplateVersion || '1'})` : 'Manual target creation'}
                  </p>
                  <p className="mt-1 text-xs text-secondary-500 dark:text-secondary-400">
                    Templates provide reusable generic defaults. Live OPMS targets still require period, department, assignee, unit, wards, and workflow-specific setup.
                  </p>
                </div>
                <FormRow cols={1}>
                  <Select
                    label="Period"
                    options={mockPeriods.map(period => ({ value: period.id, label: period.name }))}
                    value={form.periodId}
                    onChange={(e) => setForm(prev => ({ ...prev, periodId: e.target.value }))}
                  />
                </FormRow>
                <FormRow cols={2}>
                  <Select
                    label="Department"
                    options={mockDepartments.map(department => ({ value: department.id, label: department.name }))}
                    value={form.departmentId}
                    onChange={(e) => setForm(prev => ({ ...prev, departmentId: e.target.value }))}
                  />
                  <Select
                    label="Unit"
                    options={[
                      { value: '', label: 'No unit selected' },
                      ...mockDepartmentUnits
                        .filter(unit => !form.departmentId || unit.department.id === form.departmentId)
                        .map(unit => ({ value: unit.id, label: unit.name })),
                    ]}
                    value={form.unitId}
                    onChange={(e) => setForm(prev => ({ ...prev, unitId: e.target.value }))}
                  />
                </FormRow>
                <FormRow cols={2}>
                  <Select
                    label="Assigned User"
                    options={[
                      { value: '', label: 'Select employee' },
                      ...mockEmployees
                        .filter(employee => !form.departmentId || employee.department?.id === form.departmentId)
                        .map(employee => ({ value: employee.id, label: employee.displayName })),
                    ]}
                    value={form.assignedToId}
                    onChange={(e) => setForm(prev => ({ ...prev, assignedToId: e.target.value }))}
                  />
                  <Input
                    label="Wards"
                    value={form.wardIds}
                    onChange={(e) => setForm(prev => ({ ...prev, wardIds: e.target.value }))}
                    helpText="Enter ward ids or names separated by commas, for example: 1,2 or Ward 1,Ward 2."
                  />
                </FormRow>
                <div className="rounded-xl border border-secondary-200 bg-secondary-50/70 px-4 py-3 dark:border-secondary-700 dark:bg-secondary-800/60">
                  <p className="text-xs font-semibold uppercase tracking-wide text-secondary-500">Assignment</p>
                  <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">
                    {mockDepartments.find(department => department.id === form.departmentId)?.name ?? 'Department not selected'}
                  </p>
                  <p className="mt-1 text-xs text-secondary-500 dark:text-secondary-400">
                    Targets created here are automatically aligned to the selected municipal business unit.
                  </p>
                </div>
              </FormPanel>

              <FormPanel
                title="Target Definition"
                description="Capture the indicator identity and the descriptive performance statement."
                icon={<Target className="h-5 w-5" />}
              >
                <FormRow cols={2}>
                  <Input
                    label="Indicator Number"
                    value={form.indicatorNumber}
                    onChange={(e) => setForm(prev => ({ ...prev, indicatorNumber: e.target.value }))}
                  />
                  <Input
                    label="Target Name"
                    value={form.targetName}
                    onChange={(e) => setForm(prev => ({ ...prev, targetName: e.target.value }))}
                  />
                </FormRow>
                <Textarea
                  label="KPI Description"
                  rows={5}
                  value={form.kpiDescription}
                  onChange={(e) => setForm(prev => ({ ...prev, kpiDescription: e.target.value }))}
                  helpText="Describe the measurable service delivery or governance result expected from this target."
                />
              </FormPanel>
            </div>

            <div className="grid gap-4 xl:grid-cols-[1.35fr_0.95fr]">
              <FormPanel
                title="Measurement Details"
                description="Enter the baseline, annual expected result, and weighting used for evaluation."
                icon={<BarChart3 className="h-5 w-5" />}
              >
                <FormRow cols={4}>
                  <Input
                    label="Annual Target"
                    type="number"
                    value={form.annualTarget}
                    onChange={(e) => setForm(prev => ({ ...prev, annualTarget: e.target.value }))}
                  />
                  <Input
                    label="Baseline"
                    type="number"
                    value={form.baseline}
                    onChange={(e) => setForm(prev => ({ ...prev, baseline: e.target.value }))}
                  />
                  <Input
                    label="Weight %"
                    type="number"
                    value={form.weight}
                    onChange={(e) => setForm(prev => ({ ...prev, weight: e.target.value }))}
                  />
                  <Select
                    label="Unit Of Measure"
                    options={mockUnitsOfMeasure.map(unit => ({ value: unit.id, label: unit.name }))}
                    value={form.unitOfMeasureId}
                    onChange={(e) => setForm(prev => ({ ...prev, unitOfMeasureId: e.target.value }))}
                  />
                </FormRow>
              </FormPanel>

              <FormPanel
                title="Reference Summary"
                description="Preview the selected setup before saving the record."
                icon={<Building2 className="h-5 w-5" />}
              >
                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Department</p>
                    <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">
                      {mockDepartments.find(department => department.id === form.departmentId)?.name ?? '-'}
                    </p>
                  </div>
                  <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Period</p>
                    <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">
                      {mockPeriods.find(period => period.id === form.periodId)?.name ?? '-'}
                    </p>
                  </div>
                  <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Measure</p>
                    <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">
                      {mockUnitsOfMeasure.find(unit => unit.id === form.unitOfMeasureId)?.name ?? '-'}
                    </p>
                  </div>
                  <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Weight</p>
                    <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{form.weight || '0'}%</p>
                  </div>
                </div>
                <div className="rounded-xl border border-dashed border-secondary-300 bg-secondary-50 px-4 py-3 dark:border-secondary-700 dark:bg-secondary-800/50">
                  <div className="flex items-start gap-2">
                    <FileText className="mt-0.5 h-4 w-4 text-secondary-400" />
                    <p className="text-xs text-secondary-500 dark:text-secondary-400">
                      Use a concise target name and a measurable description so reporting and submissions remain consistent throughout the performance cycle.
                    </p>
                  </div>
                </div>
              </FormPanel>
            </div>
          </div>
          <div className="mt-6 flex items-center justify-end gap-3 border-t border-secondary-200 pt-4 dark:border-secondary-700">
            <Button variant="outline" onClick={() => { setShowCreateModal(false); setEditingTarget(null); }}>
              Cancel
            </Button>
            <Button variant="primary" onClick={() => { void handleSaveTarget(); }}>{editingTarget ? 'Save Changes' : 'Create Target'}</Button>
          </div>
        </Modal>
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
