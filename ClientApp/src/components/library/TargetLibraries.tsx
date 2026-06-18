import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Archive,
  ArrowLeft,
  Copy,
  CheckSquare,
  Eye,
  Library,
  Plus,
  Save,
  Search,
  Square,
  Target,
  UserSquare2,
} from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card, EmptyState } from '../ui';
import { Modal } from '../common/Modal';
import { Checkbox, FormHero, FormPanel, FormRow, Input, Select, Textarea } from '../common/Form';
import { useApp } from '../../context/AppContext';
import {
  archiveIpmsTargetTemplate as archiveIpmsTargetTemplateApi,
  archiveOpmsTargetTemplate as archiveOpmsTargetTemplateApi,
  createIpmsTarget as createIpmsTargetApi,
  createIpmsTargetTemplate as createIpmsTargetTemplateApi,
  createOpmsTarget as createOpmsTargetApi,
  createOpmsTargetTemplate as createOpmsTargetTemplateApi,
  duplicateIpmsTargetTemplate as duplicateIpmsTargetTemplateApi,
  duplicateOpmsTargetTemplate as duplicateOpmsTargetTemplateApi,
  getIpmsTargetTemplate as getIpmsTargetTemplateApi,
  getIpmsTargetTemplates as getIpmsTargetTemplatesApi,
  getOpmsTargetTemplate as getOpmsTargetTemplateApi,
  getOpmsTargetTemplates as getOpmsTargetTemplatesApi,
  updateIpmsTargetTemplate as updateIpmsTargetTemplateApi,
  updateOpmsTargetTemplate as updateOpmsTargetTemplateApi,
} from '../../api/api';
import {
  mockBudgetSources,
  mockBudgetTypes,
  mockDepartments,
  mockStrategicGoals,
  mockStrategicObjectives,
  mockUnitsOfMeasure,
  targetUnitTypes,
} from '../../data/mockData';
import type {
  IpmsTargetTemplate,
  OpmsTargetTemplate,
  SaveIpmsTargetTemplatePayload,
  SaveOpmsTargetTemplatePayload,
  SaveIpmsTargetPayload,
  SaveOpmsTargetPayload,
  TargetUnitType,
  TemplateQuarterlyTarget,
} from '../../types';

type LibraryStatusFilter = 'all' | 'active' | 'archived';

interface TemplateRowAction {
  label: string;
  onClick: () => void;
  variant?: 'primary' | 'outline' | 'ghost' | 'error';
}

const quarterOptions = ['Q1', 'Q2', 'Q3', 'Q4'] as const;

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('en-ZA', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function DetailItem({ label, value }: { label: string; value?: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
      <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">{label}</p>
      <div className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{value ?? '-'}</div>
    </div>
  );
}

function LibraryFilters({
  search,
  onSearchChange,
  status,
  onStatusChange,
  areaValue,
  areaLabel,
  areaOptions,
  onAreaChange,
  departmentOrFunctionalArea,
  departmentOptions,
  onDepartmentOrFunctionalAreaChange,
  kpiType,
  kpiTypeOptions,
  onKpiTypeChange,
  targetUnitType,
  onTargetUnitTypeChange,
  version,
  versionOptions,
  onVersionChange,
}: {
  search: string;
  onSearchChange: (value: string) => void;
  status: LibraryStatusFilter;
  onStatusChange: (value: LibraryStatusFilter) => void;
  areaValue: string;
  areaLabel: string;
  areaOptions: string[];
  onAreaChange: (value: string) => void;
  departmentOrFunctionalArea: string;
  departmentOptions: string[];
  onDepartmentOrFunctionalAreaChange: (value: string) => void;
  kpiType: string;
  kpiTypeOptions: string[];
  onKpiTypeChange: (value: string) => void;
  targetUnitType: string;
  onTargetUnitTypeChange: (value: string) => void;
  version: string;
  versionOptions: number[];
  onVersionChange: (value: string) => void;
}) {
  return (
    <Card>
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-6">
        <Input
          label="Search"
          placeholder="Code, name, target..."
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          leftIcon={<Search className="h-3.5 w-3.5" />}
        />
        <Select
          label="Status"
          value={status}
          onChange={(event) => onStatusChange(event.target.value as LibraryStatusFilter)}
          options={[
            { value: 'all', label: 'All' },
            { value: 'active', label: 'Active' },
            { value: 'archived', label: 'Archived' },
          ]}
        />
        <Select
          label={areaLabel}
          value={areaValue}
          onChange={(event) => onAreaChange(event.target.value)}
          options={[{ value: '', label: `All ${areaLabel}` }, ...areaOptions.map(option => ({ value: option, label: option }))]}
        />
        <Select
          label="Department / Functional Area"
          value={departmentOrFunctionalArea}
          onChange={(event) => onDepartmentOrFunctionalAreaChange(event.target.value)}
          options={[{ value: '', label: 'All' }, ...departmentOptions.map(option => ({ value: option, label: option }))]}
        />
        <Select
          label="KPI Type"
          value={kpiType}
          onChange={(event) => onKpiTypeChange(event.target.value)}
          options={[{ value: '', label: 'All KPI Types' }, ...kpiTypeOptions.map(option => ({ value: option, label: option }))]}
        />
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Target Unit Type"
            value={targetUnitType}
            onChange={(event) => onTargetUnitTypeChange(event.target.value)}
            options={[{ value: '', label: 'All' }, ...targetUnitTypes.map(option => ({ value: option.value, label: option.label }))]}
          />
          <Select
            label="Version"
            value={version}
            onChange={(event) => onVersionChange(event.target.value)}
            options={[{ value: '', label: 'All' }, ...versionOptions.map(option => ({ value: String(option), label: `v${option}` }))]}
          />
        </div>
      </div>
    </Card>
  );
}

function SelectionToggle({
  checked,
  onToggle,
}: {
  checked: boolean;
  onToggle: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onToggle}
      className="inline-flex items-center justify-center rounded p-1 text-secondary-500 hover:bg-secondary-100 dark:hover:bg-secondary-800"
      aria-label={checked ? 'Deselect row' : 'Select row'}
    >
      {checked ? <CheckSquare className="h-4 w-4 text-primary-600" /> : <Square className="h-4 w-4" />}
    </button>
  );
}

function TemplateListTable<T>({
  items,
  columns,
  getRowId,
  selectedIds,
  onToggleRow,
  onToggleAll,
  actions,
  emptyMessage,
}: {
  items: T[];
  columns: Array<{
    id: string;
    header: string;
    render: (item: T) => React.ReactNode;
    className?: string;
  }>;
  getRowId: (item: T) => string;
  selectedIds: string[];
  onToggleRow: (id: string) => void;
  onToggleAll: () => void;
  actions: (item: T) => TemplateRowAction[];
  emptyMessage: string;
}) {
  const allSelected = items.length > 0 && items.every(item => selectedIds.includes(getRowId(item)));

  return (
    <Card padding="none">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b border-secondary-200 bg-secondary-50 dark:border-secondary-700 dark:bg-secondary-800">
              <th className="px-3 py-2 text-left">
                <SelectionToggle checked={allSelected} onToggle={onToggleAll} />
              </th>
              {columns.map(column => (
                <th key={column.id} className="px-3 py-2 text-left text-[10px] font-semibold uppercase tracking-wide text-secondary-600 dark:text-secondary-400">
                  {column.header}
                </th>
              ))}
              <th className="px-3 py-2 text-right text-[10px] font-semibold uppercase tracking-wide text-secondary-600 dark:text-secondary-400">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-secondary-200 dark:divide-secondary-700">
            {items.length === 0 ? (
              <tr>
                <td colSpan={columns.length + 2} className="px-3 py-8 text-center text-xs text-secondary-500">
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              items.map(item => {
                const id = getRowId(item);
                const rowActions = actions(item);
                return (
                  <tr key={id} className="hover:bg-secondary-50 dark:hover:bg-secondary-800/60">
                    <td className="px-3 py-2 align-top">
                      <SelectionToggle checked={selectedIds.includes(id)} onToggle={() => onToggleRow(id)} />
                    </td>
                    {columns.map(column => (
                      <td key={column.id} className={`px-3 py-2 text-xs text-secondary-700 dark:text-secondary-300 ${column.className ?? ''}`}>
                        {column.render(item)}
                      </td>
                    ))}
                    <td className="px-3 py-2">
                      <div className="flex flex-wrap justify-end gap-2">
                        {rowActions.map(action => (
                          <Button key={action.label} size="sm" variant={action.variant ?? 'outline'} onClick={action.onClick}>
                            {action.label}
                          </Button>
                        ))}
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </Card>
  );
}

function OpmsTemplateSelectionModal({
  isOpen,
  onClose,
  onSelect,
  onCreateMultiple,
}: {
  isOpen: boolean;
  onClose: () => void;
  onSelect: (template: OpmsTargetTemplate) => void;
  onCreateMultiple: (templates: OpmsTargetTemplate[]) => void;
}) {
  const [templates, setTemplates] = useState<OpmsTargetTemplate[]>([]);
  const [search, setSearch] = useState('');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const availableTemplates = useMemo(
    () =>
      templates.filter(template =>
        template.isActive &&
        !template.isArchived &&
        `${template.templateCode} ${template.templateName} ${template.targetName} ${template.kpiDescription}`
          .toLowerCase()
          .includes(search.toLowerCase()),
      ),
    [templates, search],
  );

  useEffect(() => {
    if (!isOpen) {
      setSelectedIds([]);
      return;
    }

    const loadTemplates = async () => {
      setIsLoading(true);
      const result = await getOpmsTargetTemplatesApi();
      if (result.success && result.data) {
        setTemplates(result.data);
      }
      setIsLoading(false);
    };

    void loadTemplates();
  }, [isOpen]);

  const toggleSelected = (id: string) => {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]);
  };

  const toggleAll = () => {
    setSelectedIds(prev => (
      prev.length === availableTemplates.length
        ? []
        : availableTemplates.map(template => template.id)
    ));
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Select OPMS Library Template" size="xl">
      <div className="space-y-4">
        <Input
          label="Search Template"
          placeholder="Search OPMS templates..."
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          leftIcon={<Search className="h-3.5 w-3.5" />}
        />
        <div className="flex items-center justify-between gap-3">
          <Badge variant="primary">{selectedIds.length} selected</Badge>
          <Button
            variant="primary"
            disabled={selectedIds.length === 0}
            onClick={() => {
              onCreateMultiple(availableTemplates.filter(template => selectedIds.includes(template.id)));
              onClose();
            }}
          >
            Create Selected OPMS Targets
          </Button>
        </div>
        <TemplateListTable
          items={availableTemplates}
          getRowId={(template) => template.id}
          selectedIds={selectedIds}
          onToggleRow={toggleSelected}
          onToggleAll={toggleAll}
          emptyMessage={isLoading ? 'Loading OPMS templates...' : 'No active OPMS templates'}
          columns={[
            {
              id: 'template',
              header: 'Template',
              render: (template) => (
                <div>
                  <p className="font-medium text-secondary-900 dark:text-white">{template.templateName}</p>
                  <p className="text-[11px] text-secondary-500">{template.templateCode}</p>
                </div>
              ),
            },
            {
              id: 'target',
              header: 'Target',
              render: (template) => (
                <div>
                  <p>{template.targetName}</p>
                  <p className="text-[11px] text-secondary-500">{template.kpiDescription}</p>
                </div>
              ),
            },
            {
              id: 'area',
              header: 'Functional Area',
              render: (template) => template.functionalArea ?? template.department?.name ?? '-',
            },
            {
              id: 'type',
              header: 'KPI Type',
              render: (template) => template.kpiType,
            },
            {
              id: 'version',
              header: 'Version',
              render: (template) => <Badge variant="default">v{template.version}</Badge>,
            },
          ]}
          actions={(template) => [
            {
              label: 'Use Single',
              variant: 'outline',
              onClick: () => {
                onSelect(template);
                onClose();
              },
            },
          ]}
        />
        {!isLoading && availableTemplates.length === 0 && (
          <EmptyState
            icon={<Library className="h-5 w-5" />}
            title="No active OPMS templates"
            description="Adjust the search or create a new OPMS template in the library."
          />
        )}
      </div>
    </Modal>
  );
}

function IpmsTemplateSelectionModal({
  isOpen,
  onClose,
  onSelect,
  onCreateMultiple,
}: {
  isOpen: boolean;
  onClose: () => void;
  onSelect: (template: IpmsTargetTemplate) => void;
  onCreateMultiple: (templates: IpmsTargetTemplate[]) => void;
}) {
  const [templates, setTemplates] = useState<IpmsTargetTemplate[]>([]);
  const [search, setSearch] = useState('');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const availableTemplates = useMemo(
    () =>
      templates.filter(template =>
        template.isActive &&
        !template.isArchived &&
        `${template.templateCode} ${template.templateName} ${template.targetName} ${template.kpiDescription}`
          .toLowerCase()
          .includes(search.toLowerCase()),
      ),
    [templates, search],
  );

  useEffect(() => {
    if (!isOpen) {
      setSelectedIds([]);
      return;
    }

    const loadTemplates = async () => {
      setIsLoading(true);
      const result = await getIpmsTargetTemplatesApi();
      if (result.success && result.data) {
        setTemplates(result.data);
      }
      setIsLoading(false);
    };

    void loadTemplates();
  }, [isOpen]);

  const toggleSelected = (id: string) => {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]);
  };

  const toggleAll = () => {
    setSelectedIds(prev => (
      prev.length === availableTemplates.length
        ? []
        : availableTemplates.map(template => template.id)
    ));
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Select IPMS Library Template" size="xl">
      <div className="space-y-4">
        <Input
          label="Search Template"
          placeholder="Search IPMS templates..."
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          leftIcon={<Search className="h-3.5 w-3.5" />}
        />
        <div className="flex items-center justify-between gap-3">
          <Badge variant="primary">{selectedIds.length} selected</Badge>
          <Button
            variant="primary"
            disabled={selectedIds.length === 0}
            onClick={() => {
              onCreateMultiple(availableTemplates.filter(template => selectedIds.includes(template.id)));
              onClose();
            }}
          >
            Create Selected IPMS Targets
          </Button>
        </div>
        <TemplateListTable
          items={availableTemplates}
          getRowId={(template) => template.id}
          selectedIds={selectedIds}
          onToggleRow={toggleSelected}
          onToggleAll={toggleAll}
          emptyMessage={isLoading ? 'Loading IPMS templates...' : 'No active IPMS templates'}
          columns={[
            {
              id: 'template',
              header: 'Template',
              render: (template) => (
                <div>
                  <p className="font-medium text-secondary-900 dark:text-white">{template.templateName}</p>
                  <p className="text-[11px] text-secondary-500">{template.templateCode}</p>
                </div>
              ),
            },
            {
              id: 'target',
              header: 'Target',
              render: (template) => (
                <div>
                  <p>{template.targetName}</p>
                  <p className="text-[11px] text-secondary-500">{template.kpiDescription}</p>
                </div>
              ),
            },
            {
              id: 'area',
              header: 'Performance Area',
              render: (template) => template.performanceArea,
            },
            {
              id: 'level',
              header: 'Employee Level',
              render: (template) => template.employeeLevel,
            },
            {
              id: 'version',
              header: 'Version',
              render: (template) => <Badge variant="default">v{template.version}</Badge>,
            },
          ]}
          actions={(template) => [
            {
              label: 'Use Single',
              variant: 'outline',
              onClick: () => {
                onSelect(template);
                onClose();
              },
            },
          ]}
        />
        {!isLoading && availableTemplates.length === 0 && (
          <EmptyState
            icon={<Library className="h-5 w-5" />}
            title="No active IPMS templates"
            description="Adjust the search or create a new IPMS template in the library."
          />
        )}
      </div>
    </Modal>
  );
}

function buildQuarterlyTargetsFromForm(quarterlyTargets: { quarter: string; target: string; description: string; budget: string }[]): TemplateQuarterlyTarget[] {
  return quarterlyTargets.map(item => ({
    quarter: item.quarter as TemplateQuarterlyTarget['quarter'],
    target: item.target ? Number(item.target) : undefined,
    description: item.description || undefined,
    budget: item.budget ? Number(item.budget) : undefined,
  }));
}

function buildOpmsTargetPayloadFromTemplate(template: OpmsTargetTemplate): SaveOpmsTargetPayload {
  return {
    indicatorNumber: template.indicatorNumber,
    targetName: template.targetName,
    kpiDescription: template.kpiDescription,
    nationalKpa: template.nationalKPA,
    municipalKpa: template.municipalKPA,
    performanceObjective: template.performanceObjective,
    departmentId: null,
    unitId: null,
    assignedUserId: null,
    sourceTemplateId: template.id,
    sourceTemplateVersion: template.version,
    baseline: template.baseline,
    annualTarget: template.annualTarget,
    annualTargetDescription: template.annualTargetDescription,
    budgetSourceId: template.budgetSource?.id ? Number(template.budgetSource.id) : null,
    budgetTypeId: template.budgetType?.id ? Number(template.budgetType.id) : null,
    unitOfMeasureId: template.unitOfMeasure?.id ? Number(template.unitOfMeasure.id) : null,
    weight: template.weight,
    kpiType: template.kpiType,
    indicatorType: template.indicatorType,
    functionalArea: template.functionalArea ?? null,
    standardClassification: template.standardClassification ?? null,
    idpReference: template.idpReference ?? null,
    internalReference: template.internalReference ?? null,
    fmsLink: template.fmsLink ?? null,
    isRevised: false,
    isWithdrawn: false,
    reasonForWithdrawal: null,
    targetUnitType: template.targetUnitType,
  };
}

function buildIpmsTargetPayloadFromTemplate(template: IpmsTargetTemplate): SaveIpmsTargetPayload {
  return {
    indicatorNumber: template.templateCode,
    targetName: template.targetName,
    kpiDescription: template.kpiDescription,
    nationalKpa: '',
    municipalKpa: '',
    performanceObjective: '',
    departmentId: null,
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
  };
}

function buildOpmsTemplatePayload(form: ReturnType<typeof useOpmsTemplateForm>['form']): SaveOpmsTargetTemplatePayload {
  return {
    templateCode: form.templateCode,
    templateName: form.templateName,
    indicatorNumber: form.indicatorNumber,
    targetName: form.targetName,
    kpiDescription: form.kpiDescription,
    baseline: Number(form.baseline || 0),
    annualTarget: Number(form.annualTarget || 0),
    annualTargetDescription: form.annualTargetDescription || null,
    targetUnitType: form.targetUnitType,
    unitOfMeasure: mockUnitsOfMeasure.find(item => item.id === form.unitOfMeasureId)?.name ?? null,
    nationalKpa: form.nationalKPA || null,
    municipalKpa: form.municipalKPA || null,
    strategicGoal: mockStrategicGoals.find(item => item.id === form.strategicGoalId)?.name ?? null,
    strategicObjective: mockStrategicObjectives.find(item => item.id === form.strategicObjectiveId)?.name ?? null,
    performanceObjective: form.performanceObjective || null,
    outcome: form.outcome || null,
    output: form.output || null,
    priorityIssue: form.priorityIssue || null,
    budgetSource: mockBudgetSources.find(item => item.id === form.budgetSourceId)?.name ?? null,
    budgetType: mockBudgetTypes.find(item => item.id === form.budgetTypeId)?.name ?? null,
    weight: Number(form.weight || 0),
    kpiType: form.kpiType || null,
    indicatorType: form.indicatorType || null,
    functionalArea: form.functionalArea || null,
    standardClassification: form.standardClassification || null,
    idpReference: form.idpReference || null,
    internalReference: form.internalReference || null,
    fmsLink: form.fmsLink || null,
    defaultQuarterlyTargetsJson: JSON.stringify(buildQuarterlyTargetsFromForm(form.quarterlyTargets)),
    defaultBudgetInformation: form.defaultBudgetInformation || null,
    defaultPoeRequirements: form.defaultPoeRequirements || null,
    isActive: form.isActive,
  };
}

function buildIpmsTemplatePayload(form: ReturnType<typeof useIpmsTemplateForm>['form']): SaveIpmsTargetTemplatePayload {
  return {
    templateCode: form.templateCode,
    templateName: form.templateName,
    targetName: form.targetName,
    kpiDescription: form.kpiDescription,
    performanceArea: form.performanceArea || null,
    employeeLevel: form.employeeLevel || null,
    jobGrade: form.jobGrade || null,
    targetUnitType: form.targetUnitType,
    unitOfMeasure: mockUnitsOfMeasure.find(item => item.id === form.unitOfMeasureId)?.name ?? null,
    annualTarget: Number(form.annualTarget || 0),
    annualTargetDescription: form.annualTargetDescription || null,
    weight: Number(form.weight || 0),
    defaultRatingMethod: form.defaultRatingMethod || null,
    defaultScoreScale: form.defaultScoreScale || null,
    defaultPoeRequirements: form.defaultPoeRequirements || null,
    defaultTaskTemplatesJson: JSON.stringify(form.defaultTaskTemplates.split('\n').map(item => item.trim()).filter(Boolean)),
    linkedOpmsTargetRequired: form.linkedOpmsTargetRequired,
    functionalArea: form.functionalArea || null,
    isActive: form.isActive,
  };
}

function useOpmsTemplateForm(template?: OpmsTargetTemplate | null) {
  const [form, setForm] = useState({
    templateCode: '',
    templateName: '',
    departmentId: '',
    indicatorNumber: '',
    targetName: '',
    kpiDescription: '',
    baseline: '0',
    annualTarget: '0',
    annualTargetDescription: '',
    targetUnitType: 'absolute_count' as TargetUnitType,
    unitOfMeasureId: '',
    nationalKPA: '',
    municipalKPA: '',
    strategicGoalId: '',
    strategicObjectiveId: '',
    performanceObjective: '',
    outcome: '',
    output: '',
    priorityIssue: '',
    budgetSourceId: '',
    budgetTypeId: '',
    weight: '0',
    kpiType: '',
    indicatorType: '',
    functionalArea: '',
    standardClassification: '',
    idpReference: '',
    internalReference: '',
    fmsLink: '',
    defaultBudgetInformation: '',
    defaultPoeRequirements: '',
    isActive: true,
    version: '1',
    createdBy: '',
    createdDate: '',
    quarterlyTargets: quarterOptions.map(quarter => ({ quarter, target: '', description: '', budget: '' })),
  });

  useEffect(() => {
    if (!template) {
      setForm({
        templateCode: '',
        templateName: '',
        departmentId: '',
        indicatorNumber: '',
        targetName: '',
        kpiDescription: '',
        baseline: '0',
        annualTarget: '0',
        annualTargetDescription: '',
        targetUnitType: 'absolute_count',
        unitOfMeasureId: mockUnitsOfMeasure[0]?.id ?? '',
        nationalKPA: '',
        municipalKPA: '',
        strategicGoalId: '',
        strategicObjectiveId: '',
        performanceObjective: '',
        outcome: '',
        output: '',
        priorityIssue: '',
        budgetSourceId: '',
        budgetTypeId: '',
        weight: '0',
        kpiType: '',
        indicatorType: '',
        functionalArea: '',
        standardClassification: '',
        idpReference: '',
        internalReference: '',
        fmsLink: '',
        defaultBudgetInformation: '',
        defaultPoeRequirements: '',
        isActive: true,
        version: '1',
        createdBy: 'System Administrator',
        createdDate: new Date().toISOString().slice(0, 10),
        quarterlyTargets: quarterOptions.map(quarter => ({ quarter, target: '', description: '', budget: '' })),
      });
      return;
    }

    setForm({
      templateCode: template.templateCode,
      templateName: template.templateName,
      departmentId: template.department?.id ?? '',
      indicatorNumber: template.indicatorNumber,
      targetName: template.targetName,
      kpiDescription: template.kpiDescription,
      baseline: String(template.baseline),
      annualTarget: String(template.annualTarget),
      annualTargetDescription: template.annualTargetDescription,
      targetUnitType: template.targetUnitType,
      unitOfMeasureId: template.unitOfMeasure.id,
      nationalKPA: template.nationalKPA,
      municipalKPA: template.municipalKPA,
      strategicGoalId: template.strategicGoal?.id ?? '',
      strategicObjectiveId: template.strategicObjective?.id ?? '',
      performanceObjective: template.performanceObjective,
      outcome: template.outcome ?? '',
      output: template.output ?? '',
      priorityIssue: template.priorityIssue ?? '',
      budgetSourceId: template.budgetSource?.id ?? '',
      budgetTypeId: template.budgetType?.id ?? '',
      weight: String(template.weight),
      kpiType: template.kpiType,
      indicatorType: template.indicatorType,
      functionalArea: template.functionalArea ?? '',
      standardClassification: template.standardClassification ?? '',
      idpReference: template.idpReference ?? '',
      internalReference: template.internalReference ?? '',
      fmsLink: template.fmsLink ?? '',
      defaultBudgetInformation: template.defaultBudgetInformation ?? '',
      defaultPoeRequirements: template.defaultPoeRequirements ?? '',
      isActive: template.isActive,
      version: String(template.version),
      createdBy: template.createdBy,
      createdDate: template.createdDate.slice(0, 10),
      quarterlyTargets: quarterOptions.map(quarter => {
        const current = template.defaultQuarterlyTargets.find(item => item.quarter === quarter);
        return {
          quarter,
          target: current?.target ? String(current.target) : '',
          description: current?.description ?? '',
          budget: current?.budget ? String(current.budget) : '',
        };
      }),
    });
  }, [template]);

  return { form, setForm };
}

function useIpmsTemplateForm(template?: IpmsTargetTemplate | null) {
  const [form, setForm] = useState({
    templateCode: '',
    templateName: '',
    departmentId: '',
    targetName: '',
    kpiDescription: '',
    performanceArea: '',
    employeeLevel: '',
    jobGrade: '',
    targetUnitType: 'percentage' as TargetUnitType,
    unitOfMeasureId: '',
    annualTarget: '0',
    annualTargetDescription: '',
    weight: '0',
    defaultRatingMethod: '',
    defaultScoreScale: '',
    defaultPoeRequirements: '',
    defaultTaskTemplates: '',
    linkedOpmsTargetRequired: false,
    functionalArea: '',
    isActive: true,
    version: '1',
    createdBy: '',
    createdDate: '',
  });

  useEffect(() => {
    if (!template) {
      setForm({
        templateCode: '',
        templateName: '',
        departmentId: '',
        targetName: '',
        kpiDescription: '',
        performanceArea: '',
        employeeLevel: '',
        jobGrade: '',
        targetUnitType: 'percentage',
        unitOfMeasureId: mockUnitsOfMeasure[0]?.id ?? '',
        annualTarget: '0',
        annualTargetDescription: '',
        weight: '0',
        defaultRatingMethod: '',
        defaultScoreScale: '',
        defaultPoeRequirements: '',
        defaultTaskTemplates: '',
        linkedOpmsTargetRequired: false,
        functionalArea: '',
        isActive: true,
        version: '1',
        createdBy: 'System Administrator',
        createdDate: new Date().toISOString().slice(0, 10),
      });
      return;
    }

    setForm({
      templateCode: template.templateCode,
      templateName: template.templateName,
      departmentId: template.department?.id ?? '',
      targetName: template.targetName,
      kpiDescription: template.kpiDescription,
      performanceArea: template.performanceArea,
      employeeLevel: template.employeeLevel,
      jobGrade: template.jobGrade,
      targetUnitType: template.targetUnitType,
      unitOfMeasureId: template.unitOfMeasure.id,
      annualTarget: String(template.annualTarget),
      annualTargetDescription: template.annualTargetDescription,
      weight: String(template.weight),
      defaultRatingMethod: template.defaultRatingMethod ?? '',
      defaultScoreScale: template.defaultScoreScale ?? '',
      defaultPoeRequirements: template.defaultPoeRequirements ?? '',
      defaultTaskTemplates: template.defaultTaskTemplates.join('\n'),
      linkedOpmsTargetRequired: template.linkedOpmsTargetRequired,
      functionalArea: template.functionalArea ?? '',
      isActive: template.isActive,
      version: String(template.version),
      createdBy: template.createdBy,
      createdDate: template.createdDate.slice(0, 10),
    });
  }, [template]);

  return { form, setForm };
}

export function OPMSTargetLibraryList() {
  const { pushToast, setCurrentPath } = useApp();
  const [opmsTargetTemplates, setOpmsTargetTemplates] = useState<OpmsTargetTemplate[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<LibraryStatusFilter>('all');
  const [kpa, setKpa] = useState('');
  const [departmentOrFunctionalArea, setDepartmentOrFunctionalArea] = useState('');
  const [kpiType, setKpiType] = useState('');
  const [targetUnitType, setTargetUnitType] = useState('');
  const [version, setVersion] = useState('');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  const loadTemplates = useCallback(async () => {
    setIsLoading(true);
    const result = await getOpmsTargetTemplatesApi();
    if (result.success && result.data) {
      setOpmsTargetTemplates(result.data);
    } else {
      pushToast('error', result.message ?? 'Failed to load OPMS target library');
    }
    setIsLoading(false);
  }, [pushToast]);

  useEffect(() => {
    void loadTemplates();
  }, [loadTemplates]);

  const filteredTemplates = useMemo(
    () =>
      opmsTargetTemplates.filter(template => {
        const matchesSearch = `${template.templateCode} ${template.templateName} ${template.targetName} ${template.kpiDescription}`
          .toLowerCase()
          .includes(search.toLowerCase());
        const matchesStatus =
          status === 'all' ||
          (status === 'active' && template.isActive && !template.isArchived) ||
          (status === 'archived' && !!template.isArchived);
        const matchesKpa = !kpa || template.nationalKPA === kpa || template.municipalKPA === kpa;
        const matchesDepartmentOrArea =
          !departmentOrFunctionalArea ||
          template.department?.name === departmentOrFunctionalArea ||
          template.functionalArea === departmentOrFunctionalArea;
        const matchesKpiType = !kpiType || template.kpiType === kpiType;
        const matchesUnitType = !targetUnitType || template.targetUnitType === targetUnitType;
        const matchesVersion = !version || String(template.version) === version;
        return matchesSearch && matchesStatus && matchesKpa && matchesDepartmentOrArea && matchesKpiType && matchesUnitType && matchesVersion;
      }),
    [departmentOrFunctionalArea, kpa, kpiType, opmsTargetTemplates, search, status, targetUnitType, version],
  );

  const kpaOptions = Array.from(new Set(opmsTargetTemplates.flatMap(template => [template.nationalKPA, template.municipalKPA]).filter(Boolean))).sort();
  const departmentOptions = Array.from(new Set(opmsTargetTemplates.flatMap(template => [template.department?.name, template.functionalArea]).filter(Boolean) as string[])).sort();
  const kpiTypeOptions = Array.from(new Set(opmsTargetTemplates.map(template => template.kpiType).filter(Boolean))).sort();
  const versionOptions = Array.from(new Set(opmsTargetTemplates.map(template => template.version))).sort((a, b) => b - a);
  const toggleSelected = (id: string) => {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]);
  };
  const toggleAll = () => {
    setSelectedIds(prev => (
      prev.length === filteredTemplates.length
        ? []
        : filteredTemplates.map(template => template.id)
    ));
  };

  const createSelectedTargets = async (templates: OpmsTargetTemplate[]) => {
    const results = await Promise.all(templates.map(template => createOpmsTargetApi(buildOpmsTargetPayloadFromTemplate(template))));
    const successCount = results.filter(result => result.success).length;
    const failureCount = results.length - successCount;
    setSelectedIds([]);

    if (successCount > 0) {
      pushToast('success', `${successCount} OPMS target${successCount === 1 ? '' : 's'} created from library`);
    }
    if (failureCount > 0) {
      pushToast('error', `${failureCount} OPMS target${failureCount === 1 ? '' : 's'} failed to create`);
    }
  };

  return (
    <AppShell title="OPMS Target Library" subtitle="Reusable generic OPMS target templates used to create live OPMS targets">
      <div className="space-y-6">
        <div className="flex items-center justify-between gap-3">
          <Badge variant="primary">{filteredTemplates.length} templates</Badge>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              disabled={selectedIds.length === 0}
              onClick={() => { void createSelectedTargets(filteredTemplates.filter(template => selectedIds.includes(template.id))); }}
            >
              Create Selected OPMS Targets
            </Button>
            <Button variant="primary" icon={<Plus className="h-4 w-4" />} onClick={() => setCurrentPath('/opms/library/new')}>
              Create Template
            </Button>
          </div>
        </div>

        <LibraryFilters
          search={search}
          onSearchChange={setSearch}
          status={status}
          onStatusChange={setStatus}
          areaValue={kpa}
          areaLabel="KPA"
          areaOptions={kpaOptions}
          onAreaChange={setKpa}
          departmentOrFunctionalArea={departmentOrFunctionalArea}
          departmentOptions={departmentOptions}
          onDepartmentOrFunctionalAreaChange={setDepartmentOrFunctionalArea}
          kpiType={kpiType}
          kpiTypeOptions={kpiTypeOptions}
          onKpiTypeChange={setKpiType}
          targetUnitType={targetUnitType}
          onTargetUnitTypeChange={setTargetUnitType}
          version={version}
          versionOptions={versionOptions}
          onVersionChange={setVersion}
        />

        <TemplateListTable
          items={filteredTemplates}
          getRowId={(template) => template.id}
          selectedIds={selectedIds}
          onToggleRow={toggleSelected}
          onToggleAll={toggleAll}
          emptyMessage={isLoading ? 'Loading OPMS templates...' : 'No OPMS templates found'}
          columns={[
            {
              id: 'template',
              header: 'Template',
              render: (template) => (
                <div>
                  <p className="font-medium text-secondary-900 dark:text-white">{template.templateName}</p>
                  <p className="text-[11px] text-secondary-500">{template.templateCode}</p>
                </div>
              ),
            },
            {
              id: 'target',
              header: 'Target',
              render: (template) => (
                <div>
                  <p>{template.targetName}</p>
                  <p className="text-[11px] text-secondary-500">{template.kpiDescription}</p>
                </div>
              ),
            },
            {
              id: 'kpa',
              header: 'KPA',
              render: (template) => (
                <div>
                  <p>{template.nationalKPA}</p>
                  <p className="text-[11px] text-secondary-500">{template.municipalKPA}</p>
                </div>
              ),
            },
            {
              id: 'area',
              header: 'Functional Area',
              render: (template) => template.functionalArea ?? template.department?.name ?? '-',
            },
            {
              id: 'type',
              header: 'KPI Type',
              render: (template) => template.kpiType,
            },
            {
              id: 'unit',
              header: 'Target Unit Type',
              render: (template) => template.targetUnitType,
            },
            {
              id: 'version',
              header: 'Version',
              render: (template) => (
                <div className="flex items-center gap-2">
                  <Badge variant={template.isArchived ? 'error' : 'success'}>{template.isArchived ? 'Archived' : 'Active'}</Badge>
                  <Badge variant="default">v{template.version}</Badge>
                </div>
              ),
            },
          ]}
          actions={(template) => [
            { label: 'View', onClick: () => setCurrentPath(`/opms/library/${template.id}`) },
            { label: 'Edit', onClick: () => setCurrentPath(`/opms/library/${template.id}/edit`) },
            {
              label: 'Duplicate',
              onClick: () => {
                void (async () => {
                  const result = await duplicateOpmsTargetTemplateApi(template.id);
                  if (result.success) {
                    pushToast('success', 'OPMS template duplicated');
                    await loadTemplates();
                  } else {
                    pushToast('error', result.message ?? 'Failed to duplicate OPMS template');
                  }
                })();
              },
            },
            {
              label: 'Archive/Delete',
              variant: 'error',
              onClick: () => {
                void (async () => {
                  const result = await archiveOpmsTargetTemplateApi(template.id);
                  if (result.success) {
                    pushToast('success', 'OPMS template archived');
                    await loadTemplates();
                  } else {
                    pushToast('error', result.message ?? 'Failed to archive OPMS template');
                  }
                })();
              },
            },
            {
              label: 'Create OPMS Target',
              variant: 'primary',
              onClick: () => {
                localStorage.setItem('pending_opms_template_id', template.id);
                setCurrentPath('/opms/targets/new');
              },
            },
          ]}
        />

        {!isLoading && filteredTemplates.length === 0 && (
          <EmptyState
            icon={<Library className="h-6 w-6" />}
            title="No OPMS templates found"
            description="Try changing the filters or create a new OPMS library template."
            action={<Button variant="primary" onClick={() => setCurrentPath('/opms/library/new')}>Create Template</Button>}
          />
        )}
      </div>
    </AppShell>
  );
}

export function OPMSTargetLibraryDetail({ templateId }: { templateId: string }) {
  const { pushToast, setCurrentPath } = useApp();
  const [template, setTemplate] = useState<OpmsTargetTemplate | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadTemplate = async () => {
      setIsLoading(true);
      const result = await getOpmsTargetTemplateApi(templateId);
      if (result.success && result.data) {
        setTemplate(result.data);
      } else {
        setTemplate(null);
      }
      setIsLoading(false);
    };

    void loadTemplate();
  }, [templateId]);

  if (isLoading) {
    return (
      <AppShell title="OPMS Target Library" subtitle="Template detail">
        <Card>
          <p className="text-sm text-secondary-500">Loading template...</p>
        </Card>
      </AppShell>
    );
  }

  if (!template) {
    return (
      <AppShell title="OPMS Target Library" subtitle="Template detail">
        <EmptyState
          icon={<Library className="h-6 w-6" />}
          title="Template not found"
          action={<Button variant="primary" onClick={() => setCurrentPath('/opms/library')}>Back to Library</Button>}
        />
      </AppShell>
    );
  }

  return (
    <AppShell title={template.templateName} subtitle="OPMS library template detail">
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Button variant="ghost" icon={<ArrowLeft className="h-4 w-4" />} onClick={() => setCurrentPath('/opms/library')}>
              Back
            </Button>
            <Badge variant={template.isArchived ? 'error' : 'success'}>{template.isArchived ? 'Archived' : 'Active'}</Badge>
            <Badge variant="default">v{template.version}</Badge>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button
              variant="outline"
              icon={<Copy className="h-4 w-4" />}
              onClick={() => {
                void (async () => {
                  const result = await duplicateOpmsTargetTemplateApi(template.id);
                  if (result.success && result.data) {
                    pushToast('success', 'OPMS template duplicated');
                    setCurrentPath(`/opms/library/${result.data.id}`);
                  } else {
                    pushToast('error', result.message ?? 'Failed to duplicate OPMS template');
                  }
                })();
              }}
            >
              Duplicate
            </Button>
            <Button
              variant="outline"
              icon={<Archive className="h-4 w-4" />}
              onClick={() => {
                void (async () => {
                  const result = await archiveOpmsTargetTemplateApi(template.id);
                  if (result.success) {
                    pushToast('success', 'OPMS template archived');
                    setCurrentPath('/opms/library');
                  } else {
                    pushToast('error', result.message ?? 'Failed to archive OPMS template');
                  }
                })();
              }}
            >
              Archive/Delete
            </Button>
            <Button
              variant="primary"
              icon={<Target className="h-4 w-4" />}
              onClick={() => {
                localStorage.setItem('pending_opms_template_id', template.id);
                setCurrentPath('/opms/targets/new');
              }}
            >
              Create OPMS Target from Template
            </Button>
          </div>
        </div>

        <FormHero
          eyebrow="OPMS Target Library"
          title={template.templateName}
          description="This template is a reusable generic definition. Live OPMS targets copy from it and then capture period, department, unit, wards, assignee, and workflow-specific fields."
          badges={
            <>
              <Badge variant="primary">{template.templateCode}</Badge>
              <Badge variant="info">{template.kpiType}</Badge>
            </>
          }
        />

        <div className="grid gap-4 xl:grid-cols-3">
          <DetailItem label="Indicator Number" value={template.indicatorNumber} />
          <DetailItem label="Target Name" value={template.targetName} />
          <DetailItem label="Target Unit Type" value={template.targetUnitType} />
          <DetailItem label="Unit Of Measure" value={template.unitOfMeasure.name} />
          <DetailItem label="National KPA" value={template.nationalKPA} />
          <DetailItem label="Municipal KPA" value={template.municipalKPA} />
          <DetailItem label="Strategic Goal" value={template.strategicGoal?.name} />
          <DetailItem label="Strategic Objective" value={template.strategicObjective?.name} />
          <DetailItem label="Performance Objective" value={template.performanceObjective} />
          <DetailItem label="Outcome" value={template.outcome} />
          <DetailItem label="Output" value={template.output} />
          <DetailItem label="Priority Issue" value={template.priorityIssue} />
          <DetailItem label="Budget Source" value={template.budgetSource?.name} />
          <DetailItem label="Budget Type" value={template.budgetType?.name} />
          <DetailItem label="Weight" value={`${template.weight}%`} />
          <DetailItem label="KPI Type" value={template.kpiType} />
          <DetailItem label="Indicator Type" value={template.indicatorType} />
          <DetailItem label="Functional Area" value={template.functionalArea} />
          <DetailItem label="Standard Classification" value={template.standardClassification} />
          <DetailItem label="IDP Reference" value={template.idpReference} />
          <DetailItem label="Internal Reference" value={template.internalReference} />
          <DetailItem label="FMS Link" value={template.fmsLink} />
          <DetailItem label="Created By" value={template.createdBy} />
          <DetailItem label="Created Date" value={formatDate(template.createdDate)} />
        </div>

        <Card>
          <h3 className="text-sm font-semibold text-secondary-900 dark:text-white">KPI Description</h3>
          <p className="mt-2 text-sm text-secondary-600 dark:text-secondary-400">{template.kpiDescription}</p>
        </Card>

        <Card>
          <h3 className="text-sm font-semibold text-secondary-900 dark:text-white">Default Quarterly Targets</h3>
          <div className="mt-3 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            {template.defaultQuarterlyTargets.map(target => (
              <DetailItem
                key={target.quarter}
                label={target.quarter}
                value={
                  <div className="space-y-1">
                    <div>{target.target ?? '-'}</div>
                    <div className="text-xs font-normal text-secondary-500">{target.description ?? 'No description'}</div>
                    <div className="text-xs font-normal text-secondary-500">{target.budget ? `Budget: ${target.budget.toLocaleString()}` : 'No budget set'}</div>
                  </div>
                }
              />
            ))}
          </div>
        </Card>

        <div className="grid gap-4 xl:grid-cols-2">
          <DetailItem label="Default Budget Information" value={template.defaultBudgetInformation} />
          <DetailItem label="Default POE Requirements" value={template.defaultPoeRequirements} />
        </div>
      </div>
    </AppShell>
  );
}

export function OPMSTargetTemplateFormPage({ templateId }: { templateId?: string }) {
  const { pushToast, setCurrentPath } = useApp();
  const [template, setTemplate] = useState<OpmsTargetTemplate | null>(null);
  const [isLoading, setIsLoading] = useState(!!templateId);
  const { form, setForm } = useOpmsTemplateForm(template);

  useEffect(() => {
    if (!templateId) {
      setTemplate(null);
      setIsLoading(false);
      return;
    }

    const loadTemplate = async () => {
      setIsLoading(true);
      const result = await getOpmsTargetTemplateApi(templateId);
      if (result.success && result.data) {
        setTemplate(result.data);
      } else {
        pushToast('error', result.message ?? 'Failed to load OPMS template');
      }
      setIsLoading(false);
    };

    void loadTemplate();
  }, [templateId, pushToast]);

  const handleSave = async () => {
    const payload = buildOpmsTemplatePayload(form);

    if (template) {
      const result = await updateOpmsTargetTemplateApi(template.id, payload);
      if (result.success && result.data) {
        pushToast('success', 'OPMS template updated');
        setCurrentPath(`/opms/library/${result.data.id}`);
      } else {
        pushToast('error', result.message ?? 'Failed to update OPMS template');
      }
      return;
    }

    const result = await createOpmsTargetTemplateApi(payload);
    if (result.success && result.data) {
      pushToast('success', 'OPMS template created');
      setCurrentPath(`/opms/library/${result.data.id}`);
    } else {
      pushToast('error', result.message ?? 'Failed to create OPMS template');
    }
  };

  if (isLoading) {
    return (
      <AppShell title={templateId ? 'Edit OPMS Target Template' : 'Create OPMS Target Template'} subtitle="Reusable generic OPMS target definition">
        <Card>
          <p className="text-sm text-secondary-500">Loading template...</p>
        </Card>
      </AppShell>
    );
  }

  return (
    <AppShell title={template ? 'Edit OPMS Target Template' : 'Create OPMS Target Template'} subtitle="Reusable generic OPMS target definition">
      <div className="space-y-6">
        <FormHero
          eyebrow="OPMS Target Library"
          title={template ? 'Update OPMS target template' : 'Create reusable OPMS target template'}
          description="Capture a reusable generic OPMS definition. Live OPMS targets will inherit these values and then add period, department, unit, wards, assignee, and other live workflow fields."
          badges={<Badge variant="default">{template ? 'Edit Mode' : 'New Template'}</Badge>}
        />

        <div className="grid gap-4 xl:grid-cols-[1.15fr_0.85fr]">
          <FormPanel title="Template Identity" description="Basic definition and classification for the reusable template." icon={<Library className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Template Code" required value={form.templateCode} onChange={(event) => setForm(prev => ({ ...prev, templateCode: event.target.value }))} />
              <Input label="Template Name" required value={form.templateName} onChange={(event) => setForm(prev => ({ ...prev, templateName: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Department" value={form.departmentId} onChange={(event) => setForm(prev => ({ ...prev, departmentId: event.target.value }))} options={[{ value: '', label: 'General Template' }, ...mockDepartments.map(item => ({ value: item.id, label: item.name }))]} />
              <Input label="Functional Area" value={form.functionalArea} onChange={(event) => setForm(prev => ({ ...prev, functionalArea: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Input label="Indicator Number" required value={form.indicatorNumber} onChange={(event) => setForm(prev => ({ ...prev, indicatorNumber: event.target.value }))} />
              <Input label="Target Name" required value={form.targetName} onChange={(event) => setForm(prev => ({ ...prev, targetName: event.target.value }))} />
            </FormRow>
            <Textarea label="KPI Description" required rows={4} value={form.kpiDescription} onChange={(event) => setForm(prev => ({ ...prev, kpiDescription: event.target.value }))} />
          </FormPanel>

          <FormPanel title="Lifecycle" description="Versioning and default template status." icon={<Eye className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Version" required type="number" value={form.version} onChange={(event) => setForm(prev => ({ ...prev, version: event.target.value }))} />
              <Input label="Created By" required value={form.createdBy} onChange={(event) => setForm(prev => ({ ...prev, createdBy: event.target.value }))} />
            </FormRow>
            <Input label="Created Date" required type="date" value={form.createdDate} onChange={(event) => setForm(prev => ({ ...prev, createdDate: event.target.value }))} />
            <Checkbox label="Is Active" checked={form.isActive} onChange={(event) => setForm(prev => ({ ...prev, isActive: event.target.checked }))} />
          </FormPanel>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Performance Alignment" description="Map the reusable target to strategy and KPA structures." icon={<Target className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="National KPA" required value={form.nationalKPA} onChange={(event) => setForm(prev => ({ ...prev, nationalKPA: event.target.value }))} />
              <Input label="Municipal KPA" required value={form.municipalKPA} onChange={(event) => setForm(prev => ({ ...prev, municipalKPA: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Strategic Goal" value={form.strategicGoalId} onChange={(event) => setForm(prev => ({ ...prev, strategicGoalId: event.target.value }))} options={[{ value: '', label: 'Select Goal' }, ...mockStrategicGoals.map(item => ({ value: item.id, label: item.name }))]} />
              <Select label="Strategic Objective" value={form.strategicObjectiveId} onChange={(event) => setForm(prev => ({ ...prev, strategicObjectiveId: event.target.value }))} options={[{ value: '', label: 'Select Objective' }, ...mockStrategicObjectives.map(item => ({ value: item.id, label: item.name }))]} />
            </FormRow>
            <Input label="Performance Objective" required value={form.performanceObjective} onChange={(event) => setForm(prev => ({ ...prev, performanceObjective: event.target.value }))} />
            <FormRow cols={3}>
              <Input label="Outcome" value={form.outcome} onChange={(event) => setForm(prev => ({ ...prev, outcome: event.target.value }))} />
              <Input label="Output" value={form.output} onChange={(event) => setForm(prev => ({ ...prev, output: event.target.value }))} />
              <Input label="Priority Issue" value={form.priorityIssue} onChange={(event) => setForm(prev => ({ ...prev, priorityIssue: event.target.value }))} />
            </FormRow>
          </FormPanel>

          <FormPanel title="Measurement Defaults" description="Define target values, units, and scoring defaults copied into live targets." icon={<Target className="h-5 w-5" />}>
            <FormRow cols={3}>
              <Input label="Baseline" required type="number" value={form.baseline} onChange={(event) => setForm(prev => ({ ...prev, baseline: event.target.value }))} />
              <Input label="Annual Target" required type="number" value={form.annualTarget} onChange={(event) => setForm(prev => ({ ...prev, annualTarget: event.target.value }))} />
              <Input label="Weight" required type="number" value={form.weight} onChange={(event) => setForm(prev => ({ ...prev, weight: event.target.value }))} />
            </FormRow>
            <Textarea label="Annual Target Description" rows={3} value={form.annualTargetDescription} onChange={(event) => setForm(prev => ({ ...prev, annualTargetDescription: event.target.value }))} />
            <FormRow cols={4}>
              <Select label="Target Unit Type" required value={form.targetUnitType} onChange={(event) => setForm(prev => ({ ...prev, targetUnitType: event.target.value as TargetUnitType }))} options={targetUnitTypes} />
              <Select label="Unit of Measure" required value={form.unitOfMeasureId} onChange={(event) => setForm(prev => ({ ...prev, unitOfMeasureId: event.target.value }))} options={mockUnitsOfMeasure.map(item => ({ value: item.id, label: item.name }))} />
              <Input label="KPI Type" required value={form.kpiType} onChange={(event) => setForm(prev => ({ ...prev, kpiType: event.target.value }))} />
              <Input label="Indicator Type" required value={form.indicatorType} onChange={(event) => setForm(prev => ({ ...prev, indicatorType: event.target.value }))} />
            </FormRow>
          </FormPanel>
        </div>

        <FormPanel title="Default Quarterly Targets" description="Set the default quarterly structure to pre-populate live OPMS targets." icon={<Target className="h-5 w-5" />}>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {form.quarterlyTargets.map((quarter, index) => (
              <Card key={quarter.quarter} className="border border-secondary-200 dark:border-secondary-700" padding="md">
                <div className="space-y-2">
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">{quarter.quarter}</h4>
                  <Input
                    label="Default Target"
                    type="number"
                    value={quarter.target}
                    onChange={(event) =>
                      setForm(prev => ({
                        ...prev,
                        quarterlyTargets: prev.quarterlyTargets.map((item, itemIndex) => itemIndex === index ? { ...item, target: event.target.value } : item),
                      }))
                    }
                  />
                  <Input
                    label="Default Budget"
                    type="number"
                    value={quarter.budget}
                    onChange={(event) =>
                      setForm(prev => ({
                        ...prev,
                        quarterlyTargets: prev.quarterlyTargets.map((item, itemIndex) => itemIndex === index ? { ...item, budget: event.target.value } : item),
                      }))
                    }
                  />
                  <Textarea
                    label="Description"
                    rows={3}
                    value={quarter.description}
                    onChange={(event) =>
                      setForm(prev => ({
                        ...prev,
                        quarterlyTargets: prev.quarterlyTargets.map((item, itemIndex) => itemIndex === index ? { ...item, description: event.target.value } : item),
                      }))
                    }
                  />
                </div>
              </Card>
            ))}
          </div>
        </FormPanel>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Budget Defaults" description="Reusable budget and classification references." icon={<Target className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Select label="Budget Source" value={form.budgetSourceId} onChange={(event) => setForm(prev => ({ ...prev, budgetSourceId: event.target.value }))} options={[{ value: '', label: 'Select Budget Source' }, ...mockBudgetSources.map(item => ({ value: item.id, label: item.name }))]} />
              <Select label="Budget Type" value={form.budgetTypeId} onChange={(event) => setForm(prev => ({ ...prev, budgetTypeId: event.target.value }))} options={[{ value: '', label: 'Select Budget Type' }, ...mockBudgetTypes.map(item => ({ value: item.id, label: item.name }))]} />
            </FormRow>
            <Input label="Standard Classification" value={form.standardClassification} onChange={(event) => setForm(prev => ({ ...prev, standardClassification: event.target.value }))} />
            <Textarea label="Default Budget Information" rows={4} value={form.defaultBudgetInformation} onChange={(event) => setForm(prev => ({ ...prev, defaultBudgetInformation: event.target.value }))} />
          </FormPanel>

          <FormPanel title="References" description="Persist document and system references that copy into live OPMS targets." icon={<Target className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="IDP Reference" value={form.idpReference} onChange={(event) => setForm(prev => ({ ...prev, idpReference: event.target.value }))} />
              <Input label="Internal Reference" value={form.internalReference} onChange={(event) => setForm(prev => ({ ...prev, internalReference: event.target.value }))} />
            </FormRow>
            <Input label="FMS Link" value={form.fmsLink} onChange={(event) => setForm(prev => ({ ...prev, fmsLink: event.target.value }))} />
            <Textarea label="Default POE Requirements" rows={4} value={form.defaultPoeRequirements} onChange={(event) => setForm(prev => ({ ...prev, defaultPoeRequirements: event.target.value }))} />
          </FormPanel>
        </div>

        <div className="flex justify-end gap-3 border-t border-secondary-200 pt-4 dark:border-secondary-700">
          <Button variant="outline" onClick={() => setCurrentPath(template ? `/opms/library/${template.id}` : '/opms/library')}>
            Cancel
          </Button>
          <Button variant="primary" icon={<Save className="h-4 w-4" />} onClick={() => { void handleSave(); }}>
            {template ? 'Save Template' : 'Create Template'}
          </Button>
        </div>
      </div>
    </AppShell>
  );
}

export function IPMSTargetLibraryList() {
  const { pushToast, setCurrentPath } = useApp();
  const [ipmsTargetTemplates, setIpmsTargetTemplates] = useState<IpmsTargetTemplate[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<LibraryStatusFilter>('all');
  const [area, setArea] = useState('');
  const [departmentOrFunctionalArea, setDepartmentOrFunctionalArea] = useState('');
  const [employeeLevel, setEmployeeLevel] = useState('');
  const [targetUnitType, setTargetUnitType] = useState('');
  const [version, setVersion] = useState('');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  const loadTemplates = useCallback(async () => {
    setIsLoading(true);
    const result = await getIpmsTargetTemplatesApi();
    if (result.success && result.data) {
      setIpmsTargetTemplates(result.data);
    } else {
      pushToast('error', result.message ?? 'Failed to load IPMS target library');
    }
    setIsLoading(false);
  }, [pushToast]);

  useEffect(() => {
    void loadTemplates();
  }, [loadTemplates]);

  const filteredTemplates = useMemo(
    () =>
      ipmsTargetTemplates.filter(template => {
        const matchesSearch = `${template.templateCode} ${template.templateName} ${template.targetName} ${template.kpiDescription}`
          .toLowerCase()
          .includes(search.toLowerCase());
        const matchesStatus =
          status === 'all' ||
          (status === 'active' && template.isActive && !template.isArchived) ||
          (status === 'archived' && !!template.isArchived);
        const matchesArea = !area || template.performanceArea === area;
        const matchesDepartmentOrArea =
          !departmentOrFunctionalArea ||
          template.department?.name === departmentOrFunctionalArea ||
          template.functionalArea === departmentOrFunctionalArea;
        const matchesEmployeeLevel = !employeeLevel || template.employeeLevel === employeeLevel;
        const matchesUnitType = !targetUnitType || template.targetUnitType === targetUnitType;
        const matchesVersion = !version || String(template.version) === version;
        return matchesSearch && matchesStatus && matchesArea && matchesDepartmentOrArea && matchesEmployeeLevel && matchesUnitType && matchesVersion;
      }),
    [area, departmentOrFunctionalArea, employeeLevel, ipmsTargetTemplates, search, status, targetUnitType, version],
  );

  const areaOptions = Array.from(new Set(ipmsTargetTemplates.map(template => template.performanceArea).filter(Boolean))).sort();
  const departmentOptions = Array.from(new Set(ipmsTargetTemplates.flatMap(template => [template.department?.name, template.functionalArea]).filter(Boolean) as string[])).sort();
  const levelOptions = Array.from(new Set(ipmsTargetTemplates.map(template => template.employeeLevel).filter(Boolean))).sort();
  const versionOptions = Array.from(new Set(ipmsTargetTemplates.map(template => template.version))).sort((a, b) => b - a);
  const toggleSelected = (id: string) => {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]);
  };
  const toggleAll = () => {
    setSelectedIds(prev => (
      prev.length === filteredTemplates.length
        ? []
        : filteredTemplates.map(template => template.id)
    ));
  };

  const createSelectedTargets = async (templates: IpmsTargetTemplate[]) => {
    const results = await Promise.all(templates.map(template => createIpmsTargetApi(buildIpmsTargetPayloadFromTemplate(template))));
    const successCount = results.filter(result => result.success).length;
    const failureCount = results.length - successCount;
    setSelectedIds([]);

    if (successCount > 0) {
      pushToast('success', `${successCount} IPMS target${successCount === 1 ? '' : 's'} created from library`);
    }
    if (failureCount > 0) {
      pushToast('error', `${failureCount} IPMS target${failureCount === 1 ? '' : 's'} failed to create`);
    }
  };

  return (
    <AppShell title="IPMS Target Library" subtitle="Reusable generic IPMS target templates used to create live individual targets">
      <div className="space-y-6">
        <div className="flex items-center justify-between gap-3">
          <Badge variant="primary">{filteredTemplates.length} templates</Badge>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              disabled={selectedIds.length === 0}
              onClick={() => { void createSelectedTargets(filteredTemplates.filter(template => selectedIds.includes(template.id))); }}
            >
              Create Selected IPMS Targets
            </Button>
            <Button variant="primary" icon={<Plus className="h-4 w-4" />} onClick={() => setCurrentPath('/ipms/library/new')}>
              Create Template
            </Button>
          </div>
        </div>

        <LibraryFilters
          search={search}
          onSearchChange={setSearch}
          status={status}
          onStatusChange={setStatus}
          areaValue={area}
          areaLabel="Performance Area"
          areaOptions={areaOptions}
          onAreaChange={setArea}
          departmentOrFunctionalArea={departmentOrFunctionalArea}
          departmentOptions={departmentOptions}
          onDepartmentOrFunctionalAreaChange={setDepartmentOrFunctionalArea}
          kpiType={employeeLevel}
          kpiTypeOptions={levelOptions}
          onKpiTypeChange={setEmployeeLevel}
          targetUnitType={targetUnitType}
          onTargetUnitTypeChange={setTargetUnitType}
          version={version}
          versionOptions={versionOptions}
          onVersionChange={setVersion}
        />

        <TemplateListTable
          items={filteredTemplates}
          getRowId={(template) => template.id}
          selectedIds={selectedIds}
          onToggleRow={toggleSelected}
          onToggleAll={toggleAll}
          emptyMessage={isLoading ? 'Loading IPMS templates...' : 'No IPMS templates found'}
          columns={[
            {
              id: 'template',
              header: 'Template',
              render: (template) => (
                <div>
                  <p className="font-medium text-secondary-900 dark:text-white">{template.templateName}</p>
                  <p className="text-[11px] text-secondary-500">{template.templateCode}</p>
                </div>
              ),
            },
            {
              id: 'target',
              header: 'Target',
              render: (template) => (
                <div>
                  <p>{template.targetName}</p>
                  <p className="text-[11px] text-secondary-500">{template.kpiDescription}</p>
                </div>
              ),
            },
            {
              id: 'area',
              header: 'Performance Area',
              render: (template) => template.performanceArea,
            },
            {
              id: 'level',
              header: 'Employee Level',
              render: (template) => template.employeeLevel,
            },
            {
              id: 'unit',
              header: 'Target Unit Type',
              render: (template) => template.targetUnitType,
            },
            {
              id: 'version',
              header: 'Version',
              render: (template) => (
                <div className="flex items-center gap-2">
                  <Badge variant={template.isArchived ? 'error' : 'success'}>{template.isArchived ? 'Archived' : 'Active'}</Badge>
                  <Badge variant="default">v{template.version}</Badge>
                </div>
              ),
            },
          ]}
          actions={(template) => [
            { label: 'View', onClick: () => setCurrentPath(`/ipms/library/${template.id}`) },
            { label: 'Edit', onClick: () => setCurrentPath(`/ipms/library/${template.id}/edit`) },
            {
              label: 'Duplicate',
              onClick: () => {
                void (async () => {
                  const result = await duplicateIpmsTargetTemplateApi(template.id);
                  if (result.success) {
                    pushToast('success', 'IPMS template duplicated');
                    await loadTemplates();
                  } else {
                    pushToast('error', result.message ?? 'Failed to duplicate IPMS template');
                  }
                })();
              },
            },
            {
              label: 'Archive/Delete',
              variant: 'error',
              onClick: () => {
                void (async () => {
                  const result = await archiveIpmsTargetTemplateApi(template.id);
                  if (result.success) {
                    pushToast('success', 'IPMS template archived');
                    await loadTemplates();
                  } else {
                    pushToast('error', result.message ?? 'Failed to archive IPMS template');
                  }
                })();
              },
            },
            {
              label: 'Create IPMS Target',
              variant: 'primary',
              onClick: () => {
                localStorage.setItem('pending_ipms_template_id', template.id);
                setCurrentPath('/ipms/targets/new');
              },
            },
          ]}
        />

        {!isLoading && filteredTemplates.length === 0 && (
          <EmptyState
            icon={<Library className="h-6 w-6" />}
            title="No IPMS templates found"
            description="Try changing the filters or create a new IPMS library template."
            action={<Button variant="primary" onClick={() => setCurrentPath('/ipms/library/new')}>Create Template</Button>}
          />
        )}
      </div>
    </AppShell>
  );
}

export function IPMSTargetLibraryDetail({ templateId }: { templateId: string }) {
  const { pushToast, setCurrentPath } = useApp();
  const [template, setTemplate] = useState<IpmsTargetTemplate | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadTemplate = async () => {
      setIsLoading(true);
      const result = await getIpmsTargetTemplateApi(templateId);
      if (result.success && result.data) {
        setTemplate(result.data);
      } else {
        setTemplate(null);
      }
      setIsLoading(false);
    };

    void loadTemplate();
  }, [templateId]);

  if (isLoading) {
    return (
      <AppShell title="IPMS Target Library" subtitle="Template detail">
        <Card>
          <p className="text-sm text-secondary-500">Loading template...</p>
        </Card>
      </AppShell>
    );
  }

  if (!template) {
    return (
      <AppShell title="IPMS Target Library" subtitle="Template detail">
        <EmptyState
          icon={<Library className="h-6 w-6" />}
          title="Template not found"
          action={<Button variant="primary" onClick={() => setCurrentPath('/ipms/library')}>Back to Library</Button>}
        />
      </AppShell>
    );
  }

  return (
    <AppShell title={template.templateName} subtitle="IPMS library template detail">
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Button variant="ghost" icon={<ArrowLeft className="h-4 w-4" />} onClick={() => setCurrentPath('/ipms/library')}>
              Back
            </Button>
            <Badge variant={template.isArchived ? 'error' : 'success'}>{template.isArchived ? 'Archived' : 'Active'}</Badge>
            <Badge variant="default">v{template.version}</Badge>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button
              variant="outline"
              icon={<Copy className="h-4 w-4" />}
              onClick={() => {
                void (async () => {
                  const result = await duplicateIpmsTargetTemplateApi(template.id);
                  if (result.success && result.data) {
                    pushToast('success', 'IPMS template duplicated');
                    setCurrentPath(`/ipms/library/${result.data.id}`);
                  } else {
                    pushToast('error', result.message ?? 'Failed to duplicate IPMS template');
                  }
                })();
              }}
            >
              Duplicate
            </Button>
            <Button
              variant="outline"
              icon={<Archive className="h-4 w-4" />}
              onClick={() => {
                void (async () => {
                  const result = await archiveIpmsTargetTemplateApi(template.id);
                  if (result.success) {
                    pushToast('success', 'IPMS template archived');
                    setCurrentPath('/ipms/library');
                  } else {
                    pushToast('error', result.message ?? 'Failed to archive IPMS template');
                  }
                })();
              }}
            >
              Archive/Delete
            </Button>
            <Button
              variant="primary"
              icon={<UserSquare2 className="h-4 w-4" />}
              onClick={() => {
                localStorage.setItem('pending_ipms_template_id', template.id);
                setCurrentPath('/ipms/targets/new');
              }}
            >
              Create IPMS Target from Template
            </Button>
          </div>
        </div>

        <FormHero
          eyebrow="IPMS Target Library"
          title={template.templateName}
          description="This template is a reusable generic individual performance definition. Live IPMS targets copy from it and then capture employee, supervisor, department, period, and related OPMS links."
          badges={
            <>
              <Badge variant="primary">{template.templateCode}</Badge>
              <Badge variant="info">{template.employeeLevel}</Badge>
            </>
          }
        />

        <div className="grid gap-4 xl:grid-cols-3">
          <DetailItem label="Target Name" value={template.targetName} />
          <DetailItem label="Performance Area" value={template.performanceArea} />
          <DetailItem label="Employee Level" value={template.employeeLevel} />
          <DetailItem label="Job Grade" value={template.jobGrade} />
          <DetailItem label="Target Unit Type" value={template.targetUnitType} />
          <DetailItem label="Unit of Measure" value={template.unitOfMeasure.name} />
          <DetailItem label="Annual Target" value={template.annualTarget} />
          <DetailItem label="Weight" value={`${template.weight}%`} />
          <DetailItem label="Default Rating Method" value={template.defaultRatingMethod} />
          <DetailItem label="Default Score Scale" value={template.defaultScoreScale} />
          <DetailItem label="Linked OPMS Required" value={template.linkedOpmsTargetRequired ? 'Yes' : 'No'} />
          <DetailItem label="Created Date" value={formatDate(template.createdDate)} />
        </div>

        <Card>
          <h3 className="text-sm font-semibold text-secondary-900 dark:text-white">KPI Description</h3>
          <p className="mt-2 text-sm text-secondary-600 dark:text-secondary-400">{template.kpiDescription}</p>
        </Card>

        <div className="grid gap-4 xl:grid-cols-2">
          <DetailItem label="Default POE Requirements" value={template.defaultPoeRequirements} />
          <DetailItem
            label="Default Task Templates"
            value={
              <div className="space-y-1">
                {template.defaultTaskTemplates.map(task => (
                  <div key={task}>{task}</div>
                ))}
              </div>
            }
          />
        </div>
      </div>
    </AppShell>
  );
}

export function IPMSTargetTemplateFormPage({ templateId }: { templateId?: string }) {
  const { pushToast, setCurrentPath } = useApp();
  const [template, setTemplate] = useState<IpmsTargetTemplate | null>(null);
  const [isLoading, setIsLoading] = useState(!!templateId);
  const { form, setForm } = useIpmsTemplateForm(template);

  useEffect(() => {
    if (!templateId) {
      setTemplate(null);
      setIsLoading(false);
      return;
    }

    const loadTemplate = async () => {
      setIsLoading(true);
      const result = await getIpmsTargetTemplateApi(templateId);
      if (result.success && result.data) {
        setTemplate(result.data);
      } else {
        pushToast('error', result.message ?? 'Failed to load IPMS template');
      }
      setIsLoading(false);
    };

    void loadTemplate();
  }, [templateId, pushToast]);

  const handleSave = async () => {
    const payload = buildIpmsTemplatePayload(form);

    if (template) {
      const result = await updateIpmsTargetTemplateApi(template.id, payload);
      if (result.success && result.data) {
        pushToast('success', 'IPMS template updated');
        setCurrentPath(`/ipms/library/${result.data.id}`);
      } else {
        pushToast('error', result.message ?? 'Failed to update IPMS template');
      }
      return;
    }

    const result = await createIpmsTargetTemplateApi(payload);
    if (result.success && result.data) {
      pushToast('success', 'IPMS template created');
      setCurrentPath(`/ipms/library/${result.data.id}`);
    } else {
      pushToast('error', result.message ?? 'Failed to create IPMS template');
    }
  };

  if (isLoading) {
    return (
      <AppShell title={templateId ? 'Edit IPMS Target Template' : 'Create IPMS Target Template'} subtitle="Reusable generic IPMS target definition">
        <Card>
          <p className="text-sm text-secondary-500">Loading template...</p>
        </Card>
      </AppShell>
    );
  }

  return (
    <AppShell title={template ? 'Edit IPMS Target Template' : 'Create IPMS Target Template'} subtitle="Reusable generic IPMS target definition">
      <div className="space-y-6">
        <FormHero
          eyebrow="IPMS Target Library"
          title={template ? 'Update IPMS target template' : 'Create reusable IPMS target template'}
          description="Capture a reusable generic IPMS definition. Live IPMS targets will inherit these values and then capture employee, supervisor, period, department, and related OPMS target details."
          badges={<Badge variant="default">{template ? 'Edit Mode' : 'New Template'}</Badge>}
        />

        <div className="grid gap-4 xl:grid-cols-[1.05fr_0.95fr]">
          <FormPanel title="Template Identity" description="Basic reusable definition for the IPMS library." icon={<Library className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Template Code" required value={form.templateCode} onChange={(event) => setForm(prev => ({ ...prev, templateCode: event.target.value }))} />
              <Input label="Template Name" required value={form.templateName} onChange={(event) => setForm(prev => ({ ...prev, templateName: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Department" value={form.departmentId} onChange={(event) => setForm(prev => ({ ...prev, departmentId: event.target.value }))} options={[{ value: '', label: 'General Template' }, ...mockDepartments.map(item => ({ value: item.id, label: item.name }))]} />
              <Input label="Functional Area" value={form.functionalArea} onChange={(event) => setForm(prev => ({ ...prev, functionalArea: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Input label="Performance Area" required value={form.performanceArea} onChange={(event) => setForm(prev => ({ ...prev, performanceArea: event.target.value }))} />
              <Input label="Target Name" required value={form.targetName} onChange={(event) => setForm(prev => ({ ...prev, targetName: event.target.value }))} />
            </FormRow>
            <Textarea label="KPI Description" required rows={4} value={form.kpiDescription} onChange={(event) => setForm(prev => ({ ...prev, kpiDescription: event.target.value }))} />
          </FormPanel>

          <FormPanel title="Lifecycle" description="Versioning and default status for the reusable IPMS template." icon={<Eye className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Version" required type="number" value={form.version} onChange={(event) => setForm(prev => ({ ...prev, version: event.target.value }))} />
              <Input label="Created By" required value={form.createdBy} onChange={(event) => setForm(prev => ({ ...prev, createdBy: event.target.value }))} />
            </FormRow>
            <Input label="Created Date" required type="date" value={form.createdDate} onChange={(event) => setForm(prev => ({ ...prev, createdDate: event.target.value }))} />
            <Checkbox label="Is Active" checked={form.isActive} onChange={(event) => setForm(prev => ({ ...prev, isActive: event.target.checked }))} />
            <Checkbox label="Linked OPMS Target Required" checked={form.linkedOpmsTargetRequired} onChange={(event) => setForm(prev => ({ ...prev, linkedOpmsTargetRequired: event.target.checked }))} />
          </FormPanel>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Employee Applicability" description="Reusable defaults for role level, grade, and measurement." icon={<UserSquare2 className="h-5 w-5" />}>
            <FormRow cols={3}>
              <Input label="Employee Level" required value={form.employeeLevel} onChange={(event) => setForm(prev => ({ ...prev, employeeLevel: event.target.value }))} />
              <Input label="Job Grade" required value={form.jobGrade} onChange={(event) => setForm(prev => ({ ...prev, jobGrade: event.target.value }))} />
              <Input label="Weight" required type="number" value={form.weight} onChange={(event) => setForm(prev => ({ ...prev, weight: event.target.value }))} />
            </FormRow>
            <FormRow cols={3}>
              <Select label="Target Unit Type" required value={form.targetUnitType} onChange={(event) => setForm(prev => ({ ...prev, targetUnitType: event.target.value as TargetUnitType }))} options={targetUnitTypes} />
              <Select label="Unit of Measure" required value={form.unitOfMeasureId} onChange={(event) => setForm(prev => ({ ...prev, unitOfMeasureId: event.target.value }))} options={mockUnitsOfMeasure.map(item => ({ value: item.id, label: item.name }))} />
              <Input label="Annual Target" required type="number" value={form.annualTarget} onChange={(event) => setForm(prev => ({ ...prev, annualTarget: event.target.value }))} />
            </FormRow>
            <Textarea label="Annual Target Description" rows={3} value={form.annualTargetDescription} onChange={(event) => setForm(prev => ({ ...prev, annualTargetDescription: event.target.value }))} />
          </FormPanel>

          <FormPanel title="Scoring Defaults" description="Default rating method, score scale, and evidence expectations." icon={<Target className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Default Rating Method" value={form.defaultRatingMethod} onChange={(event) => setForm(prev => ({ ...prev, defaultRatingMethod: event.target.value }))} />
              <Input label="Default Score Scale" value={form.defaultScoreScale} onChange={(event) => setForm(prev => ({ ...prev, defaultScoreScale: event.target.value }))} />
            </FormRow>
            <Textarea label="Default POE Requirements" rows={4} value={form.defaultPoeRequirements} onChange={(event) => setForm(prev => ({ ...prev, defaultPoeRequirements: event.target.value }))} />
            <Textarea
              label="Default Task Templates"
              rows={6}
              value={form.defaultTaskTemplates}
              onChange={(event) => setForm(prev => ({ ...prev, defaultTaskTemplates: event.target.value }))}
              helpText="Enter one default task template per line."
            />
          </FormPanel>
        </div>

        <div className="flex justify-end gap-3 border-t border-secondary-200 pt-4 dark:border-secondary-700">
          <Button variant="outline" onClick={() => setCurrentPath(template ? `/ipms/library/${template.id}` : '/ipms/library')}>
            Cancel
          </Button>
          <Button variant="primary" icon={<Save className="h-4 w-4" />} onClick={() => { void handleSave(); }}>
            {template ? 'Save Template' : 'Create Template'}
          </Button>
        </div>
      </div>
    </AppShell>
  );
}

export { OpmsTemplateSelectionModal, IpmsTemplateSelectionModal };
