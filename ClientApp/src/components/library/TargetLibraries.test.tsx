import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { IPMSTargetLibraryList, OPMSTargetLibraryList } from './TargetLibraries';

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

vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
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
