import React, { useState } from 'react';
import { Plus, Download, Eye, Edit2, Link2 } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { useApp } from '../../context/AppContext';
import { mockIPMSTargets, mockDepartments, mockOPMSTargets } from '../../data/mockData';
import type { IPMSTarget } from '../../types';

export function IPMSTargetList() {
  const { setCurrentPath } = useApp();
  const [showCreateModal, setShowCreateModal] = useState(false);

  const handleRowClick = (row: IPMSTarget) => {
    setCurrentPath(`/ipms/targets/${row.id}`);
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
        onClick={(e) => e.stopPropagation()}
        className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700"
        title="Edit"
      >
        <Edit2 className="w-4 h-4 text-secondary-400" />
      </button>
      <button
        onClick={(e) => e.stopPropagation()}
        className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700"
        title="Link to OPMS"
      >
        <Link2 className="w-4 h-4 text-secondary-400" />
      </button>
    </div>
  );

  return (
    <AppShell title="IPMS Targets" subtitle="Individual Performance Management System targets">
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{mockIPMSTargets.length} targets</Badge>
          <div className="flex items-center gap-2">
            <Button variant="outline" icon={<Download className="w-4 h-4" />}>
              Export
            </Button>
            <Button variant="primary" icon={<Plus className="w-4 h-4" />} onClick={() => setShowCreateModal(true)}>
              New IPMS Target
            </Button>
          </div>
        </div>

        <Card>
          <DataTable
            data={mockIPMSTargets}
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
          onClose={() => setShowCreateModal(false)}
          title="Create IPMS Target"
          size="lg"
        >
          <div className="space-y-4">
            <p className="text-sm text-secondary-600">
              Create a new IPMS target for individual performance tracking.
            </p>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-secondary-700 mb-1">
                  Link to OPMS Target (Optional)
                </label>
                <select className="w-full px-3 py-2 border border-secondary-200 rounded-lg">
                  <option value="">Select OPMS target...</option>
                  {mockOPMSTargets.map(t => (
                    <option key={t.id} value={t.id}>{t.indicatorNumber} - {t.targetName}</option>
                  ))}
                </select>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-secondary-700 mb-1">Department</label>
                  <select className="w-full px-3 py-2 border border-secondary-200 rounded-lg">
                    {mockDepartments.map(d => (
                      <option key={d.id} value={d.id}>{d.name}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-secondary-700 mb-1">Assigned Employee</label>
                  <input type="text" className="w-full px-3 py-2 border border-secondary-200 rounded-lg" placeholder="Select employee" />
                </div>
              </div>
            </div>
          </div>
          <div className="flex justify-end gap-3 mt-6">
            <Button variant="outline" onClick={() => setShowCreateModal(false)}>Cancel</Button>
            <Button variant="primary">Create Target</Button>
          </div>
        </Modal>
      </div>
    </AppShell>
  );
}
