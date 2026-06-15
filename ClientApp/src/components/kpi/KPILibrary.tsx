import React, { useState } from 'react';
import { Plus, Eye, Edit2, Copy, Download, Target, Search } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card, EmptyState } from '../ui';
import { Modal } from '../common/Modal';
import { Input, Select, Textarea, FormSection, FormRow } from '../common/Form';
import { FileUpload } from '../common/FileUpload';
import { mockKPITemplates, mockDepartments, mockStrategicGoals, mockStrategicObjectives, mockBudgetSources, mockUnitsOfMeasure } from '../../data/mockData';
import type { KPITemplate } from '../../types';

function KPIFormModal({ isOpen, onClose, template }: { isOpen: boolean; onClose: () => void; template?: KPITemplate }) {
  const [step, setStep] = useState(1);
  const steps = [{ id: 1, title: 'Basic' }, { id: 2, title: 'Strategy' }, { id: 3, title: 'Metrics' }, { id: 4, title: 'References' }];

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={template ? 'Edit Template' : 'New Template'} size="lg">
      {/* Progress */}
      <div className="flex items-center gap-1 mb-4">
        {steps.map((s, idx) => (
          <React.Fragment key={s.id}>
            <button onClick={() => setStep(s.id)} className={`flex items-center gap-1.5 px-2 py-1 rounded text-xs font-medium ${step >= s.id ? 'bg-primary-600 text-white' : 'bg-secondary-100 text-secondary-600'}`}>
              <span className="w-4 h-4 rounded-full bg-white/20 flex items-center justify-center text-[10px]">{s.id}</span>
              {s.title}
            </button>
            {idx < steps.length - 1 && <div className={`h-0.5 w-4 ${step > s.id ? 'bg-primary-600' : 'bg-secondary-200'}`} />}
          </React.Fragment>
        ))}
      </div>

      {/* Steps */}
      <div className="min-h-[200px]">
        {step === 1 && (
          <div className="space-y-2">
            <Input label="Name" placeholder="Template name" required defaultValue={template?.name} />
            <FormRow cols={2}>
              <Select label="Department" options={mockDepartments.map(d => ({ value: d.id, label: d.name }))} placeholder="Select" defaultValue={template?.department?.id} />
              <Input label="Indicator #" placeholder="e.g., KPI-001" defaultValue={template?.indicatorNumber} />
            </FormRow>
            <Textarea label="Description" placeholder="KPI description" rows={2} defaultValue={template?.kpiDescription} />
          </div>
        )}
        {step === 2 && (
          <div className="space-y-2">
            <FormRow cols={2}>
              <Input label="National KPA" defaultValue={template?.nationalKPA} />
              <Input label="Municipal KPA" defaultValue={template?.municipalKPA} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Strategic Goal" options={mockStrategicGoals.map(g => ({ value: g.id, label: g.name }))} defaultValue={template?.strategicGoal?.id} />
              <Select label="Objective" options={mockStrategicObjectives.map(o => ({ value: o.id, label: o.name }))} defaultValue={template?.strategicObjective?.id} />
            </FormRow>
            <Input label="Performance Objective" defaultValue={template?.performanceObjective} />
          </div>
        )}
        {step === 3 && (
          <div className="space-y-2">
            <FormRow cols={3}>
              <Input label="Demand" type="number" defaultValue={template?.demand} />
              <Input label="Backlog" type="number" defaultValue={template?.backlog} />
              <Input label="Weight %" type="number" defaultValue={template?.weight} />
            </FormRow>
            <Select label="Unit of Measure" options={mockUnitsOfMeasure.map(u => ({ value: u.id, label: u.name }))} defaultValue={template?.unitOfMeasure?.id} />
          </div>
        )}
        {step === 4 && (
          <div className="space-y-2">
            <FormRow cols={2}>
              <Select label="Budget Source" options={mockBudgetSources.map(b => ({ value: b.id, label: b.name }))} defaultValue={template?.budgetSource?.id} />
              <Input label="IDP Reference" defaultValue={template?.idpReference} />
            </FormRow>
            <FormRow cols={2}>
              <Input label="Internal Ref" defaultValue={template?.internalReference} />
              <Input label="FMS Link" defaultValue={template?.fmsLink} />
            </FormRow>
            <FileUpload documentTypes={[{ value: 'kpi_doc', label: 'KPI Doc' }]} />
          </div>
        )}
      </div>

      {/* Navigation */}
      <div className="flex items-center justify-between mt-4 pt-3 border-t border-secondary-200">
        <Button variant="ghost" size="sm" onClick={() => setStep(s => Math.max(1, s - 1))} disabled={step === 1}>Back</Button>
        {step < 4 ? (
          <Button variant="primary" size="sm" onClick={() => setStep(s => s + 1)}>Next</Button>
        ) : (
          <Button variant="primary" size="sm" onClick={onClose}>{template ? 'Update' : 'Create'}</Button>
        )}
      </div>
    </Modal>
  );
}

export function KPILibrary() {
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [selectedTemplate, setSelectedTemplate] = useState<KPITemplate | null>(null);
  const [searchQuery, setSearchQuery] = useState('');

  const filteredTemplates = mockKPITemplates.filter(t =>
    t.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    t.kpiDescription.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <AppShell title="KPI Library" subtitle="Reusable KPI templates">
      <div className="space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <Badge variant="primary">{mockKPITemplates.length} templates</Badge>
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-secondary-400" />
              <input type="text" placeholder="Search..." value={searchQuery} onChange={(e) => setSearchQuery(e.target.value)} className="pl-8 pr-3 py-1.5 text-xs border border-secondary-200 dark:border-secondary-700 rounded bg-white dark:bg-secondary-800 focus:ring-1 focus:ring-primary-500 w-48" />
            </div>
          </div>
          <div className="flex items-center gap-1">
            <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Export</Button>
            <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => setShowCreateModal(true)}>New</Button>
          </div>
        </div>

        {/* Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          {filteredTemplates.map(template => (
            <Card key={template.id} className="hover:shadow cursor-pointer p-3" onClick={() => setSelectedTemplate(template)}>
              <div className="flex items-start justify-between mb-2">
                <div className="p-1.5 bg-primary-50 rounded"><Target className="w-4 h-4 text-primary-600" /></div>
                <Badge size="sm" variant={template.isActive ? 'success' : 'error'}>{template.isActive ? 'Active' : 'Inactive'}</Badge>
              </div>
              <h3 className="text-sm font-semibold text-secondary-900 dark:text-white mb-1">{template.name}</h3>
              <p className="text-xs text-secondary-500 line-clamp-2 mb-2">{template.kpiDescription}</p>
              <div className="flex items-center justify-between text-xs text-secondary-500">
                <span>{template.department?.name ?? 'General'}</span>
                <span className="font-medium">{template.weight ?? 0}%</span>
              </div>
              <div className="flex items-center gap-1 mt-2 pt-2 border-t text-[10px] text-secondary-500">
                <span>{template.indicatorNumber}</span>
                <span>•</span>
                <span>{template.kpiType}</span>
              </div>
            </Card>
          ))}
        </div>

        {filteredTemplates.length === 0 && (
          <EmptyState icon={<Target className="w-6 h-6" />} title="No templates" action={<Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => setShowCreateModal(true)}>Create</Button>} />
        )}

        <KPIFormModal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} />

        {/* Detail Modal */}
        <Modal isOpen={!!selectedTemplate && !showCreateModal} onClose={() => setSelectedTemplate(null)} title={selectedTemplate?.name} size="md">
          {selectedTemplate && (
            <div className="space-y-3">
              <div className="flex items-center gap-2">
                <Badge size="sm" variant={selectedTemplate.isActive ? 'success' : 'error'}>{selectedTemplate.isActive ? 'Active' : 'Inactive'}</Badge>
                <Badge size="sm" variant="default">{selectedTemplate.indicatorNumber}</Badge>
              </div>
              <div><p className="text-[10px] text-secondary-500">Description</p><p className="text-xs">{selectedTemplate.kpiDescription}</p></div>
              <div className="grid grid-cols-2 gap-2">
                <div><p className="text-[10px] text-secondary-500">Department</p><p className="text-xs font-medium">{selectedTemplate.department?.name ?? 'General'}</p></div>
                <div><p className="text-[10px] text-secondary-500">KPI Type</p><p className="text-xs font-medium">{selectedTemplate.kpiType}</p></div>
                <div><p className="text-[10px] text-secondary-500">KPA</p><p className="text-xs font-medium">{selectedTemplate.nationalKPA}</p></div>
                <div><p className="text-[10px] text-secondary-500">Weight</p><p className="text-xs font-medium">{selectedTemplate.weight}%</p></div>
              </div>
              <div className="flex items-center justify-end gap-2 pt-3 border-t">
                <Button variant="ghost" size="sm" onClick={() => setSelectedTemplate(null)}>Close</Button>
                <Button variant="outline" size="sm" icon={<Copy className="w-3.5 h-3.5" />}>Use</Button>
                <Button variant="primary" size="sm" icon={<Edit2 className="w-3.5 h-3.5" />}>Edit</Button>
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AppShell>
  );
}
