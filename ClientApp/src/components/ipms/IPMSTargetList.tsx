import React, { useState } from 'react';
import { Plus, Download, Eye, Edit2, Link2, Trash2, CalendarRange, Building2, BarChart3, FileText, UserSquare2 } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Input, Select, Textarea, FormRow } from '../common/Form';
import { useApp } from '../../context/AppContext';
import { mockIPMSTargets, mockDepartments, mockOPMSTargets, mockUnitsOfMeasure, mockPeriods, mockEmployees } from '../../data/mockData';
import type { IPMSTarget } from '../../types';

function FormPanel({
  title,
  description,
  icon,
  children,
  className = '',
}: {
  title: string;
  description: string;
  icon: React.ReactNode;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <Card className={`rounded-2xl border border-secondary-200/80 bg-white shadow-sm ${className}`} padding="lg">
      <div className="mb-4 flex items-start gap-3">
        <div className="rounded-xl bg-primary-50 p-2 text-primary-600 dark:bg-primary-900/40 dark:text-primary-300">
          {icon}
        </div>
        <div>
          <h3 className="text-sm font-semibold text-secondary-900 dark:text-white">{title}</h3>
          <p className="mt-1 text-xs text-secondary-500 dark:text-secondary-400">{description}</p>
        </div>
      </div>
      <div className="space-y-4">{children}</div>
    </Card>
  );
}

export function IPMSTargetList() {
  const {
    setCurrentPath,
    ipmsTargets,
    createIPMSTarget,
    updateIPMSTarget,
    deleteIPMSTarget,
    duplicateIPMSTarget,
    pushToast,
  } = useApp();
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingTarget, setEditingTarget] = useState<IPMSTarget | null>(null);
  const [form, setForm] = useState({
    relatedOPMSTargetId: '',
    periodId: '',
    departmentId: '',
    unitOfMeasureId: '',
    indicatorNumber: '',
    targetName: '',
    annualTarget: '0',
    baseline: '0',
    weight: '0',
    kpiDescription: '',
  });

  const handleRowClick = (row: IPMSTarget) => {
    setCurrentPath(`/ipms/targets/${row.id}`);
  };

  const openCreateModal = () => {
    setEditingTarget(null);
    const template = ipmsTargets[0] ?? mockIPMSTargets[0];
    setForm({
      relatedOPMSTargetId: template.relatedOPMSTarget?.id ?? '',
      periodId: template.period.id,
      departmentId: template.department.id,
      unitOfMeasureId: template.unitOfMeasure.id,
      indicatorNumber: '',
      targetName: '',
      annualTarget: String(template.annualTarget),
      baseline: String(template.baseline),
      weight: String(template.weight),
      kpiDescription: '',
    });
    setShowCreateModal(true);
  };

  const openEditModal = (target: IPMSTarget) => {
    setEditingTarget(target);
    setForm({
      relatedOPMSTargetId: target.relatedOPMSTarget?.id ?? '',
      periodId: target.period.id,
      departmentId: target.department.id,
      unitOfMeasureId: target.unitOfMeasure.id,
      indicatorNumber: target.indicatorNumber,
      targetName: target.targetName,
      annualTarget: String(target.annualTarget),
      baseline: String(target.baseline),
      weight: String(target.weight),
      kpiDescription: target.kpiDescription,
    });
    setShowCreateModal(true);
  };

  const handleSaveTarget = () => {
    const template = editingTarget ?? ipmsTargets[0] ?? mockIPMSTargets[0];
    const department = mockDepartments.find(item => item.id === form.departmentId) ?? template.department;
    const period = mockPeriods.find(item => item.id === form.periodId) ?? template.period;
    const unitOfMeasure = mockUnitsOfMeasure.find(item => item.id === form.unitOfMeasureId) ?? template.unitOfMeasure;
    const assignedEmployee = mockEmployees.find(item => item.department?.id === department.id) ?? template.assignedTo ?? mockEmployees[0];
    const relatedOPMSTarget = mockOPMSTargets.find(item => item.id === form.relatedOPMSTargetId);

    const nextTarget: IPMSTarget = {
      ...template,
      id: editingTarget?.id ?? '',
      department,
      period,
      unitOfMeasure,
      assignedTo: assignedEmployee,
      relatedOPMSTarget,
      indicatorNumber: form.indicatorNumber,
      targetName: form.targetName,
      annualTarget: Number(form.annualTarget || 0),
      baseline: Number(form.baseline || 0),
      weight: Number(form.weight || 0),
      kpiDescription: form.kpiDescription,
    };

    if (editingTarget) {
      updateIPMSTarget(nextTarget);
      pushToast('success', 'IPMS target updated');
    } else {
      createIPMSTarget(nextTarget);
      pushToast('success', 'IPMS target created');
    }

    setShowCreateModal(false);
    setEditingTarget(null);
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
          openEditModal(row);
        }}
        className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700"
        title="Edit"
      >
        <Edit2 className="w-4 h-4 text-secondary-400" />
      </button>
      <button
        onClick={(e) => {
          e.stopPropagation();
          duplicateIPMSTarget(row.id);
          pushToast('success', 'IPMS target copied');
        }}
        className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700"
        title="Link to OPMS"
      >
        <Link2 className="w-4 h-4 text-secondary-400" />
      </button>
      <button
        onClick={(e) => {
          e.stopPropagation();
          deleteIPMSTarget(row.id);
          pushToast('success', 'IPMS target deleted');
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
            <Button variant="primary" icon={<Plus className="w-4 h-4" />} onClick={openCreateModal}>
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
            emptyMessage="No IPMS targets found"
            getRowId={(row) => row.id}
          />
        </Card>

        <Modal
          isOpen={showCreateModal}
          onClose={() => { setShowCreateModal(false); setEditingTarget(null); }}
          title={editingTarget ? 'Edit IPMS Target' : 'Create IPMS Target'}
          size="full"
        >
          <div className="space-y-5">
            <div className="rounded-2xl border border-primary-100 bg-gradient-to-r from-primary-50 to-white px-5 py-4 dark:border-primary-900/40 dark:from-primary-950/30 dark:to-secondary-900">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-primary-700 dark:text-primary-300">
                    Individual Performance Setup
                  </p>
                  <h3 className="mt-1 text-lg font-semibold text-secondary-900 dark:text-white">
                    {editingTarget ? 'Update IPMS target details' : 'Create a new IPMS target'}
                  </h3>
                  <p className="mt-1 max-w-2xl text-sm text-secondary-600 dark:text-secondary-400">
                    Capture employee performance information with a cleaner card-based add/edit interface aligned to the layout in your example.
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Badge variant="info">{editingTarget ? 'Edit Mode' : 'New Record'}</Badge>
                  <Badge variant="default">IPMS</Badge>
                </div>
              </div>
            </div>

            <div className="grid gap-4 xl:grid-cols-[1fr_1.3fr]">
              <FormPanel
                title="Alignment"
                description="Select the planning period, organizational owner, and any related OPMS target."
                icon={<CalendarRange className="h-5 w-5" />}
              >
                <Select
                  label="Link To OPMS Target"
                  options={[{ value: '', label: 'None' }, ...mockOPMSTargets.map(target => ({ value: target.id, label: `${target.indicatorNumber} - ${target.targetName}` }))]}
                  value={form.relatedOPMSTargetId}
                  onChange={(e) => setForm(prev => ({ ...prev, relatedOPMSTargetId: e.target.value }))}
                />
                <FormRow cols={2}>
                  <Select
                    label="Period"
                    options={mockPeriods.map(period => ({ value: period.id, label: period.name }))}
                    value={form.periodId}
                    onChange={(e) => setForm(prev => ({ ...prev, periodId: e.target.value }))}
                  />
                  <Select
                    label="Department"
                    options={mockDepartments.map(department => ({ value: department.id, label: department.name }))}
                    value={form.departmentId}
                    onChange={(e) => setForm(prev => ({ ...prev, departmentId: e.target.value }))}
                  />
                </FormRow>
                <div className="rounded-xl border border-secondary-200 bg-secondary-50/70 px-4 py-3 dark:border-secondary-700 dark:bg-secondary-800/60">
                  <p className="text-xs font-semibold uppercase tracking-wide text-secondary-500">Linked Strategic Target</p>
                  <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">
                    {mockOPMSTargets.find(target => target.id === form.relatedOPMSTargetId)?.targetName ?? 'No OPMS target linked'}
                  </p>
                  <p className="mt-1 text-xs text-secondary-500 dark:text-secondary-400">
                    Link the individual target where direct contribution to an organizational indicator is required.
                  </p>
                </div>
              </FormPanel>

              <FormPanel
                title="Target Definition"
                description="Define the employee-level indicator and expected outcome."
                icon={<UserSquare2 className="h-5 w-5" />}
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
                  helpText="Describe the personal performance deliverable, output, or service standard being measured."
                />
              </FormPanel>
            </div>

            <div className="grid gap-4 xl:grid-cols-[1.3fr_1fr]">
              <FormPanel
                title="Performance Measures"
                description="Set the numeric baseline, target expectation, weighting, and unit."
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
                description="Quick review of the selected IPMS setup before saving."
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
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">OPMS Link</p>
                    <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">
                      {mockOPMSTargets.find(target => target.id === form.relatedOPMSTargetId)?.indicatorNumber ?? 'None'}
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
                      Use clear and specific wording so managers, verifiers, and approvers can interpret the target consistently during the review cycle.
                    </p>
                  </div>
                </div>
              </FormPanel>
            </div>
          </div>
          <div className="mt-6 flex justify-end gap-3 border-t border-secondary-200 pt-4 dark:border-secondary-700">
            <Button variant="outline" onClick={() => { setShowCreateModal(false); setEditingTarget(null); }}>Cancel</Button>
            <Button variant="primary" onClick={handleSaveTarget}>{editingTarget ? 'Save Changes' : 'Create Target'}</Button>
          </div>
        </Modal>
      </div>
    </AppShell>
  );
}
