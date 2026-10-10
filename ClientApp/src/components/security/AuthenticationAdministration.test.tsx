import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { AuthenticationAdministrationPage } from './AuthenticationAdministration';

const api = vi.hoisted(() => ({
  getAuthenticationConfiguration: vi.fn(), getAuthenticationConfigurationHistoryPage: vi.fn(), getAuthenticationEventsPage: vi.fn(), getAuthenticationProviders: vi.fn(),
  getSecurityUsersPage: vi.fn(), getUserAuthenticatorsPage: vi.fn(), provisionUserAuthenticator: vi.fn(),
  saveAuthenticationConfiguration: vi.fn(), setUserAuthenticatorStatus: vi.fn(),
}));
const security = vi.hoisted(() => ({ canReadField: vi.fn(() => true), canEditField: vi.fn(() => true) }));
vi.mock('../../api/api', () => api);
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));

describe('AuthenticationAdministrationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canReadField.mockReturnValue(true);
    security.canEditField.mockReturnValue(true);
    api.getAuthenticationConfiguration.mockResolvedValue({ success: true, data: {
      publicId: 'configuration-1', configurationFamilyPublicId: 'family-1', versionNumber: 1, isCurrent: true, mode: 1, providerRegistrationCode: null, displayName: 'Local sign-in',
      isActive: true, effectiveFrom: '2026-10-01T08:00:00Z', effectiveTo: null, rowVersion: 'AQ==',
      policy: {
        publicId: 'policy-1', minimumPasswordLength: 14, maximumFailedAttempts: 4, lockoutMinutes: 30,
        requireMfaForPrivilegedLocalUsers: true, requireMfaForAllLocalUsers: false,
        requireFirstLoginPasswordChange: true, sessionIdleTimeoutMinutes: 20,
        sessionAbsoluteTimeoutHours: 12, maximumConcurrentSessions: 3, rowVersion: 'Ag==',
      },
    } });
    api.getAuthenticationConfigurationHistoryPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'configuration-1', configurationFamilyPublicId: 'family-1', versionNumber: 1, isCurrent: true,
      mode: 1, providerRegistrationCode: null, displayName: 'Local sign-in', isActive: true,
      effectiveFrom: '2026-10-01T08:00:00Z', effectiveTo: null, rowVersion: 'AQ==',
      policy: { minimumPasswordLength: 14, maximumFailedAttempts: 4, sessionIdleTimeoutMinutes: 20 },
    }], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });
    api.getAuthenticationEventsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'event-1', userPublicId: '44444444-4444-4444-4444-444444444444', providerCode: 'ENTRA', eventType: 'ExternalSignIn', success: true, occurredAt: '2026-10-01T09:00:00Z', correlationId: 'trace-1' }], page: 1, pageSize: 25, totalCount: 31, totalPages: 2 } });
    api.getAuthenticationProviders.mockResolvedValue({ success: true, data: [] });
    api.getSecurityUsersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 } });
    api.getUserAuthenticatorsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'authenticator-1', userPublicId: 'user-public-1', userEmail: 'person@example.test', providerRegistrationCode: 'ENTRA', expectedEmail: 'person@example.test', isActive: true, rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 31, totalPages: 2 } });
    api.saveAuthenticationConfiguration.mockResolvedValue({ success: true, data: {} });
  });

  it('loads and saves editable municipality policy values', async () => {
    render(<AuthenticationAdministrationPage />);
    const passwordLength = await screen.findByLabelText('Minimum password length');
    await waitFor(() => expect(passwordLength).toHaveValue(14));
    fireEvent.change(passwordLength, { target: { value: '18' } });
    fireEvent.click(screen.getByRole('checkbox', { name: 'MFA for all local users' }));
    fireEvent.change(screen.getByLabelText(/Governance reason/, { selector: '#authentication-policy-reason' }), { target: { value: 'Approved security policy update' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create configuration version' }));

    await waitFor(() => expect(api.saveAuthenticationConfiguration).toHaveBeenCalledWith(expect.objectContaining({
      reason: 'Approved security policy update',
      policy: expect.objectContaining({ minimumPasswordLength: 18, requireMfaForAllLocalUsers: true, rowVersion: 'Ag==' }),
    })));
  });

  it('loads bounded identity and event pages and transports search filters', async () => {
    render(<AuthenticationAdministrationPage />);

    expect(await screen.findByText('31 enterprise identities')).toBeInTheDocument();
    expect(await screen.findByText('31 authentication events')).toBeInTheDocument();
    expect(screen.getByText('44444444-4444-4444-4444-444444444444')).toBeInTheDocument();
    expect(api.getUserAuthenticatorsPage).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25, sortBy: 'email' }));
    expect(api.getAuthenticationEventsPage).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25, sortBy: 'occurredAt' }));

    fireEvent.change(screen.getByLabelText('Search enterprise identities'), { target: { value: 'person' } });
    fireEvent.change(screen.getByLabelText('Identity status'), { target: { value: 'inactive' } });
    fireEvent.change(screen.getByLabelText('Search authentication events'), { target: { value: 'denied' } });
    fireEvent.change(screen.getByLabelText('Event result'), { target: { value: 'failure' } });

    await waitFor(() => expect(api.getUserAuthenticatorsPage).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'person', active: false })));
    await waitFor(() => expect(api.getAuthenticationEventsPage).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'denied', success: false })));
  });

  it('hides protected authentication members and avoids sensitive email sorting without grants', async () => {
    security.canReadField.mockReturnValue(false);
    security.canEditField.mockReturnValue(false);
    render(<AuthenticationAdministrationPage />);

    expect(await screen.findByText('31 enterprise identities')).toBeInTheDocument();
    expect(api.getUserAuthenticatorsPage).toHaveBeenCalledWith(expect.objectContaining({ sortBy: 'createdAt' }));
    expect(screen.queryByRole('columnheader', { name: 'User' })).not.toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: 'IP address' })).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Verified email')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Pre-provision identity' })).toBeDisabled();
    expect(screen.getByText('Expected enterprise email is protected by member security.')).toBeInTheDocument();
  });
});
