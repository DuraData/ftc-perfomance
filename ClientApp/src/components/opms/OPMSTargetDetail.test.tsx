import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import type { OPMSTarget } from '../../types';
import { AssigneesTab, GeneralInfoTab, OPMSTargetDetail, QuarterlyTargetsTab, StrategyTab, VoteNumbersTab } from './OPMSTargetDetail';
import { readKpiDetailContext } from './kpiDetailNavigation';

const api = vi.hoisted(() => ({
  getOpmsTarget: vi.fn(),
  getOpmsSubmissionsPage: vi.fn(),
  getIpmsTargetsPage: vi.fn(),
  getAuditTrailsPage: vi.fn(),
  getTidConfiguration: vi.fn(),
  getTidHistoryPage: vi.fn(),
  getC88ConfigurationsPage: vi.fn(),
  getC88MappingsPage: vi.fn(),
  getC88IndicatorsPage: vi.fn(),
  getC88AssignmentsPage: vi.fn(),
  getC88PlansPage: vi.fn(),
}));
const app = vi.hoisted(() => ({ setCurrentPath: vi.fn() }));
const security = vi.hoisted(() => ({ canRead: vi.fn(), canReadField: vi.fn() }));

vi.mock('../../api/api', async importOriginal => ({ ...(await importOriginal<typeof import('../../api/api')>()), ...api }));
vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../common/Tabs', () => ({
  Tabs: ({ tabs, onChange }: { tabs: Array<{ id: string; label: string; badge?: number }>; onChange: (id: string) => void }) => <div>{tabs.map(tab => <button key={tab.id} type="button" onClick={() => onChange(tab.id)}>{tab.label} {tab.badge ?? ''}</button>)}</div>,
  TabPanel: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

const liveTarget = {
  department: { id: '17', name: 'Live Water Services' },
  voteNumbers: [{ id: 'vote-live', number: 'V-100', name: 'Live Capital Vote', amount: 2500 }],
  assignedTo: { id: 'employee-one', firstName: 'Live', lastName: 'Owner', displayName: 'Live Owner', email: 'owner@example.test' },
  additionalAssignees: [{ id: 'employee-two', firstName: 'Live', lastName: 'Support', displayName: 'Live Support', email: 'support@example.test' }],
} as OPMSTarget;

describe('OPMS target relational tabs', () => {
  beforeEach(() => {
    security.canRead.mockReturnValue(true);
    security.canReadField.mockReturnValue(true);
    api.getTidConfiguration.mockResolvedValue({ success: true, data: { municipalityPublicId: 'municipality-1', tidEnabled: false, allKpisRequired: false, scopedKpiCount: 1, currentTidCount: 0, missingTidCount: 0, rowVersion: 'AQ==' } });
    api.getC88ConfigurationsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 1, totalCount: 0, totalPages: 0 } });
  });

  it('accepts only governed KPI-detail deep-link tabs and preserves reporting-period context', () => {
    expect(readKpiDetailContext('?tab=internal-audit&period=period-public')).toEqual({
      tab: 'internal-audit',
      reportingPeriodPublicId: 'period-public',
    });
    expect(readKpiDetailContext('?tab=made-up&period=')).toEqual({ tab: 'kpi-details', reportingPeriodPublicId: undefined });
    expect(readKpiDetailContext('?tab=tid')).toEqual({ tab: 'tid', reportingPeriodPublicId: undefined });
    expect(readKpiDetailContext('?tab=circular-88')).toEqual({ tab: 'circular-88', reportingPeriodPublicId: undefined });
    expect(readKpiDetailContext('?tab=strategic-alignment')).toEqual({ tab: 'strategic-alignment', reportingPeriodPublicId: undefined });
  });

  it('preserves the selected reporting period when moving between governed workspace tabs', async () => {
    window.history.replaceState({}, '', '/opms/targets/target-public?tab=reporting&period=period-public');
    app.setCurrentPath.mockReset();
    const detailTarget = {
      ...liveTarget,
      id: 'target-public', period: { id: 'year-1', name: '2024/2025' }, unit: null,
      indicatorNumber: 'OPMS-1', targetName: 'Water KPI', kpiDescription: 'Description', targetUnitType: 'percentage',
      unitOfMeasure: { id: 'unit-1', name: 'Percent' }, kpiType: 'Quantitative', indicatorType: 'Output', annualTarget: 100, baseline: 50, weight: 10,
    } as unknown as OPMSTarget;
    api.getOpmsTarget.mockResolvedValue({ success: true, data: detailTarget });
    api.getOpmsSubmissionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getIpmsTargetsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getAuditTrailsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });

    render(<OPMSTargetDetail targetId="target-public" />);
    fireEvent.click(await screen.findByRole('button', { name: 'Internal Audit' }));

    expect(app.setCurrentPath).toHaveBeenCalledWith('/opms/targets/target-public?tab=internal-audit&period=period-public');
  });

  it('renders the governed financial-year name instead of a numeric legacy period placeholder', () => {
    render(<GeneralInfoTab target={{
      ...liveTarget,
      period: { id: '1', name: '2024/2025', startDate: '', endDate: '', fiscalYear: '', isActive: true },
      indicatorNumber: 'OPMS-1', targetName: 'Road target', kpiDescription: 'Roads resurfaced',
      targetUnitType: 'absolute_count', unitOfMeasure: { id: 'km', name: 'Kilometers', code: 'KM', isActive: true },
      kpiType: 'Quantitative', indicatorType: 'Output', weight: 20,
    } as OPMSTarget} />);

    expect(screen.getByLabelText('Period')).toHaveDisplayValue('2024/2025');
    expect(screen.queryByDisplayValue('Reporting period 1')).not.toBeInTheDocument();
  });

  it('renders vote numbers from the target API model', () => {
    render(<VoteNumbersTab target={liveTarget} />);

    expect(screen.getByText('V-100')).toBeInTheDocument();
    expect(screen.getByText('Live Capital Vote')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add' })).not.toBeInTheDocument();
  });

  it('renders only target assignees and removes duplicate actors', () => {
    render(<AssigneesTab target={{ ...liveTarget, additionalAssignees: [liveTarget.assignedTo!, ...liveTarget.additionalAssignees] }} />);

    expect(screen.getAllByText('Live Owner')).toHaveLength(1);
    expect(screen.getByText('Live Support')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add' })).not.toBeInTheDocument();
  });

  it('renders canonical strategic classifications instead of legacy placeholders', () => {
    render(<StrategyTab target={{
      ...liveTarget,
      nationalKPA: 'Basic Service Delivery', municipalKPA: 'Infrastructure', backToBasicsPillar: 'Delivering Basic Services',
      strategicGoal: { id: '', code: 'SG-INFRA', name: 'Reliable Infrastructure Services', isActive: true },
      strategicGoalCode: 'SG-INFRA', strategicIntervention: 'Priority Roads Delivery Programme',
      strategicObjective: { id: '', code: 'SO-ROADS', name: 'Improve Road and Stormwater Infrastructure', strategicGoal: {} as never, isActive: true },
      strategicObjectiveCode: 'SO-ROADS', performanceObjectiveCode: 'PO-ROADS', performanceObjective: 'Resurface Priority Road Kilometres',
      functionalArea: 'Technical Services', idpReference: 'IDP-1', internalReference: 'ITS-1', fmsLink: 'FMS-1',
    } as OPMSTarget} />);

    expect(screen.getByLabelText('Strategic Goal')).toHaveValue('SG-INFRA · Reliable Infrastructure Services');
    expect(screen.getByLabelText('Strategic Objective')).toHaveValue('SO-ROADS · Improve Road and Stormwater Infrastructure');
    expect(screen.getByLabelText('Performance Objective')).toHaveValue('PO-ROADS · Resurface Priority Road Kilometres');
    expect(screen.queryByDisplayValue('Not supplied by API')).not.toBeInTheDocument();
  });

  it('does not render protected live KPI references when their member reads are denied', () => {
    render(<StrategyTab target={{
      ...liveTarget, strategicGoal: { id: '', name: 'Goal', isActive: true },
      strategicObjective: { id: '', name: 'Objective', strategicGoal: {} as never, isActive: true },
      internalReference: 'INTERNAL-SECRET', fmsLink: 'FMS-SECRET',
    } as unknown as OPMSTarget} canReadInternalReference={false} canReadFmsLink={false} />);

    expect(screen.queryByLabelText('Internal Reference')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('FMS Link')).not.toBeInTheDocument();
    expect(screen.queryByDisplayValue('INTERNAL-SECRET')).not.toBeInTheDocument();
  });

  it('does not invent a percentage improvement when the baseline is zero', () => {
    render(<QuarterlyTargetsTab target={{
      ...liveTarget, annualTarget: 120, baseline: 0, q1Target: 25, q2Target: 30, midTermTarget: 55, q3Target: 30, q4Target: 35,
    } as OPMSTarget} />);

    expect(screen.getByText('Not calculable')).toBeInTheDocument();
    expect(screen.queryByText('12000.0%')).not.toBeInTheDocument();
    expect(screen.queryByText('Revised')).not.toBeInTheDocument();
  });

  it('pages target submissions and linked IPMS records with authoritative totals', async () => {
    window.history.replaceState({}, '', '/opms/targets/target-public');
    const detailTarget = {
      ...liveTarget,
      id: 'target-public',
      period: { id: 'period-1', name: 'Quarter 1' },
      unit: null,
      indicatorNumber: 'OPMS-1', targetName: 'Water KPI', kpiDescription: 'Description', targetUnitType: 'percentage',
      unitOfMeasure: { id: 'unit-1', name: 'Percent' }, kpiType: 'Quantitative', indicatorType: 'Output', annualTarget: 100, baseline: 50, weight: 10,
    } as unknown as OPMSTarget;
    api.getOpmsTarget.mockResolvedValue({ success: true, data: detailTarget });
    api.getOpmsSubmissionsPage.mockImplementation(async ({ page }: { page: number }) => ({ success: true, data: {
      items: page === 1 ? [{ id: 'submission-qualitative', target: detailTarget, quarter: 'Q1', dueDate: '2026-09-30T00:00:00Z', actualPerformance: 'Service restored', status: 'submitted' }] : [],
      page, pageSize: 25, totalCount: 26, totalPages: 2,
    } }));
    api.getIpmsTargetsPage.mockImplementation(async ({ page }: { page: number }) => ({ success: true, data: { items: [], page, pageSize: 25, totalCount: 27, totalPages: 2 } }));
    api.getAuditTrailsPage.mockImplementation(async ({ page }: { page: number }) => ({ success: true, data: { items: [], page, pageSize: 25, totalCount: 26, totalPages: 2 } }));

    render(<OPMSTargetDetail targetId="target-public" />);
    await waitFor(() => expect(api.getOpmsSubmissionsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, targetPublicId: 'target-public' }));
    await waitFor(() => expect(api.getIpmsTargetsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, relatedOpmsTargetPublicId: 'target-public' }));
    await waitFor(() => expect(api.getAuditTrailsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc' }, { entityName: 'OpmsTarget', entityId: 'target-public' }));

    fireEvent.click(await screen.findByRole('button', { name: 'Reporting 26' }));
    expect(screen.getByText('26 submissions · Page 1 of 2')).toBeInTheDocument();
    expect(screen.getByText('Service restored')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next submissions' }));
    await waitFor(() => expect(api.getOpmsSubmissionsPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, targetPublicId: 'target-public' }));

    fireEvent.click(screen.getByRole('button', { name: 'KPI Details' }));
    fireEvent.click(screen.getByRole('button', { name: 'IPMS 27' }));
    expect(screen.getByText('27 linked targets · Page 1 of 2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next linked targets' }));
    await waitFor(() => expect(api.getIpmsTargetsPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, relatedOpmsTargetPublicId: 'target-public' }));

    fireEvent.click(screen.getByRole('button', { name: 'History' }));
    expect(screen.getByText('26 audit entries · Page 1 of 2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next audit entries' }));
    await waitFor(() => expect(api.getAuditTrailsPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc' }, { entityName: 'OpmsTarget', entityId: 'target-public' }));
  });
});
