import React, { useState } from 'react';
import { Plus, Download, Edit2, Eye, Trash2 } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card, EmptyState } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Input, Select, FormSection, FormRow } from '../common/Form';
import {
  mockPeriods,
  mockBudgetSources,
  mockBudgetTypes,
  mockUnitsOfMeasure,
  mockStrategicGoals,
  mockKPAs,
  mockApprovalSetups,
  mockEmployees,
  mockDepartments,
} from '../../data/mockData';
import type { Period, ApprovalSetup } from '../../types';

export function PeriodList() {
  const columns = [
    {
      id: 'name',
      header: 'Period Name',
      accessor: (row: Period) => (
        <div>
          <p className="font-medium">{row.name}</p>
          <p className="text-xs text-secondary-500">{row.fiscalYear}</p>
        </div>
      ),
    },
    {
      id: 'dates',
      header: 'Date Range',
      accessor: (row: Period) => `${new Date(row.startDate).toLocaleDateString()} - ${new Date(row.endDate).toLocaleDateString()}`,
    },
    {
      id: 'status',
      header: 'Status',
      accessor: (row: Period) => (
        row.isActive ? <Badge variant="success">Active</Badge> : <Badge variant="default">Closed</Badge>
      ),
    },
  ];

  return (
    <AppShell title="Periods" subtitle="Manage financial periods">
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{mockPeriods.length} periods</Badge>
          <Button variant="primary" icon={<Plus className="w-4 h-4" />}>Add Period</Button>
        </div>
        <Card>
          <DataTable data={mockPeriods} columns={columns} getRowId={(row) => row.id} />
        </Card>
      </div>
    </AppShell>
  );
}

export function BudgetSourceList() {
  const columns = [
    { id: 'name', header: 'Budget Source', accessor: (row: typeof mockBudgetSources[0]) => row.name },
    { id: 'code', header: 'Code', accessor: (row: typeof mockBudgetSources[0]) => row.code },
    { id: 'status', header: 'Status', accessor: (row: typeof mockBudgetSources[0]) => row.isActive ? <Badge variant="success">Active</Badge> : <Badge variant="error">Inactive</Badge> },
  ];

  return (
    <AppShell title="Budget Sources" subtitle="Manage budget source configurations">
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{mockBudgetSources.length} budget sources</Badge>
          <Button variant="primary" icon={<Plus className="w-4 h-4" />}>Add Source</Button>
        </div>
        <Card>
          <DataTable data={mockBudgetSources} columns={columns} getRowId={(row) => row.id} />
        </Card>
      </div>
    </AppShell>
  );
}

export function ApprovalSetupList() {
  const [showModal, setShowModal] = useState(false);

  const columns = [
    {
      id: 'user',
      header: 'User',
      accessor: (row: ApprovalSetup) => (
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-primary-100 flex items-center justify-center">
            <span className="text-sm font-medium text-primary-700">
              {row.user.firstName[0]}{row.user.lastName[0]}
            </span>
          </div>
          <div>
            <p className="font-medium">{row.user.displayName}</p>
            <p className="text-xs text-secondary-500">{row.user.email}</p>
          </div>
        </div>
      ),
    },
    {
      id: 'approver',
      header: 'Approver',
      accessor: (row: ApprovalSetup) => (
        row.approver ? (
          <div>
            <p className="font-medium">{row.approver.displayName}</p>
            <p className="text-xs text-secondary-500">{row.approver.email}</p>
          </div>
        ) : <span className="text-secondary-400">Not assigned</span>
      ),
    },
    {
      id: 'department',
      header: 'Department',
      accessor: (row: ApprovalSetup) => row.department?.name ?? '-',
    },
    {
      id: 'admin',
      header: 'Admin Approver',
      accessor: (row: ApprovalSetup) => (
        row.isAdminApprover ? <Badge variant="success">Yes</Badge> : <Badge variant="default">No</Badge>
      ),
    },
    {
      id: 'status',
      header: 'Status',
      accessor: (row: ApprovalSetup) => (
        row.isActive ? <Badge variant="success">Active</Badge> : <Badge variant="error">Inactive</Badge>
      ),
    },
  ];

  const actions = (row: ApprovalSetup) => (
    <div className="flex items-center justify-end gap-1">
      <button className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700">
        <Edit2 className="w-4 h-4 text-secondary-400" />
      </button>
    </div>
  );

  return (
    <AppShell title="Approval Setup" subtitle="Configure approval workflows">
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{mockApprovalSetups.length} configurations</Badge>
          <Button variant="primary" icon={<Plus className="w-4 h-4" />} onClick={() => setShowModal(true)}>
            Add Configuration
          </Button>
        </div>
        <Card>
          <DataTable
            data={mockApprovalSetups}
            columns={columns}
            actions={actions}
            getRowId={(row) => row.id}
          />
        </Card>

        <Modal
          isOpen={showModal}
          onClose={() => setShowModal(false)}
          title="Add Approval Configuration"
          size="lg"
        >
          <div className="space-y-4">
            <Select
              label="User"
              options={mockEmployees.map(e => ({ value: e.id, label: `${e.displayName} (${e.email})` }))}
              placeholder="Select user"
            />
            <Select
              label="Approver"
              options={mockEmployees.map(e => ({ value: e.id, label: `${e.displayName} (${e.email})` }))}
              placeholder="Select approver"
            />
            <Select
              label="Department"
              options={mockDepartments.map(d => ({ value: d.id, label: d.name }))}
              placeholder="Select department"
            />
            <Input label="Admin Approver" type="checkbox" />
          </div>
          <div className="flex justify-end gap-3 mt-6">
            <Button variant="outline" onClick={() => setShowModal(false)}>Cancel</Button>
            <Button variant="primary">Save</Button>
          </div>
        </Modal>
      </div>
    </AppShell>
  );
}

export function LookupTables() {
  const [activeTable, setActiveTable] = useState<string | null>(null);

  const tables = [
    { id: 'uom', label: 'Units of Measure', data: mockUnitsOfMeasure },
    { id: 'budget-types', label: 'Budget Types', data: mockBudgetTypes },
    { id: 'strategic-goals', label: 'Strategic Goals', data: mockStrategicGoals },
    { id: 'kpas', label: 'KPAs', data: mockKPAs },
  ];

  return (
    <AppShell title="Lookup Tables" subtitle="Manage system lookup values">
      <div className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {tables.map(table => (
            <button
              key={table.id}
              onClick={() => setActiveTable(activeTable === table.id ? null : table.id)}
              className={`p-4 rounded-xl border transition-all ${
                activeTable === table.id
                  ? 'bg-primary-50 border-primary-200'
                  : 'bg-white dark:bg-secondary-900 border-secondary-200 dark:border-secondary-700 hover:shadow-md'
              }`}
            >
              <div className="flex items-center justify-between">
                <h3 className="font-medium text-secondary-900 dark:text-white">{table.label}</h3>
                <Badge variant="default">{table.data.length}</Badge>
              </div>
            </button>
          ))}
        </div>

        {activeTable && (
          <Card>
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-secondary-900 dark:text-white">
                {tables.find(t => t.id === activeTable)?.label}
              </h3>
              <div className="flex items-center gap-2">
                <Button variant="outline" size="sm" icon={<Download className="w-4 h-4" />}>
                  Export
                </Button>
                <Button variant="primary" size="sm" icon={<Plus className="w-4 h-4" />}>
                  Add Item
                </Button>
              </div>
            </div>
            <div className="space-y-2">
              {tables.find(t => t.id === activeTable)?.data.map((item: Record<string, unknown>) => (
                <div
                  key={item.id as string}
                  className="flex items-center justify-between p-3 bg-secondary-50 dark:bg-secondary-800 rounded-lg"
                >
                  <div>
                    <p className="font-medium text-secondary-900 dark:text-white">{item.name as string}</p>
                    <p className="text-xs text-secondary-500">{item.code as string}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant={item.isActive as boolean ? 'success' : 'error'}>
                      {(item.isActive as boolean) ? 'Active' : 'Inactive'}
                    </Badge>
                    <button className="p-1.5 rounded hover:bg-secondary-100 dark:hover:bg-secondary-700">
                      <Edit2 className="w-4 h-4 text-secondary-400" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </Card>
        )}
      </div>
    </AppShell>
  );
}
