import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import type { OPMSTarget } from '../../types';
import { KpiCircular88ReferenceTab, KpiStrategicAlignmentTab, KpiTidReferenceTab } from './KpiReferenceTabs';
import { useKpiOptionalModuleState } from './kpiOptionalModules';

const api = vi.hoisted(() => ({
  getTidConfiguration: vi.fn(), getTidHistoryPage: vi.fn(), getC88ConfigurationsPage: vi.fn(),
  getC88MappingsPage: vi.fn(), getC88IndicatorsPage: vi.fn(), getC88AssignmentsPage: vi.fn(), getC88PlansPage: vi.fn(),
}));
const app = vi.hoisted(() => ({ setCurrentPath: vi.fn() }));
const security = vi.hoisted(() => ({ canRead: vi.fn() }));

vi.mock('../../api/api', async importOriginal => ({ ...(await importOriginal<typeof import('../../api/api')>()), ...api }));
vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));

const target = {
  id: 'target-public', municipalityFinancialYearPublicId: 'year-public', indicatorNumber: 'FIN-001',
  nationalKPA: 'Basic Services', municipalKPA: 'Service Delivery', strategicGoalName: 'Reliable services',
  strategicIntervention: 'Renewal programme', strategicObjectiveName: 'Reduce interruptions',
  performanceObjectiveName: 'Continuity', idpReference: '',
} as unknown as OPMSTarget;

function OptionalProbe() {
  const state = useKpiOptionalModuleState(target);
  return <div>{state.loading ? 'Loading' : `${state.tidVisible}:${state.c88Visible}:${state.c88Configuration?.publicId ?? '-'}`}</div>;
}

describe('KPI optional reference tabs', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canRead.mockReturnValue(true);
  });

  it('shows optional tabs only from effective municipality configuration', async () => {
    api.getTidConfiguration.mockResolvedValue({ success: true, data: { tidEnabled: true } });
    api.getC88ConfigurationsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'config-1', isEnabled: true }], page: 1, pageSize: 1, totalCount: 1, totalPages: 1 } });
    render(<OptionalProbe />);
    expect(await screen.findByText('true:true:config-1')).toBeInTheDocument();
    expect(api.getC88ConfigurationsPage).toHaveBeenCalledWith(expect.anything(), expect.objectContaining({ municipalityFinancialYearPublicId: 'year-public', enabled: true }));
  });

  it('renders missing IDP alignment as informational without blocking core OPMS', () => {
    render(<KpiStrategicAlignmentTab target={target} />);
    expect(screen.getByText('IDP Alignment: Not Mapped')).toBeInTheDocument();
    expect(screen.getByText(/Missing optional mappings do not block reporting or workflow/)).toBeInTheDocument();
    expect(screen.getByText('Reliable services')).toBeInTheDocument();
  });

  it('renders the current municipality TID and immutable history as view only', async () => {
    api.getTidHistoryPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'tid-1', targetPublicId: 'target-public', versionNumber: 2, indicatorDefinition: 'Reliable water availability', purpose: 'Measure continuity',
      dataSource: 'Operations', collectionMethod: 'Meter readings', calculationMethod: 'Available hours / planned hours', verificationMethod: 'Signed operations report',
      responsibleEmployeeName: 'TID Owner', effectiveFrom: '2026-07-01T00:00:00Z', isCurrent: true, createdAt: '2026-07-01T00:00:00Z', rowVersion: 'AQ==', sourceDocuments: [],
    }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    render(<KpiTidReferenceTab targetPublicId="target-public" />);
    expect(await screen.findByText('Reliable water availability')).toBeInTheDocument();
    expect(screen.getByText('Current v2')).toBeInTheDocument();
    expect(screen.getByText('Operational reference · view only')).toBeInTheDocument();
  });

  it('loads exact-target Circular 88 mappings with official definition, plan, data elements and responsibility', async () => {
    api.getC88MappingsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'map-1', indicatorPublicId: 'indicator-1', opmsTargetPublicId: 'target-public', mappingType: 'Direct', isActive: true }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getC88IndicatorsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'indicator-1', code: 'C88-WAT-1', name: 'Water continuity', definition: 'Official definition', officialTechnicalIndicatorDescription: 'Official TID', valueType: 'Decimal', calculationOperator: 'Ratio', isActive: true, dataElements: [{ code: 'HOURS', name: 'Available hours' }], applicability: [{}] }], page: 1, pageSize: 1, totalCount: 1, totalPages: 1 } });
    api.getC88AssignmentsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'assignment-1', role: 'PrimaryCapturer', employeeName: 'C88 Owner' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getC88PlansPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'plan-1', baselineValue: '10', annualTarget: '20' }], page: 1, pageSize: 1, totalCount: 1, totalPages: 1 } });
    render(<KpiCircular88ReferenceTab targetPublicId="target-public" configuration={{ publicId: 'config-1', municipalityFinancialYearPublicId: 'year-public', financialYearCode: '2026/27', catalogueVersionPublicId: 'version-1', catalogueVersionCode: 'NT-2026', isEnabled: true, effectiveFrom: '2026-07-01', rowVersion: 'AQ==' }} />);
    expect(await screen.findByText('C88-WAT-1 · Water continuity')).toBeInTheDocument();
    expect(screen.getAllByText('Official definition')).toHaveLength(2);
    expect(screen.getByText('HOURS: Available hours')).toBeInTheDocument();
    expect(screen.getByText('PrimaryCapturer: C88 Owner')).toBeInTheDocument();
    expect(api.getC88MappingsPage).toHaveBeenCalledWith(expect.anything(), expect.objectContaining({ opmsTargetPublicId: 'target-public', active: true }));
    await waitFor(() => expect(api.getC88IndicatorsPage).toHaveBeenCalled());
  });
});
