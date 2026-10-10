import { fireEvent, render, screen } from '@testing-library/react';
import { FileUpload } from './FileUpload';

describe('FileUpload evidence scan state', () => {
  it('shows quarantine state and exposes governed rescan without a download', () => {
    const onRescan = vi.fn();
    render(<FileUpload onRescan={onRescan} existingFiles={[{
      id: 'evidence-1', name: 'proof.pdf', size: 128, type: 'application/pdf', progress: 100,
      scanStatus: 'ScannerUnavailable', isQuarantined: true, scanDetail: 'Scanner could not be reached.', url: '',
    }]} />);

    expect(screen.getByText(/Malware scan: ScannerUnavailable/)).toBeInTheDocument();
    expect(screen.queryByTitle('Download')).not.toBeInTheDocument();
    fireEvent.click(screen.getByTitle('Rescan quarantined evidence'));
    expect(onRescan).toHaveBeenCalledWith('evidence-1');
  });

  it('records a governed assessment and renders immutable history', () => {
    const onAssess = vi.fn();
    render(<FileUpload onAssess={onAssess} existingFiles={[{
      id: 'evidence-2', name: 'register.pdf', size: 256, type: 'application/pdf', progress: 100,
      scanStatus: 'Clean', isQuarantined: false, url: '/content', assessments: [{ publicId: 'assessment-1', outcome: 'Accepted', comment: 'Verified against source register.', assessedByUserPublicId: '11111111-1111-1111-1111-111111111111', assessedByName: 'Internal Auditor', assessedAt: '2026-10-02T00:00:00Z', correlationId: 'trace-1' }],
    }]} />);

    expect(screen.getByText(/Internal Auditor/)).toHaveTextContent('Accepted');
    fireEvent.change(screen.getByLabelText('Assessment outcome for register.pdf'), { target: { value: '3' } });
    fireEvent.change(screen.getByLabelText('Assessment comment for register.pdf'), { target: { value: 'Provide signed minutes.' } });
    fireEvent.click(screen.getByRole('button', { name: 'Record assessment' }));
    expect(onAssess).toHaveBeenCalledWith('evidence-2', 3, 'Provide signed minutes.');
  });

  it('records concurrency-protected replacement provenance between released files', () => {
    const onReplace = vi.fn();
    render(<FileUpload onReplace={onReplace} existingFiles={[
      { id: 'old-id', publicId: 'old-public', rowVersion: 'AAAAAAAAAAE=', name: 'old.pdf', size: 100, type: 'application/pdf', progress: 100, scanStatus: 'Clean', isQuarantined: false },
      { id: 'new-id', publicId: 'new-public', rowVersion: 'AAAAAAAAAAI=', name: 'new.pdf', size: 120, type: 'application/pdf', progress: 100, scanStatus: 'Clean', isQuarantined: false },
    ]} />);

    fireEvent.change(screen.getByLabelText('Replacement evidence for old.pdf'), { target: { value: 'new-public' } });
    fireEvent.change(screen.getByLabelText('Replacement reason for old.pdf'), { target: { value: 'Corrected signed version' } });
    fireEvent.click(screen.getByLabelText('Record replacement for old.pdf'));
    expect(onReplace).toHaveBeenCalledWith('old-id', 'new-public', 'Corrected signed version', 'AAAAAAAAAAE=', 'AAAAAAAAAAI=');
  });

  it('places and releases legal holds while retaining the displayed history', () => {
    const onPlaceHold = vi.fn();
    const onReleaseHold = vi.fn();
    render(<FileUpload onPlaceHold={onPlaceHold} onReleaseHold={onReleaseHold} existingFiles={[{
      id: 'evidence-3', name: 'contract.pdf', size: 200, type: 'application/pdf', progress: 100,
      legalHolds: [{ holdId: 'hold-1', holdReference: 'CASE-2026-1', isActive: true, placedReason: 'Pending investigation', placedByUserPublicId: '22222222-2222-2222-2222-222222222222', placedAt: '2026-10-01T00:00:00Z' }],
    }]} />);

    expect(screen.getByText(/Active legal hold/)).toHaveTextContent('CASE-2026-1');
    fireEvent.change(screen.getByLabelText('Legal hold release reason for contract.pdf CASE-2026-1'), { target: { value: 'Matter concluded' } });
    fireEvent.click(screen.getByRole('button', { name: 'Release hold' }));
    expect(onReleaseHold).toHaveBeenCalledWith('evidence-3', 'hold-1', 'Matter concluded');

    fireEvent.change(screen.getByLabelText('Legal hold reference for contract.pdf'), { target: { value: 'CASE-2026-2' } });
    fireEvent.change(screen.getByLabelText('Legal hold reason for contract.pdf'), { target: { value: 'New preservation notice' } });
    fireEvent.click(screen.getByLabelText('Place legal hold on contract.pdf'));
    expect(onPlaceHold).toHaveBeenCalledWith('evidence-3', 'CASE-2026-2', 'New preservation notice');
  });

  it('queues disposal only for retired evidence after retention and displays provenance', () => {
    const onDispose = vi.fn();
    render(<FileUpload onDispose={onDispose} existingFiles={[{
      id: 'evidence-4', name: 'expired.pdf', size: 200, type: 'application/pdf', progress: 100,
      isActive: false, retainUntil: '2020-01-01T00:00:00Z', rowVersion: 'AAAAAAAAAAE=',
      disposals: [{ disposalId: 'failed-1', status: 'Failed', approvalReference: 'COUNCIL-OLD', reason: 'Prior attempt', requestedByUserPublicId: '33333333-3333-3333-3333-333333333333', requestedAt: '2026-10-01T00:00:00Z', failedAt: '2026-10-01T00:01:00Z', detail: 'Storage unavailable' }],
    }]} />);

    expect(screen.getByText(/COUNCIL-OLD/)).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Disposal approval reference for expired.pdf'), { target: { value: 'COUNCIL-2026-42' } });
    fireEvent.change(screen.getByLabelText('Disposal reason for expired.pdf'), { target: { value: 'Retention period completed' } });
    fireEvent.click(screen.getByLabelText('Request disposal for expired.pdf'));
    expect(onDispose).toHaveBeenCalledWith('evidence-4', 'COUNCIL-2026-42', 'Retention period completed', 'AAAAAAAAAAE=');
  });
});
