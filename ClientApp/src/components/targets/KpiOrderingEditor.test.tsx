import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { KpiOrderingEditor } from './KpiOrderingEditor';

const api = vi.hoisted(() => ({
  getIpmsTargetOrderingRevisions: vi.fn(),
  getOpmsTargetOrderingRevisions: vi.fn(),
  reviseIpmsTargetOrdering: vi.fn(),
  reviseOpmsTargetOrdering: vi.fn(),
}));

vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canUpdate: () => true }) }));

describe('KpiOrderingEditor', () => {
  beforeEach(() => {
    api.getOpmsTargetOrderingRevisions.mockResolvedValue({ success: true, data: [{
      publicId: 'revision-1', fieldName: 'RevisedOrderNumber', originalValue: '9', revisedValue: '2', reason: 'Approved resequencing', approvalReference: 'COUNCIL-1', effectiveAt: '2026-10-01T00:00:00Z', revisedByUserId: 'user', recordedAt: '2026-10-01T00:00:00Z',
    }] });
    api.reviseOpmsTargetOrdering.mockResolvedValue({ success: true, data: { originalOrderNumber: 3, revisedOrderNumber: 1, rowVersion: 'Ag==' } });
  });

  it('explains period rules and submits a governed ordering revision', async () => {
    const onUpdated = vi.fn();
    render(<KpiOrderingEditor kind="opms" targetId="target-1" originalOrderNumber={3} revisedOrderNumber={2} rowVersion="AQ==" onUpdated={onUpdated} />);

    expect(screen.getByText(/Q1, Q2 and Mid-Term use original order/)).toBeInTheDocument();
    expect(await screen.findByText(/COUNCIL-1/)).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Revised order number'), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText(/Approval reference/), { target: { value: 'COUNCIL-2' } });
    fireEvent.change(screen.getByLabelText(/Revision reason/), { target: { value: 'Final sequence' } });
    fireEvent.click(screen.getByRole('button', { name: 'Record ordering revision' }));

    await waitFor(() => expect(api.reviseOpmsTargetOrdering).toHaveBeenCalledWith('target-1', expect.objectContaining({ originalOrderNumber: 3, revisedOrderNumber: 1, approvalReference: 'COUNCIL-2', reason: 'Final sequence', rowVersion: 'AQ==' })));
    expect(onUpdated).toHaveBeenCalledWith({ originalOrderNumber: 3, revisedOrderNumber: 1, rowVersion: 'Ag==' });
  });
});
