import React, { useState } from 'react';
import { Plus, Download, Edit2, Eye, Trash2 } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { ConfirmDialog } from '../common/Modal';
import { Input, Select, Textarea, FormSection, FormRow } from '../common/Form';

// Generic CRUD page for simple lookup tables
interface LookupItem {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
}

interface GenericLookupPageProps {
  title: string;
  subtitle: string;
  entityName: string;
  mockData: LookupItem[];
  fields?: { key: string; label: string; type: 'text' | 'select' | 'textarea' }[];
}

export function GenericLookupPage({ title, subtitle, entityName, mockData, fields }: GenericLookupPageProps) {
  const [selectedItem, setSelectedItem] = useState<LookupItem | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  const defaultFields = fields || [
    { key: 'name', label: 'Name', type: 'text' as const },
    { key: 'code', label: 'Code', type: 'text' as const },
    { key: 'description', label: 'Description', type: 'textarea' as const },
  ];

  const columns = [
    { id: 'name', header: 'Name', accessor: (row: LookupItem) => <div><p className="font-medium">{row.name}</p><p className="text-[10px] text-secondary-500">{row.code}</p></div> },
    { id: 'description', header: 'Description', accessor: (row: LookupItem) => row.description || '-' },
    { id: 'status', header: 'Status', accessor: (row: LookupItem) => row.isActive ? <Badge size="sm" variant="success">Active</Badge> : <Badge size="sm" variant="error">Inactive</Badge> },
  ];

  const actions = (row: LookupItem) => (
    <div className="flex items-center justify-end gap-0.5">
      <button onClick={(e) => { e.stopPropagation(); setSelectedItem(row); }} className="p-1 rounded hover:bg-secondary-100"><Eye className="w-3.5 h-3.5 text-secondary-400" /></button>
      <button onClick={(e) => { e.stopPropagation(); setSelectedItem(row); setShowCreateModal(true); }} className="p-1 rounded hover:bg-secondary-100"><Edit2 className="w-3.5 h-3.5 text-secondary-400" /></button>
      <button onClick={(e) => { e.stopPropagation(); setSelectedItem(row); setShowDeleteConfirm(true); }} className="p-1 rounded hover:bg-error-50"><Trash2 className="w-3.5 h-3.5 text-error-400" /></button>
    </div>
  );

  return (
    <AppShell title={title} subtitle={subtitle}>
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{mockData.length} records</Badge>
          <div className="flex gap-1">
            <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Export</Button>
            <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => { setSelectedItem(null); setShowCreateModal(true); }}>Add</Button>
          </div>
        </div>
        <Card>
          <DataTable data={mockData} columns={columns} onRowClick={(row) => setSelectedItem(row)} actions={actions} getRowId={(row) => row.id} />
        </Card>

        <Modal isOpen={showCreateModal} onClose={() => { setShowCreateModal(false); setSelectedItem(null); }} title={`${selectedItem ? 'Edit' : 'New'} ${entityName}`} size="md">
          <div className="space-y-3">
            <FormRow cols={2}>
              {defaultFields.filter(f => f.type === 'text').slice(0, 2).map(field => (
                <Input key={field.key} label={field.label} defaultValue={selectedItem?.[field.key as keyof LookupItem] as string || ''} placeholder={`Enter ${field.label.toLowerCase()}`} required={field.key === 'name'} />
              ))}
            </FormRow>
            {defaultFields.filter(f => f.type === 'textarea').map(field => (
              <Textarea key={field.key} label={field.label} defaultValue={selectedItem?.[field.key as keyof LookupItem] as string || ''} rows={2} />
            ))}
          </div>
          <div className="flex justify-end gap-2 mt-4">
            <Button variant="ghost" size="sm" onClick={() => { setShowCreateModal(false); setSelectedItem(null); }}>Cancel</Button>
            <Button variant="primary" size="sm">{selectedItem ? 'Update' : 'Create'}</Button>
          </div>
        </Modal>

        <ConfirmDialog
          isOpen={showDeleteConfirm}
          onClose={() => { setShowDeleteConfirm(false); setSelectedItem(null); }}
          onConfirm={() => { setShowDeleteConfirm(false); setSelectedItem(null); }}
          title={`Delete ${entityName}`}
          message={`Are you sure you want to delete "${selectedItem?.name}"? This action cannot be undone.`}
          confirmLabel="Delete"
          variant="danger"
        />

        <Modal isOpen={!!selectedItem && !showCreateModal} onClose={() => setSelectedItem(null)} title={selectedItem?.name} size="sm">
          {selectedItem && (
            <div className="space-y-3">
              <div><p className="text-[10px] text-secondary-500">Code</p><p className="text-sm font-medium">{selectedItem.code}</p></div>
              {selectedItem.description && <div><p className="text-[10px] text-secondary-500">Description</p><p className="text-xs">{selectedItem.description}</p></div>}
              <div><p className="text-[10px] text-secondary-500">Status</p><Badge size="sm" variant={selectedItem.isActive ? 'success' : 'error'}>{selectedItem.isActive ? 'Active' : 'Inactive'}</Badge></div>
              <div className="flex justify-end gap-2 pt-3 border-t">
                <Button variant="ghost" size="sm" onClick={() => setSelectedItem(null)}>Close</Button>
                <Button variant="outline" size="sm" onClick={() => { setShowCreateModal(true); }}>Edit</Button>
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AppShell>
  );
}

// Mock data generators
export const mockCountries = [
  { id: '1', name: 'South Africa', code: 'ZA', description: 'Republic of South Africa', isActive: true },
  { id: '2', name: 'Namibia', code: 'NA', description: 'Republic of Namibia', isActive: true },
  { id: '3', name: 'Botswana', code: 'BW', description: 'Republic of Botswana', isActive: true },
];

export const mockProvinces = [
  { id: '1', name: 'Gauteng', code: 'GP', description: 'Gauteng Province', isActive: true },
  { id: '2', name: 'Western Cape', code: 'WC', description: 'Western Cape Province', isActive: true },
  { id: '3', name: 'KwaZulu-Natal', code: 'KZN', description: 'KwaZulu-Natal Province', isActive: true },
  { id: '4', name: 'Eastern Cape', code: 'EC', description: 'Eastern Cape Province', isActive: true },
];

export const mockCities = [
  { id: '1', name: 'Johannesburg', code: 'JHB', description: 'City of Johannesburg', isActive: true },
  { id: '2', name: 'Cape Town', code: 'CPT', description: 'City of Cape Town', isActive: true },
  { id: '3', name: 'Durban', code: 'DBN', description: 'City of Durban', isActive: true },
  { id: '4', name: 'Pretoria', code: 'PTA', description: 'City of Pretoria', isActive: true },
];

export const mockSuburbs = [
  { id: '1', name: 'Sandton', code: 'SAND', description: 'Sandton Business District', isActive: true },
  { id: '2', name: 'Rosebank', code: 'ROSE', description: 'Rosebank', isActive: true },
  { id: '3', name: 'Brooklyn', code: 'BRK', description: 'Brooklyn, Pretoria', isActive: true },
];

export const mockAddresses = [
  { id: '1', name: 'Head Office', code: 'HQ', description: '123 Main Street, Sandton', isActive: true },
  { id: '2', name: 'Regional Office North', code: 'NORTH', description: '456 Pretoria Road, Pretoria', isActive: true },
  { id: '3', name: 'Regional Office South', code: 'SOUTH', description: '789 Cape Road, Cape Town', isActive: true },
];

export const mockOrganisations = [
  { id: '1', name: 'Metro Municipality', code: 'METRO', description: 'Metropolitan Municipality', isActive: true },
  { id: '2', name: 'Provincial Government', code: 'PROV', description: 'Provincial Government', isActive: true },
  { id: '3', name: 'National Treasury', code: 'NT', description: 'National Treasury', isActive: true },
];

export const mockIndustries = [
  { id: '1', name: 'Government', code: 'GOV', description: 'Government Sector', isActive: true },
  { id: '2', name: 'Healthcare', code: 'HLTH', description: 'Healthcare Sector', isActive: true },
  { id: '3', name: 'Education', code: 'EDU', description: 'Education Sector', isActive: true },
];

export const mockContacts = [
  { id: '1', name: 'John Smith', code: 'JS01', description: 'john@municipality.gov', isActive: true },
  { id: '2', name: 'Jane Doe', code: 'JD01', description: 'jane@municipality.gov', isActive: true },
];

export const mockResumes = [
  { id: '1', name: 'Resume - John Smith', code: 'RES-JS01', description: 'Updated 2024-01-15', isActive: true },
  { id: '2', name: 'Resume - Jane Doe', code: 'RES-JD01', description: 'Updated 2024-02-20', isActive: true },
];

export const mockPortfolios = [
  { id: '1', name: 'Portfolio - John Smith', code: 'PORT-JS01', description: 'Professional portfolio', isActive: true },
];

// Dedicated page components
export function CountriesPage() {
  return <GenericLookupPage title="Countries" subtitle="Manage country records" entityName="Country" mockData={mockCountries} />;
}

export function ProvincesPage() {
  return <GenericLookupPage title="Provinces" subtitle="Manage province records" entityName="Province" mockData={mockProvinces} />;
}

export function CitiesPage() {
  return <GenericLookupPage title="Cities" subtitle="Manage city records" entityName="City" mockData={mockCities} />;
}

export function SuburbsPage() {
  return <GenericLookupPage title="Suburbs" subtitle="Manage suburb records" entityName="Suburb" mockData={mockSuburbs} />;
}

export function AddressesPage() {
  return <GenericLookupPage title="Addresses" subtitle="Manage address records" entityName="Address" mockData={mockAddresses} />;
}

export function OrganisationsPage() {
  return <GenericLookupPage title="Organisations" subtitle="Manage organisation records" entityName="Organisation" mockData={mockOrganisations} />;
}

export function IndustriesPage() {
  return <GenericLookupPage title="Industries" subtitle="Manage industry records" entityName="Industry" mockData={mockIndustries} />;
}

export function ContactsPage() {
  return <GenericLookupPage title="Contacts" subtitle="Manage contact records" entityName="Contact" mockData={mockContacts} />;
}

export function ResumesPage() {
  return <GenericLookupPage title="Resumes" subtitle="Manage employee resumes" entityName="Resume" mockData={mockResumes} />;
}

export function OccupationsPage() {
  const occupations = [
    { id: '1', name: 'Manager', code: 'MGR', description: 'Management Position', isActive: true },
    { id: '2', name: 'Officer', code: 'OFF', description: 'Officer Position', isActive: true },
    { id: '3', name: 'Administrator', code: 'ADMIN', description: 'Administrative Position', isActive: true },
  ];
  return <GenericLookupPage title="Occupations" subtitle="Manage occupation classifications" entityName="Occupation" mockData={occupations} />;
}

export function BudgetTypesPage() {
  const budgetTypes = [
    { id: '1', name: 'Capital Expenditure', code: 'CAPEX', description: 'Capital projects', isActive: true },
    { id: '2', name: 'Operating Expenditure', code: 'OPEX', description: 'Operational costs', isActive: true },
    { id: '3', name: 'Maintenance', code: 'MAINT', description: 'Maintenance budget', isActive: true },
  ];
  return <GenericLookupPage title="Budget Types" subtitle="Manage budget type records" entityName="Budget Type" mockData={budgetTypes} />;
}

export function StrategicGoalsPage() {
  const goals = [
    { id: '1', name: 'Good Governance', code: 'SG1', description: 'Enhance governance and accountability', isActive: true },
    { id: '2', name: 'Service Delivery', code: 'SG2', description: 'Deliver quality services', isActive: true },
    { id: '3', name: 'Economic Development', code: 'SG3', description: 'Promote economic growth', isActive: true },
  ];
  return <GenericLookupPage title="Strategic Goals" subtitle="Manage strategic goals" entityName="Strategic Goal" mockData={goals} />;
}

export function StrategicObjectivesPage() {
  const objectives = [
    { id: '1', name: 'Improve Infrastructure', code: 'SO1', description: 'Infrastructure improvement', isActive: true },
    { id: '2', name: 'Enhance Service Delivery', code: 'SO2', description: 'Service delivery enhancement', isActive: true },
    { id: '3', name: 'Increase Revenue', code: 'SO3', description: 'Revenue growth', isActive: true },
  ];
  return <GenericLookupPage title="Strategic Objectives" subtitle="Manage strategic objectives" entityName="Strategic Objective" mockData={objectives} />;
}

export function UnitOfMeasurePage() {
  const units = [
    { id: '1', name: 'Percentage', code: 'PCT', description: '%', isActive: true },
    { id: '2', name: 'Number', code: 'NUM', description: 'Count', isActive: true },
    { id: '3', name: 'Kilometers', code: 'KM', description: 'Distance', isActive: true },
    { id: '4', name: 'Rands', code: 'ZAR', description: 'Currency', isActive: true },
  ];
  return <GenericLookupPage title="Units of Measure" subtitle="Manage measurement units" entityName="Unit of Measure" mockData={units} />;
}

export function KPAsPage() {
  const kpas = [
    { id: '1', name: 'Basic Service Delivery', code: 'BSD', description: 'National KPA', isActive: true },
    { id: '2', name: 'Good Governance', code: 'GG', description: 'National KPA', isActive: true },
    { id: '3', name: 'Financial Viability', code: 'FV', description: 'National KPA', isActive: true },
  ];
  return <GenericLookupPage title="KPAs" subtitle="Key Performance Areas" entityName="KPA" mockData={kpas} />;
}

export function MunicipalKPAsPage() {
  const kpas = [
    { id: '1', name: 'Infrastructure Development', code: 'INFRA', description: 'Municipal KPA', isActive: true },
    { id: '2', name: 'Revenue Management', code: 'REV', description: 'Municipal KPA', isActive: true },
    { id: '3', name: 'Community Services', code: 'COMM', description: 'Municipal KPA', isActive: true },
  ];
  return <GenericLookupPage title="Municipal KPAs" subtitle="Municipal Key Performance Areas" entityName="Municipal KPA" mockData={kpas} />;
}

export function DepartmentalObjectivesPage() {
  const objectives = [
    { id: '1', name: 'Roads Maintenance', code: 'ROADS', description: 'Road infrastructure maintenance', isActive: true },
    { id: '2', name: 'Water Services', code: 'WATER', description: 'Water service delivery', isActive: true },
  ];
  return <GenericLookupPage title="Departmental Objectives" subtitle="Departmental objectives" entityName="Departmental Objective" mockData={objectives} />;
}

export function OutputsPage() {
  const outputs = [
    { id: '1', name: 'Roads Repaired', code: 'OUT1', description: 'Number of roads repaired', isActive: true },
    { id: '2', name: 'Connections Installed', code: 'OUT2', description: 'Water connections installed', isActive: true },
  ];
  return <GenericLookupPage title="Outputs" subtitle="Output definitions" entityName="Output" mockData={outputs} />;
}

export function PerformanceObjectivesPage() {
  const objectives = [
    { id: '1', name: 'Maintain Quality Standards', code: 'PO1', description: 'Quality assurance', isActive: true },
    { id: '2', name: 'Reduce Turnaround Time', code: 'PO2', description: 'Efficiency improvement', isActive: true },
  ];
  return <GenericLookupPage title="Performance Objectives" subtitle="Performance objectives" entityName="Performance Objective" mockData={objectives} />;
}

export function PriorityIssuesPage() {
  const issues = [
    { id: '1', name: 'Infrastructure Backlog', code: 'PI1', description: 'Infrastructure maintenance backlog', isActive: true },
    { id: '2', name: 'Revenue Collection', code: 'PI2', description: 'Revenue collection challenges', isActive: true },
  ];
  return <GenericLookupPage title="Priority Issues" subtitle="Priority issues tracking" entityName="Priority Issue" mockData={issues} />;
}
