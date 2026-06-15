import React, { useState } from 'react';
import {
  Plus,
  Filter,
  Download,
  MoreHorizontal,
  Eye,
  Edit2,
  Trash2,
  Copy,
} from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card, EmptyState } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { useApp } from '../../context/AppContext';
import { mockOPMSTargets, mockDepartments, mockPeriods, statusLabels } from '../../data/mockData';
import type { OPMSTarget } from '../../types';
import { Target } from 'lucide-react';

const statusBadgeVariant = (status: string): 'default' | 'primary' | 'success' | 'warning' | 'error' | 'info' => {
  switch (status) {
    case 'approved':
    case 'completed':
      return 'success';
    case 'pending_verification':
    case 'pending_approval':
      return 'warning';
    case 'rejected':
      return 'error';
    case 'submitted':
    case 'verified':
      return 'info';
    default:
      return 'default';
  }
};

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
  const { setCurrentPath } = useApp();
  const [showCreateModal, setShowCreateModal] = useState(false);

  const handleRowClick = (row: OPMSTarget) => {
    setCurrentPath(`/opms/targets/${row.id}`);
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
        onClick={(e) => e.stopPropagation()}
        className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700 transition-colors"
        title="Edit"
      >
        <Edit2 className="w-4 h-4 text-secondary-400" />
      </button>
      <button
        onClick={(e) => e.stopPropagation()}
        className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700 transition-colors"
        title="Copy"
      >
        <Copy className="w-4 h-4 text-secondary-400" />
      </button>
    </div>
  );

  return (
    <AppShell title="OPMS Targets" subtitle="Organizational Performance Management System targets">
      <div className="space-y-6">
        {/* Header actions */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Badge variant="primary">{mockOPMSTargets.length} targets</Badge>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" icon={<Download className="w-4 h-4" />}>
              Export
            </Button>
            <Button variant="primary" icon={<Plus className="w-4 h-4" />} onClick={() => setShowCreateModal(true)}>
              New Target
            </Button>
          </div>
        </div>

        {/* Filters */}
        <OPMSTargetFilters onFilterChange={() => {}} />

        {/* Data Table */}
        <DataTable
          data={mockOPMSTargets}
          columns={columns}
          onRowClick={handleRowClick}
          actions={actions}
          searchPlaceholder="Search targets..."
          emptyMessage="No OPMS targets found"
          getRowId={(row) => row.id}
        />

        {/* Create Modal */}
        <Modal
          isOpen={showCreateModal}
          onClose={() => setShowCreateModal(false)}
          title="Create New OPMS Target"
          size="xl"
        >
          <div className="space-y-4">
            <p className="text-sm text-secondary-600 dark:text-secondary-400">
              Choose how you want to create the new target:
            </p>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <button className="flex flex-col items-center gap-3 p-6 border border-secondary-200 dark:border-secondary-700 rounded-xl hover:bg-secondary-50 dark:hover:bg-secondary-800 transition-colors">
                <div className="p-3 bg-primary-100 dark:bg-primary-900 rounded-xl">
                  <Plus className="w-6 h-6 text-primary-600 dark:text-primary-400" />
                </div>
                <div className="text-center">
                  <p className="font-medium text-secondary-900 dark:text-white">Create New</p>
                  <p className="text-xs text-secondary-500 dark:text-secondary-400">Start from scratch</p>
                </div>
              </button>
              <button className="flex flex-col items-center gap-3 p-6 border border-secondary-200 dark:border-secondary-700 rounded-xl hover:bg-secondary-50 dark:hover:bg-secondary-800 transition-colors">
                <div className="p-3 bg-primary-100 dark:bg-primary-900 rounded-xl">
                  <Copy className="w-6 h-6 text-primary-600 dark:text-primary-400" />
                </div>
                <div className="text-center">
                  <p className="font-medium text-secondary-900 dark:text-white">Copy Existing</p>
                  <p className="text-xs text-secondary-500 dark:text-secondary-400">From another target</p>
                </div>
              </button>
              <button className="flex flex-col items-center gap-3 p-6 border border-secondary-200 dark:border-secondary-700 rounded-xl hover:bg-secondary-50 dark:hover:bg-secondary-800 transition-colors">
                <div className="p-3 bg-primary-100 dark:bg-primary-900 rounded-xl">
                  <Target className="w-6 h-6 text-primary-600 dark:text-primary-400" />
                </div>
                <div className="text-center">
                  <p className="font-medium text-secondary-900 dark:text-white">From KPI Library</p>
                  <p className="text-xs text-secondary-500 dark:text-secondary-400">Use a template</p>
                </div>
              </button>
            </div>
          </div>
          <div className="flex items-center justify-end gap-3 mt-6">
            <Button variant="outline" onClick={() => setShowCreateModal(false)}>
              Cancel
            </Button>
            <Button variant="primary">Continue</Button>
          </div>
        </Modal>
      </div>
    </AppShell>
  );
}
