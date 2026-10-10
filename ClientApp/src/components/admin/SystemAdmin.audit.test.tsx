import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { AdminAuditLogsPage } from './SystemAdmin';

const api = vi.hoisted(() => ({
  getLoginAuditLogs: vi.fn(),
  getAuditTrailsPage: vi.fn(),
}));
const security = vi.hoisted(() => ({
  canRead: vi.fn((resource: string) => Boolean(resource)),
  canReadField: vi.fn((resource: string, member: string) => Boolean(resource && member)),
}));

vi.mock('../../api/api', async importOriginal => ({
  ...(await importOriginal<typeof import('../../api/api')>()),
  ...api,
}));
vi.mock('../../context/AppContext', () => ({
  useApp: () => ({ permissions: ['Audit.LoginLogs.View', 'Audit.Trails.View'], pushToast: vi.fn() }),
}));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));

const login = {
  publicId: '22222222-2222-2222-2222-222222222222', userPublicId: '33333333-3333-3333-3333-333333333333', email: 'anna@example.test', ipAddress: '127.0.0.1', userAgent: 'test',
  success: false, failureReason: 'Account locked', loggedAt: '2026-01-03T00:00:00Z',
};
const trail = {
  publicId: '11111111-1111-1111-1111-111111111111', municipalityId: 1,
  entityName: 'OpmsSubmission', entityId: 'submission-a', action: 'Approve', changedBy: 'auditor-a',
  changedAt: '2026-01-03T00:00:00Z', ipAddress: '192.0.2.10', correlationId: 'correlation-a',
  oldValue: '{"secret":"before"}', newValue: '{"secret":"after"}',
};

describe('Audit administration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canRead.mockReturnValue(true);
    security.canReadField.mockReturnValue(true);
    api.getLoginAuditLogs.mockResolvedValue({ success: true, data: { items: [login], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    api.getAuditTrailsPage.mockResolvedValue({ success: true, data: { items: [trail], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
  });

  it('uses authoritative server paging, search, failure filtering, and audit tabs', async () => {
    render(<AdminAuditLogsPage />);

    expect(await screen.findByText('anna@example.test')).toBeInTheDocument();
    expect(api.getLoginAuditLogs).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'createdAt', sortDirection: 'desc' }, false);
    expect(screen.getByText('26 logs')).toBeInTheDocument();

    fireEvent.click(screen.getByLabelText('Next page'));
    await waitFor(() => expect(api.getLoginAuditLogs).toHaveBeenCalledWith(expect.objectContaining({ page: 2 }), false));

    fireEvent.change(screen.getByLabelText('Search login audit logs'), { target: { value: 'locked' } });
    await waitFor(() => expect(api.getLoginAuditLogs).toHaveBeenCalledWith(expect.objectContaining({ page: 1, search: 'locked' }), false));

    fireEvent.click(screen.getByLabelText('Failures only'));
    await waitFor(() => expect(api.getLoginAuditLogs).toHaveBeenCalledWith(expect.objectContaining({ page: 1, search: 'locked' }), true));

    fireEvent.click(screen.getByRole('button', { name: 'Audit Trails' }));
    expect(await screen.findByText('OpmsSubmission')).toBeInTheDocument();
    expect(api.getAuditTrailsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'createdAt', sortDirection: 'desc' });
  });

  it('hides protected login-audit members and uses a non-sensitive fallback sort when denied', async () => {
    security.canReadField.mockReturnValue(false);
    api.getLoginAuditLogs.mockResolvedValue({
      success: true,
      data: { items: [{ ...login, email: null, ipAddress: null, userAgent: null, failureReason: null }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });

    render(<AdminAuditLogsPage />);

    await waitFor(() => expect(api.getLoginAuditLogs).toHaveBeenCalledWith(
      { page: 1, pageSize: 25, search: '', sortBy: 'createdAt', sortDirection: 'desc' }, false,
    ));
    expect(screen.queryByRole('columnheader', { name: 'Email' })).not.toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: 'IP' })).not.toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: 'User Agent' })).not.toBeInTheDocument();
    expect(screen.queryByText('anna@example.test')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'View' }));
    expect(screen.queryByText('Failure Reason')).not.toBeInTheDocument();
    expect(screen.queryByText('IP Address')).not.toBeInTheDocument();
  });

  it('hides protected audit-trail members and never sends a denied actor sort', async () => {
    const { rerender } = render(<AdminAuditLogsPage />);
    await screen.findByText('anna@example.test');
    fireEvent.click(screen.getByRole('button', { name: 'Audit Trails' }));
    expect(await screen.findByText('OpmsSubmission')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /Changed By/ }));
    await waitFor(() => expect(api.getAuditTrailsPage).toHaveBeenLastCalledWith(
      expect.objectContaining({ sortBy: 'changedBy' }),
    ));

    security.canReadField.mockImplementation((resource: string) => resource !== 'AUDIT_TRAIL');
    api.getAuditTrailsPage.mockResolvedValue({
      success: true,
      data: {
        items: [{ ...trail, entityId: null, changedBy: null, ipAddress: null, oldValue: null, newValue: null }],
        page: 1, pageSize: 25, totalCount: 1, totalPages: 1,
      },
    });
    rerender(<AdminAuditLogsPage />);

    await waitFor(() => expect(api.getAuditTrailsPage).toHaveBeenLastCalledWith(
      { page: 1, pageSize: 25, search: '', sortBy: 'createdAt', sortDirection: 'desc' },
    ));
    expect(screen.queryByRole('columnheader', { name: 'Entity ID' })).not.toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: /Changed By/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: 'IP' })).not.toBeInTheDocument();
    expect(screen.queryByText('submission-a')).not.toBeInTheDocument();
    expect(screen.queryByText('auditor-a')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'View' }));
    expect(screen.queryByText('Entity ID')).not.toBeInTheDocument();
    expect(screen.queryByText('Changed By')).not.toBeInTheDocument();
    expect(screen.queryByText('IP Address')).not.toBeInTheDocument();
    expect(screen.queryByText('Old Value')).not.toBeInTheDocument();
    expect(screen.queryByText('New Value')).not.toBeInTheDocument();
  });
});
