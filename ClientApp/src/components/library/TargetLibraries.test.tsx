import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import {
  IPMSTargetLibraryList,
  IPMSTargetTemplateFormPage,
  OPMSTargetLibraryList,
  OPMSTargetTemplateFormPage,
} from './TargetLibraries';

const api = vi.hoisted(() => ({
  archiveIpmsTargetTemplate: vi.fn(),
  archiveOpmsTargetTemplate: vi.fn(),
  createIpmsTargetTemplate: vi.fn(),
  createOpmsTargetTemplate: vi.fn(),
  duplicateIpmsTargetTemplate: vi.fn(),
  duplicateOpmsTargetTemplate: vi.fn(),
  getIpmsTargetTemplate: vi.fn(),
  getIpmsTargetTemplateFacets: vi.fn(),
  getIpmsTargetTemplatesPage: vi.fn(),
  getOpmsTargetTemplate: vi.fn(),
  getOpmsTargetTemplateFacets: vi.fn(),
  getOpmsTargetTemplatesPage: vi.fn(),
  updateIpmsTargetTemplate: vi.fn(),
  updateOpmsTargetTemplate: vi.fn(),
}));
const app = vi.hoisted(() => ({ pushToast: vi.fn(), setCurrentPath: vi.fn() }));
const referenceData = vi.hoisted(() => ({
  lookups: {
    periods: [],
    strategicGoals: [{ id: 11, name: 'Service Delivery Excellence' }],
    strategicObjectives: [{ id: 12, name: 'Improve Road Infrastructure', strategicGoalId: 11 }],
    budgetSources: [{ id: 13, name: 'Municipal Infrastructure Grant' }],
    budgetTypes: [{ id: 14, name: 'Capital Expenditure' }],
    unitsOfMeasure: [{ id: 15, name: 'Kilometers' }, { id: 16, name: 'Percentage' }],
  },
  isLoading: false,
  error: null,
}));

vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../hooks/usePerformanceReferenceData', () => ({ usePerformanceReferenceData: () => referenceData }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));

const facets = { primaryAreas: ['Services'], functionalAreas: ['Operations'], classifications: ['Outcome'], targetUnitTypes: ['percentage'], versions: [2, 1] };
const opmsTemplate = {
  id: '1', templateCode: 'OP-01', templateName: 'Water template', indicatorNumber: 'KPI-01', targetName: 'Water target',
  kpiDescription: 'Water delivery', nationalKPA: 'Services', municipalKPA: 'Water', functionalArea: 'Operations', kpiType: 'Outcome',
  targetUnitType: 'percentage', version: 2, isActive: true, isArchived: false,
};
const ipmsTemplate = {
  id: '2', templateCode: 'IP-01', templateName: 'Manager template', targetName: 'Manager target', kpiDescription: 'Leadership',
  performanceArea: 'Leadership', employeeLevel: 'Manager', functionalArea: 'Corporate', targetUnitType: 'percentage',
  version: 2, isActive: true, isArchived: false,
};

describe('target library registers', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getOpmsTargetTemplateFacets.mockResolvedValue({ success: true, data: facets });
    api.getIpmsTargetTemplateFacets.mockResolvedValue({ success: true, data: facets });
    api.getOpmsTargetTemplatesPage.mockResolvedValue({ success: true, data: { items: [opmsTemplate], page: 1, pageSize: 25, totalCount: 27, totalPages: 2 } });
    api.getIpmsTargetTemplatesPage.mockResolvedValue({ success: true, data: { items: [ipmsTemplate], page: 1, pageSize: 25, totalCount: 28, totalPages: 2 } });
  });

  it('round-trips OPMS name-based references through the edit form without destructive defaults', async () => {
    const editable = {
      ...opmsTemplate,
      publicId: 'opms-public-id',
      rowVersion: 'row-version',
      baseline: 5,
      annualTarget: 40,
      annualTargetDescription: 'Forty kilometres',
      unitOfMeasure: { id: '', name: 'Kilometers' },
      strategicGoal: { id: '', name: 'Service Delivery Excellence' },
      strategicObjective: { id: '', name: 'Improve Road Infrastructure' },
      performanceObjective: 'Improve road condition',
      budgetSource: { id: '', name: 'Municipal Infrastructure Grant' },
      budgetType: { id: '', name: 'Capital Expenditure' },
      weight: 15,
      indicatorType: 'Quantitative',
      defaultQuarterlyTargets: [],
      createdBy: 'tester',
      createdDate: '2026-10-10T00:00:00Z',
    };
    api.getOpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });
    api.updateOpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });

    render(<OPMSTargetTemplateFormPage templateId="opms-public-id" />);

    expect(await screen.findByText('Edit Mode')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByLabelText('Unit of Measure*')).toHaveValue('15');
      expect(screen.getByLabelText('Strategic Goal')).toHaveValue('11');
      expect(screen.getByLabelText('Strategic Objective')).toHaveValue('12');
      expect(screen.getByLabelText('Budget Source')).toHaveValue('13');
      expect(screen.getByLabelText('Budget Type')).toHaveValue('14');
    });
    fireEvent.click(screen.getByRole('button', { name: 'Save Template' }));

    await waitFor(() => expect(api.updateOpmsTargetTemplate).toHaveBeenCalledWith(
      '1',
      expect.objectContaining({
        rowVersion: 'row-version',
        unitOfMeasure: 'Kilometers',
        strategicGoal: 'Service Delivery Excellence',
        strategicObjective: 'Improve Road Infrastructure',
        budgetSource: 'Municipal Infrastructure Grant',
        budgetType: 'Capital Expenditure',
      }),
    ));
  });

  it('round-trips the IPMS unit of measure through the edit form', async () => {
    const editable = {
      ...ipmsTemplate,
      publicId: 'ipms-public-id',
      rowVersion: 'row-version',
      jobGrade: '16',
      unitOfMeasure: { id: '', name: 'Percentage' },
      annualTarget: 100,
      annualTargetDescription: 'Annual delivery',
      weight: 20,
      defaultTaskTemplates: [],
      linkedOpmsTargetRequired: false,
      createdBy: 'tester',
      createdDate: '2026-10-10T00:00:00Z',
    };
    api.getIpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });
    api.updateIpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });

    render(<IPMSTargetTemplateFormPage templateId="ipms-public-id" />);

    expect(await screen.findByText('Edit Mode')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByLabelText('Unit of Measure*')).toHaveValue('16'));
    fireEvent.click(screen.getByRole('button', { name: 'Save Template' }));
    await waitFor(() => expect(api.updateIpmsTargetTemplate).toHaveBeenCalledWith(
      '2',
      expect.objectContaining({ rowVersion: 'row-version', unitOfMeasure: 'Percentage' }),
    ));
  });

  it('uses authoritative OPMS target-library paging, filters, and search', async () => {
    render(<OPMSTargetLibraryList />);

    expect(await screen.findByText('27 templates')).toBeInTheDocument();
    expect(api.getOpmsTargetTemplatesPage).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc' }));
    fireEvent.change(screen.getByLabelText('Search'), { target: { value: 'water' } });
    await waitFor(() => expect(api.getOpmsTargetTemplatesPage).toHaveBeenCalledWith(expect.objectContaining({ search: 'water', page: 1 })));
    fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'active' } });
    await waitFor(() => expect(api.getOpmsTargetTemplatesPage).toHaveBeenCalledWith(expect.objectContaining({ status: 'active', page: 1 })));
  });

  it('uses authoritative IPMS target-library paging and sorting', async () => {
    render(<IPMSTargetLibraryList />);

    expect(await screen.findByText('28 templates')).toBeInTheDocument();
    expect(api.getIpmsTargetTemplatesPage).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25 }));
    fireEvent.change(screen.getByLabelText('Sort templates'), { target: { value: 'templateCode' } });
    await waitFor(() => expect(api.getIpmsTargetTemplatesPage).toHaveBeenCalledWith(expect.objectContaining({ sortBy: 'templateCode', page: 1 })));
  });
});
