import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import {
  IPMSTargetLibraryDetail,
  IPMSTargetLibraryList,
  IPMSTargetTemplateFormPage,
  OPMSTargetLibraryDetail,
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
  getStrategicPlanningMastersPage: vi.fn(),
  updateIpmsTargetTemplate: vi.fn(),
  updateOpmsTargetTemplate: vi.fn(),
}));
const app = vi.hoisted(() => ({ pushToast: vi.fn(), setCurrentPath: vi.fn() }));
const security = vi.hoisted(() => ({
  canReadField: vi.fn(() => true),
  canEditField: vi.fn(() => true),
}));
vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));
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
    security.canReadField.mockReturnValue(true);
    security.canEditField.mockReturnValue(true);
    api.getOpmsTargetTemplateFacets.mockResolvedValue({ success: true, data: facets });
    api.getIpmsTargetTemplateFacets.mockResolvedValue({ success: true, data: facets });
    api.getOpmsTargetTemplatesPage.mockResolvedValue({ success: true, data: { items: [opmsTemplate], page: 1, pageSize: 25, totalCount: 27, totalPages: 2 } });
    api.getIpmsTargetTemplatesPage.mockResolvedValue({ success: true, data: { items: [ipmsTemplate], page: 1, pageSize: 25, totalCount: 28, totalPages: 2 } });
    const masters = {
      'strategic-goals': [{ publicId: 'goal-public', code: 'SG1', name: 'Service Delivery Excellence' }],
      'strategic-objectives': [{ publicId: 'objective-public', code: 'SO1', name: 'Improve Road Infrastructure' }],
      'budget-sources': [{ publicId: 'source-public', code: 'MIG', name: 'Municipal Infrastructure Grant' }],
      'budget-types': [{ publicId: 'type-public', code: 'CAPEX', name: 'Capital Expenditure' }],
      'kpi-units-of-measure': [
        { publicId: 'kilometres-public', code: 'KM', name: 'Kilometers' },
        { publicId: 'percentage-public', code: 'PCT', name: 'Percentage' },
      ],
    } as const;
    api.getStrategicPlanningMastersPage.mockImplementation(async (kind: keyof typeof masters, query: { search?: string }) => {
      const rows = masters[kind] ?? [];
      const items = query.search ? rows.filter(item => item.name.toLowerCase() === query.search?.toLowerCase()) : rows;
      return { success: true, data: { items, page: 1, pageSize: 25, totalCount: items.length, totalPages: items.length ? 1 : 0 } };
    });
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
      createdDate: '2026-10-10T00:00:00Z',
    };
    api.getOpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });
    api.updateOpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });

    render(<OPMSTargetTemplateFormPage templateId="opms-public-id" />);

    expect(await screen.findByText('Edit Mode')).toBeInTheDocument();
    expect(screen.queryByLabelText(/Created By/i)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/Created Date/i)).not.toBeInTheDocument();
    expect(screen.getByText(/Creator identity and creation time are assigned by the server/i)).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByLabelText('Unit of Measure*')).toHaveValue('kilometres-public');
      expect(screen.getByLabelText('Strategic Goal')).toHaveValue('goal-public');
      expect(screen.getByLabelText('Strategic Objective')).toHaveValue('objective-public');
      expect(screen.getByLabelText('Budget Source')).toHaveValue('source-public');
      expect(screen.getByLabelText('Budget Type')).toHaveValue('type-public');
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
      createdDate: '2026-10-10T00:00:00Z',
    };
    api.getIpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });
    api.updateIpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });

    render(<IPMSTargetTemplateFormPage templateId="ipms-public-id" />);

    expect(await screen.findByText('Edit Mode')).toBeInTheDocument();
    expect(screen.queryByLabelText(/Created By/i)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/Created Date/i)).not.toBeInTheDocument();
    expect(screen.getByText(/Creator identity and creation time are assigned by the server/i)).toBeInTheDocument();
    await waitFor(() => expect(screen.getByLabelText('Unit of Measure*')).toHaveValue('percentage-public'));
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

  it.each([
    {
      label: 'OPMS',
      component: <OPMSTargetLibraryDetail templateId="opms-public-id" />,
      load: () => api.getOpmsTargetTemplate.mockResolvedValue({
        success: true,
        data: {
          ...opmsTemplate,
          publicId: 'opms-public-id', rowVersion: 'row-version', baseline: 5, annualTarget: 40,
          annualTargetDescription: 'Annual description', unitOfMeasure: { id: '', name: 'Kilometers' },
          strategicGoal: { id: '', name: 'Service Delivery Excellence' },
          strategicObjective: { id: '', name: 'Improve Road Infrastructure' },
          performanceObjective: 'Improve roads', budgetSource: { id: '', name: 'Grant' },
          budgetType: { id: '', name: 'Capital' }, weight: 10, indicatorType: 'Quantitative',
          defaultQuarterlyTargets: [], createdDate: '2026-10-10T00:00:00Z',
        },
      }),
      editPath: '/opms/library/1/edit',
    },
    {
      label: 'IPMS',
      component: <IPMSTargetLibraryDetail templateId="ipms-public-id" />,
      load: () => api.getIpmsTargetTemplate.mockResolvedValue({
        success: true,
        data: {
          ...ipmsTemplate,
          publicId: 'ipms-public-id', rowVersion: 'row-version', jobGrade: '16',
          unitOfMeasure: { id: '', name: 'Percentage' }, annualTarget: 100,
          annualTargetDescription: 'Annual description', weight: 20, defaultRatingMethod: 'Weighted',
          defaultScoreScale: '1-5', defaultPoeRequirements: 'Evidence', defaultTaskTemplates: [],
          linkedOpmsTargetRequired: false, createdDate: '2026-10-10T00:00:00Z',
        },
      }),
      editPath: '/ipms/library/2/edit',
    },
  ])('opens the $label edit form from an active template detail', async ({ component, load, editPath }) => {
    load();
    render(component);

    fireEvent.click(await screen.findByRole('button', { name: 'Edit' }));

    expect(app.setCurrentPath).toHaveBeenCalledWith(editPath);
  });

  it('does not render or duplicate hostile OPMS protected values without member reads', async () => {
    security.canReadField.mockReturnValue(false);
    security.canEditField.mockReturnValue(false);
    api.getOpmsTargetTemplate.mockResolvedValue({
      success: true,
      data: {
        ...opmsTemplate,
        publicId: 'opms-public-id', rowVersion: 'row-version', baseline: 5, annualTarget: 40,
        annualTargetDescription: 'Annual description', unitOfMeasure: { id: '', name: 'Kilometers' },
        performanceObjective: 'Improve roads', weight: 10, indicatorType: 'Quantitative',
        internalReference: 'SECRET-INTERNAL', fmsLink: 'https://secret-fms.example',
        defaultQuarterlyTargets: [{ quarter: 'Q1', target: 10, description: 'SECRET-QUARTER', budget: 500 }],
        defaultBudgetInformation: 'SECRET-BUDGET', defaultPoeRequirements: 'SECRET-POE',
        createdDate: '2026-10-10T00:00:00Z',
      },
    });

    render(<OPMSTargetLibraryDetail templateId="opms-public-id" />);

    expect(await screen.findByText('Water delivery')).toBeInTheDocument();
    expect(screen.queryByText('SECRET-INTERNAL')).not.toBeInTheDocument();
    expect(screen.queryByText('https://secret-fms.example')).not.toBeInTheDocument();
    expect(screen.queryByText('SECRET-QUARTER')).not.toBeInTheDocument();
    expect(screen.queryByText('SECRET-BUDGET')).not.toBeInTheDocument();
    expect(screen.queryByText('SECRET-POE')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Duplicate' })).not.toBeInTheDocument();
  });

  it('omits hostile OPMS protected values from edits without member updates', async () => {
    security.canEditField.mockReturnValue(false);
    const editable = {
      ...opmsTemplate,
      publicId: 'opms-public-id', rowVersion: 'row-version', baseline: 5, annualTarget: 40,
      annualTargetDescription: 'Annual description', unitOfMeasure: { id: '', name: 'Kilometers' },
      performanceObjective: 'Improve roads', weight: 10, indicatorType: 'Quantitative',
      internalReference: 'SECRET-INTERNAL', fmsLink: 'https://secret-fms.example',
      defaultQuarterlyTargets: [{ quarter: 'Q1', target: 10, description: 'SECRET-QUARTER', budget: 500 }],
      defaultBudgetInformation: 'SECRET-BUDGET', defaultPoeRequirements: 'SECRET-POE',
      createdDate: '2026-10-10T00:00:00Z',
    };
    api.getOpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });
    api.updateOpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });

    render(<OPMSTargetTemplateFormPage templateId="opms-public-id" />);

    expect(await screen.findByText('Edit Mode')).toBeInTheDocument();
    expect(screen.queryByLabelText('Internal Reference')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('FMS Link')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Default Budget Information')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Default POE Requirements')).not.toBeInTheDocument();
    expect(screen.queryByText('Default Quarterly Targets')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Save Template' }));

    await waitFor(() => expect(api.updateOpmsTargetTemplate).toHaveBeenCalled());
    const payload = api.updateOpmsTargetTemplate.mock.calls[0][1];
    expect(payload).not.toHaveProperty('internalReference');
    expect(payload).not.toHaveProperty('fmsLink');
    expect(payload).not.toHaveProperty('defaultQuarterlyTargetsJson');
    expect(payload).not.toHaveProperty('defaultBudgetInformation');
    expect(payload).not.toHaveProperty('defaultPoeRequirements');
  });

  it('does not render or duplicate hostile IPMS protected values without member reads', async () => {
    security.canReadField.mockReturnValue(false);
    security.canEditField.mockReturnValue(false);
    api.getIpmsTargetTemplate.mockResolvedValue({
      success: true,
      data: {
        ...ipmsTemplate,
        publicId: 'ipms-public-id', rowVersion: 'row-version', jobGrade: 'SECRET-GRADE',
        unitOfMeasure: { id: '', name: 'Percentage' }, annualTarget: 100,
        annualTargetDescription: 'Annual description', weight: 20, defaultRatingMethod: 'Weighted',
        defaultScoreScale: 'SECRET-SCALE', defaultPoeRequirements: 'SECRET-POE',
        defaultTaskTemplates: ['SECRET-TASK'], linkedOpmsTargetRequired: false,
        createdDate: '2026-10-10T00:00:00Z',
      },
    });

    render(<IPMSTargetLibraryDetail templateId="ipms-public-id" />);

    expect(await screen.findByText('Manager template')).toBeInTheDocument();
    expect(screen.queryByText('SECRET-GRADE')).not.toBeInTheDocument();
    expect(screen.queryByText('SECRET-SCALE')).not.toBeInTheDocument();
    expect(screen.queryByText('SECRET-POE')).not.toBeInTheDocument();
    expect(screen.queryByText('SECRET-TASK')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Duplicate' })).not.toBeInTheDocument();
  });

  it('omits hostile IPMS protected values from edits without member updates', async () => {
    security.canEditField.mockReturnValue(false);
    const editable = {
      ...ipmsTemplate,
      publicId: 'ipms-public-id', rowVersion: 'row-version', jobGrade: 'SECRET-GRADE',
      unitOfMeasure: { id: '', name: 'Percentage' }, annualTarget: 100,
      annualTargetDescription: 'Annual description', weight: 20, defaultRatingMethod: 'Weighted',
      defaultScoreScale: 'SECRET-SCALE', defaultPoeRequirements: 'SECRET-POE',
      defaultTaskTemplates: ['SECRET-TASK'], linkedOpmsTargetRequired: false,
      createdDate: '2026-10-10T00:00:00Z',
    };
    api.getIpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });
    api.updateIpmsTargetTemplate.mockResolvedValue({ success: true, data: editable });

    render(<IPMSTargetTemplateFormPage templateId="ipms-public-id" />);

    expect(await screen.findByText('Edit Mode')).toBeInTheDocument();
    expect(screen.queryByLabelText('Job Grade*')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Default Score Scale')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Default POE Requirements')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Default Task Templates')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Save Template' }));

    await waitFor(() => expect(api.updateIpmsTargetTemplate).toHaveBeenCalled());
    const payload = api.updateIpmsTargetTemplate.mock.calls[0][1];
    expect(payload).not.toHaveProperty('jobGrade');
    expect(payload).not.toHaveProperty('defaultScoreScale');
    expect(payload).not.toHaveProperty('defaultPoeRequirements');
    expect(payload).not.toHaveProperty('defaultTaskTemplatesJson');
  });
});
