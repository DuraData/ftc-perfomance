import { useState } from 'react';
import { Plus, Eye, Edit2, Download } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Tabs } from '../common/Tabs';
import { Input, Select, FormSection, FormRow, FormHero, FormPanel } from '../common/Form';
import { mockEmployees, mockDepartments, mockDepartmentUnits, mockPositions } from '../../data/mockData';
import type { Employee, Department, DepartmentUnit, Position } from '../../types';

function EmployeeDetailModal({ employee, isOpen, onClose }: { employee: Employee | null; isOpen: boolean; onClose: () => void }) {
  if (!employee) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={employee.displayName} size="lg">
      <Tabs tabs={[{ id: 'personal', label: 'Personal' }, { id: 'employment', label: 'Employment' }, { id: 'contact', label: 'Contact' }]} activeTab="personal" onChange={() => {}} variant="compact" />
      <div className="mt-4 space-y-3">
        <FormSection title="Personal">
          <FormRow cols={3}>
            <Input label="First Name" defaultValue={employee.firstName} />
            <Input label="Last Name" defaultValue={employee.lastName} />
            <Input label="Display Name" defaultValue={employee.displayName} />
          </FormRow>
          <FormRow cols={2}>
            <Input label="DOB" type="date" defaultValue={employee.dateOfBirth} />
            <Select label="ID Type" options={[{ value: 'sa_id', label: 'SA ID' }, { value: 'passport', label: 'Passport' }]} defaultValue="sa_id" />
          </FormRow>
        </FormSection>
        <FormSection title="Employment">
          <FormRow cols={2}>
            <Select label="Department" options={mockDepartments.map(d => ({ value: d.id, label: d.name }))} defaultValue={employee.department?.id} />
            <Select label="Position" options={mockPositions.map(p => ({ value: p.id, label: p.title }))} defaultValue={employee.position?.id} />
          </FormRow>
        </FormSection>
      </div>
      <div className="flex justify-end gap-2 mt-4">
        <Button variant="outline" size="sm" onClick={onClose}>Cancel</Button>
        <Button variant="primary" size="sm">Save</Button>
      </div>
    </Modal>
  );
}

export function EmployeeList() {
  const [selectedEmployee, setSelectedEmployee] = useState<Employee | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);

  const columns = [
    { id: 'name', header: 'Name', accessor: (row: Employee) => (
      <div className="flex items-center gap-2">
        <div className="w-7 h-7 rounded-full bg-primary-100 flex items-center justify-center">
          <span className="text-[10px] font-medium text-primary-700">{row.firstName[0]}{row.lastName[0]}</span>
        </div>
        <div><p className="font-medium text-secondary-900">{row.displayName}</p><p className="text-[10px] text-secondary-500">{row.email}</p></div>
      </div>
    )},
    { id: 'department', header: 'Department', accessor: (row: Employee) => row.department?.name ?? '-' },
    { id: 'position', header: 'Position', accessor: (row: Employee) => row.position?.title ?? '-' },
    { id: 'status', header: 'Status', accessor: (row: Employee) => row.isActive ? <Badge size="sm" variant="success">Active</Badge> : <Badge size="sm" variant="error">Inactive</Badge> },
  ];

  const actions = (row: Employee) => (
    <div className="flex items-center justify-end gap-0.5">
      <button onClick={(e) => { e.stopPropagation(); setSelectedEmployee(row); }} className="p-1 rounded hover:bg-secondary-100"><Eye className="w-3.5 h-3.5 text-secondary-400" /></button>
      <button className="p-1 rounded hover:bg-secondary-100"><Edit2 className="w-3.5 h-3.5 text-secondary-400" /></button>
    </div>
  );

  return (
    <AppShell title="Employees" subtitle="Employee records">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{mockEmployees.length} employees</Badge>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Export</Button>
            <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => setShowCreateModal(true)}>Add</Button>
          </div>
        </div>
        <Card>
          <DataTable data={mockEmployees} columns={columns} onRowClick={(row) => setSelectedEmployee(row)} actions={actions} getRowId={(row) => row.id} />
        </Card>
        <EmployeeDetailModal employee={selectedEmployee} isOpen={!!selectedEmployee} onClose={() => setSelectedEmployee(null)} />
        <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Add Employee" size="md">
          <div className="space-y-5">
            <FormHero
              eyebrow="HR Management"
              title="Create employee profile"
              description="Capture the employee's identity and organizational assignment using the standardized add/edit layout."
              badges={<Badge variant="default">New Employee</Badge>}
            />
            <div className="grid gap-4">
              <FormPanel title="Personal Information" description="Enter the employee's core profile details." icon={<Plus className="h-5 w-5" />}>
                <FormRow cols={2}>
                  <Input label="First Name" placeholder="First name" required />
                  <Input label="Last Name" placeholder="Last name" required />
                </FormRow>
                <Input label="Email" type="email" placeholder="Email" required />
              </FormPanel>
              <FormPanel title="Organization" description="Assign the employee to the correct department and position." icon={<Edit2 className="h-5 w-5" />}>
                <FormRow cols={2}>
                  <Select label="Department" options={mockDepartments.map(d => ({ value: d.id, label: d.name }))} placeholder="Select" required />
                  <Select label="Position" options={mockPositions.map(p => ({ value: p.id, label: p.title }))} placeholder="Select" required />
                </FormRow>
              </FormPanel>
            </div>
          </div>
          <div className="mt-6 flex justify-end gap-2 border-t border-secondary-200 pt-4 dark:border-secondary-700">
            <Button variant="outline" size="sm" onClick={() => setShowCreateModal(false)}>Cancel</Button>
            <Button variant="primary" size="sm">Add</Button>
          </div>
        </Modal>
      </div>
    </AppShell>
  );
}

export function DepartmentList() {
  const [selectedDepartment, setSelectedDepartment] = useState<Department | null>(null);

  const columns = [
    { id: 'name', header: 'Department', accessor: (row: Department) => <div><p className="font-medium">{row.name}</p><p className="text-[10px] text-secondary-500">{row.code}</p></div> },
    { id: 'manager', header: 'Manager', accessor: (row: Department) => row.manager?.displayName ?? '-' },
    { id: 'employees', header: 'Staff', accessor: () => Math.floor(Math.random() * 50 + 5) },
    { id: 'status', header: 'Status', accessor: (row: Department) => row.isActive ? <Badge size="sm" variant="success">Active</Badge> : <Badge size="sm" variant="error">Inactive</Badge> },
  ];

  return (
    <AppShell title="Departments" subtitle="Organizational departments">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{mockDepartments.length} departments</Badge>
          <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />}>Add</Button>
        </div>
        <Card>
          <DataTable data={mockDepartments} columns={columns} onRowClick={(row) => setSelectedDepartment(row)} getRowId={(row) => row.id} />
        </Card>
        <Modal isOpen={!!selectedDepartment} onClose={() => setSelectedDepartment(null)} title={selectedDepartment?.name} size="md">
          <Tabs tabs={[{ id: 'details', label: 'Details' }, { id: 'employees', label: 'Staff', badge: 15 }, { id: 'units', label: 'Units', badge: 3 }, { id: 'positions', label: 'Positions', badge: 5 }]} activeTab="details" onChange={() => {}} variant="compact" />
          <div className="mt-4">
            <FormSection title="Info">
              <FormRow cols={2}>
                <Input label="Name" defaultValue={selectedDepartment?.name} />
                <Input label="Code" defaultValue={selectedDepartment?.code} />
              </FormRow>
            </FormSection>
          </div>
          <div className="flex justify-end gap-2 mt-4">
            <Button variant="outline" size="sm" onClick={() => setSelectedDepartment(null)}>Cancel</Button>
            <Button variant="primary" size="sm">Save</Button>
          </div>
        </Modal>
      </div>
    </AppShell>
  );
}

export function DepartmentUnitList() {
  const columns = [
    { id: 'name', header: 'Unit', accessor: (row: DepartmentUnit) => <div><p className="font-medium">{row.name}</p><p className="text-[10px] text-secondary-500">{row.code}</p></div> },
    { id: 'department', header: 'Department', accessor: (row: DepartmentUnit) => row.department.name },
    { id: 'head', header: 'Head', accessor: (row: DepartmentUnit) => row.head?.displayName ?? '-' },
    { id: 'status', header: 'Status', accessor: (row: DepartmentUnit) => row.isActive ? <Badge size="sm" variant="success">Active</Badge> : <Badge size="sm" variant="error">Inactive</Badge> },
  ];

  return (
    <AppShell title="Department Units" subtitle="Department units">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{mockDepartmentUnits.length} units</Badge>
          <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />}>Add</Button>
        </div>
        <Card>
          <DataTable data={mockDepartmentUnits} columns={columns} getRowId={(row) => row.id} />
        </Card>
      </div>
    </AppShell>
  );
}

export function PositionList() {
  const columns = [
    { id: 'title', header: 'Position', accessor: (row: Position) => <div><p className="font-medium">{row.title}</p><p className="text-[10px] text-secondary-500">{row.code}</p></div> },
    { id: 'department', header: 'Department', accessor: (row: Position) => row.department.name },
    { id: 'level', header: 'Level', accessor: (row: Position) => `L${row.level}` },
    { id: 'status', header: 'Status', accessor: (row: Position) => row.isActive ? <Badge size="sm" variant="success">Active</Badge> : <Badge size="sm" variant="error">Inactive</Badge> },
  ];

  return (
    <AppShell title="Positions" subtitle="Organizational positions">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{mockPositions.length} positions</Badge>
          <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />}>Add</Button>
        </div>
        <Card>
          <DataTable data={mockPositions} columns={columns} getRowId={(row) => row.id} />
        </Card>
      </div>
    </AppShell>
  );
}
