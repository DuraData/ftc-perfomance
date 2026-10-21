import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { RfiQueue } from './RfiQueue';

const api = vi.hoisted(() => ({ getRfiQueue: vi.fn() }));
const pushToast = vi.fn();

vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ currentMunicipalityPublicId: 'municipality-1', pushToast, userProfile: { publicId: 'user-1' } }) }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ title, children }: { title: string; children: React.ReactNode }) => <main><h1>{title}</h1>{children}</main> }));
vi.mock('../common/CalendarMasterPicker', () => ({ CalendarMasterPicker: ({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) => <label>{label}<input aria-label={label} value={value} onChange={event => onChange(event.target.value)} /></label> }));
vi.mock('./PerformanceRfiWorkspace', () => ({ PerformanceRfiWorkspace: ({ kind, submissionId, onChanged }: { kind: number; submissionId: string; onChanged: () => void }) => <div><p>Governed RFI workspace {kind}:{submissionId}</p><button type="button" onClick={onChanged}>Simulate RFI change</button></div> }));

const row = {
  publicId: '11111111-1111-4111-8111-111111111111',
  submissionPublicId: '22222222-2222-4222-8222-222222222222',
  kind: 'opms' as const,
  targetPublicId: '33333333-3333-4333-8333-333333333333',
  targetName: 'Reduce water losses',
  indicatorNumber: 'KPI-007',
  period: 'Q2',
  municipalityFinancialYearPublicId: '44444444-4444-4444-8444-444444444444',
  question: null,
  raisedAt: '2026-10-01T00:00:00Z',
  responseDueAt: '2026-10-24T00:00:00Z',
  status: 'open' as const,
  canRespond: true,
  canClose: false,
};

describe('RfiQueue', () => {
  const storageKey = 'workflow-rfi-queue-filters:user-1:municipality-1';
  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/workflow/rfis');
    api.getRfiQueue.mockResolvedValue({ success: true, data: { counts: { open: 1, responded: 2, overdue: 3, closed: 4 }, page: { items: [row], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } } });
  });

  it('renders masked authorised rows and opens the governed per-submission workspace', async () => {
    render(<RfiQueue />);

    expect(await screen.findByText('Reduce water losses')).toBeInTheDocument();
    expect(screen.getByText('Protected RFI question')).toBeInTheDocument();
    expect(screen.getByText('Ready to respond')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Open OPMS RFI for Reduce water losses' }));
    expect(screen.getByText('Governed RFI workspace 1:22222222-2222-4222-8222-222222222222')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Simulate RFI change' }));
    await waitFor(() => expect(api.getRfiQueue).toHaveBeenCalledTimes(2));
  });

  it('restores validated session filters and lets a valid deep link override framework and status', async () => {
    sessionStorage.setItem(storageKey, JSON.stringify({ search: 'stored', kind: 'ipms', period: 'Q3', status: 'closed', sortBy: 'targetName', sortDirection: 'desc', municipalityFinancialYearPublicId: 'not-a-public-id' }));
    window.history.replaceState({}, '', '/workflow/rfis?kind=opms&status=overdue&municipalityFinancialYearPublicId=55555555-5555-4555-8555-555555555555');

    render(<RfiQueue />);

    await waitFor(() => expect(api.getRfiQueue).toHaveBeenCalledWith(1, 25, expect.objectContaining({
      search: 'stored', kind: 'opms', period: 'Q3', status: 'overdue', sortBy: 'targetName', sortDirection: 'desc', municipalityFinancialYearPublicId: '55555555-5555-4555-8555-555555555555',
    })));
  });

  it('rejects malformed stored filters and persists controlled changes', async () => {
    sessionStorage.setItem(storageKey, JSON.stringify({ kind: 'secret', status: 'deleted', sortBy: 'question', sortDirection: 'sideways', municipalityFinancialYearPublicId: 'malformed' }));
    render(<RfiQueue />);

    await waitFor(() => expect(api.getRfiQueue).toHaveBeenCalledWith(1, 25, expect.objectContaining({ kind: 'all', status: 'all', sortBy: 'dueAt', sortDirection: 'asc', municipalityFinancialYearPublicId: '' })));
    fireEvent.change(screen.getByLabelText('RFI status'), { target: { value: 'responded' } });
    await waitFor(() => expect(JSON.parse(sessionStorage.getItem(storageKey) ?? '{}')).toEqual(expect.objectContaining({ status: 'responded' })));
  });
});
