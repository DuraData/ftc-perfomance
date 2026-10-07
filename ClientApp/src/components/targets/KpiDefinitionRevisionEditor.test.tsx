import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { KpiDefinitionRevisionEditor } from './KpiDefinitionRevisionEditor';
import type { OPMSTarget } from '../../types';

const api = vi.hoisted(() => ({
  getIpmsTargetFieldRevisionsPage: vi.fn(),
  getOpmsTargetFieldRevisionsPage: vi.fn(),
  reviseIpmsTargetDefinition: vi.fn(),
  reviseOpmsTargetDefinition: vi.fn(),
}));
const security = vi.hoisted(() => ({ canReadField: vi.fn(() => true) }));

vi.mock('../../api/api', () => api);
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canExecute: () => true, canReadField: security.canReadField }) }));

const target = {
  id: 'target-1', indicatorNumber: 'KPI-1', targetName: 'Original target', kpiDescription: 'Original wording',
  isIndicatorNumberRevised: false, revisedIndicatorNumber: undefined,
  isTargetNameRevised: false, revisedTargetName: undefined,
  isKpiDescriptionRevised: false, revisedKpiDescription: undefined,
  rowVersion: 'AQ==',
} as OPMSTarget;

describe('KpiDefinitionRevisionEditor', () => {
  beforeEach(() => {
    security.canReadField.mockReturnValue(true);
    api.getOpmsTargetFieldRevisionsPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'revision-1', fieldName: 'IndicatorNumber', originalValue: 'KPI-0', revisedValue: 'KPI-1',
      reason: 'Earlier approval', approvalReference: 'COUNCIL-1', effectiveAt: '2026-09-01T00:00:00Z',
      revisedByUserPublicId: 'user-public-id', revisedByName: 'Revision User', recordedAt: '2026-09-01T00:00:00Z',
    }], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });
    api.reviseOpmsTargetDefinition.mockResolvedValue({ success: true, data: { ...target, isIndicatorNumberRevised: true, revisedIndicatorNumber: 'KPI-2', rowVersion: 'Ag==' } });
  });

  it('fails closed against hostile revision-history payloads when member grants are absent', async () => {
    security.canReadField.mockReturnValue(false);

    render(<KpiDefinitionRevisionEditor kind="opms" target={target} onUpdated={vi.fn()} />);

    expect(await screen.findByText('IndicatorNumber')).toBeInTheDocument();
    expect(screen.queryByText(/KPI-0/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Revised: KPI-1/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Earlier approval/)).not.toBeInTheDocument();
    expect(screen.queryByText(/COUNCIL-1/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Revision User/)).not.toBeInTheDocument();
  });

  it('submits independent field flags and shows immutable approval history', async () => {
    const onUpdated = vi.fn();
    render(<KpiDefinitionRevisionEditor kind="opms" target={target} onUpdated={onUpdated} />);

    expect(screen.getByText(/Q1, Q2 and Mid-Term retain the originals/)).toBeInTheDocument();
    expect(await screen.findByText(/COUNCIL-1/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Revised KPI number' }));
    fireEvent.change(screen.getByRole('textbox', { name: 'Revised KPI number' }), { target: { value: 'KPI-2' } });
    fireEvent.change(screen.getByLabelText(/External approval reference/), { target: { value: 'COUNCIL-2' } });
    fireEvent.change(screen.getByLabelText(/Revision reason/), { target: { value: 'Externally approved number change' } });
    fireEvent.click(screen.getByRole('button', { name: 'Record approved revision' }));

    await waitFor(() => expect(api.reviseOpmsTargetDefinition).toHaveBeenCalledWith('target-1', expect.objectContaining({
      isIndicatorNumberRevised: true,
      revisedIndicatorNumber: 'KPI-2',
      isTargetNameRevised: false,
      isKpiDescriptionRevised: false,
      approvalReference: 'COUNCIL-2',
      reason: 'Externally approved number change',
      rowVersion: 'AQ==',
    })));
    expect(onUpdated).toHaveBeenCalledWith(expect.objectContaining({ revisedIndicatorNumber: 'KPI-2', rowVersion: 'Ag==' }));
  });
});
