import { useCallback, useEffect, useMemo, useState } from 'react';
import { ShieldCheck } from 'lucide-react';
import { Button } from '../ui';
import { Checkbox, Input, Select } from '../common/Form';
import {
  getAuthenticationConfiguration, getAuthenticationConfigurationHistoryPage, getAuthenticationEventsPage, getAuthenticationProviders, getSecurityUsersPage,
  getUserAuthenticatorsPage, provisionUserAuthenticator, saveAuthenticationConfiguration, setUserAuthenticatorStatus,
} from '../../api/api';
import type { AuthenticationConfiguration, AuthenticationEvent, EnterpriseProviderOption, SecurityUserSummary, UserAuthenticator } from '../../types';
import { useSecurity } from '../../context/SecurityContext';

const nowLocal = () => new Date(Date.now() - new Date().getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
const successorLocal = (previous: string) => {
  const candidate = Math.ceil(Math.max(Date.now(), new Date(previous).getTime() + 60_000) / 60_000) * 60_000;
  return new Date(candidate - new Date(candidate).getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
};
const modeOptions = [
  { value: 1, label: 'Local passwords' }, { value: 2, label: 'Microsoft Entra ID' },
  { value: 3, label: 'Active Directory federation' }, { value: 4, label: 'Hybrid' },
];

export function AuthenticationAdministrationPage() {
  const security = useSecurity();
  const canReadUserEmail = security.canReadField('AUTHENTICATION', 'UserEmail');
  const canReadExpectedEmail = security.canReadField('AUTHENTICATION', 'ExpectedEmail');
  const canEditExpectedEmail = security.canEditField('AUTHENTICATION', 'ExpectedEmail');
  const canReadEventUser = security.canReadField('AUTHENTICATION', 'EventUserId');
  const canReadEventIp = security.canReadField('AUTHENTICATION', 'EventIpAddress');
  const [configuration, setConfiguration] = useState<AuthenticationConfiguration | null>(null);
  const [configurationVersions, setConfigurationVersions] = useState<AuthenticationConfiguration[]>([]);
  const [configurationVersionPage, setConfigurationVersionPage] = useState(1);
  const [configurationVersionTotalCount, setConfigurationVersionTotalCount] = useState(0);
  const [configurationVersionTotalPages, setConfigurationVersionTotalPages] = useState(0);
  const [configurationVersionSearch, setConfigurationVersionSearch] = useState('');
  const [providers, setProviders] = useState<EnterpriseProviderOption[]>([]);
  const [users, setUsers] = useState<SecurityUserSummary[]>([]);
  const [userPage, setUserPage] = useState(1);
  const [userTotalPages, setUserTotalPages] = useState(0);
  const [userSearch, setUserSearch] = useState('');
  const [authenticators, setAuthenticators] = useState<UserAuthenticator[]>([]);
  const [authenticatorPage, setAuthenticatorPage] = useState(1);
  const [authenticatorTotalCount, setAuthenticatorTotalCount] = useState(0);
  const [authenticatorTotalPages, setAuthenticatorTotalPages] = useState(0);
  const [authenticatorSearch, setAuthenticatorSearch] = useState('');
  const [authenticatorStatus, setAuthenticatorStatus] = useState<'all' | 'active' | 'inactive'>('all');
  const [authenticatorRevision, setAuthenticatorRevision] = useState(0);
  const [events, setEvents] = useState<AuthenticationEvent[]>([]);
  const [eventPage, setEventPage] = useState(1);
  const [eventTotalCount, setEventTotalCount] = useState(0);
  const [eventTotalPages, setEventTotalPages] = useState(0);
  const [eventSearch, setEventSearch] = useState('');
  const [eventResult, setEventResult] = useState<'all' | 'success' | 'failure'>('all');
  const [mode, setMode] = useState<1 | 2 | 3 | 4>(1);
  const [providerCode, setProviderCode] = useState('');
  const [displayName, setDisplayName] = useState('Municipal sign-in');
  const [effectiveFrom, setEffectiveFrom] = useState(nowLocal);
  const [minimumPasswordLength, setMinimumPasswordLength] = useState(12);
  const [maximumFailedAttempts, setMaximumFailedAttempts] = useState(5);
  const [lockoutMinutes, setLockoutMinutes] = useState(15);
  const [requireMfaForPrivilegedLocalUsers, setRequireMfaForPrivilegedLocalUsers] = useState(true);
  const [requireMfaForAllLocalUsers, setRequireMfaForAllLocalUsers] = useState(false);
  const [requireFirstLoginPasswordChange, setRequireFirstLoginPasswordChange] = useState(true);
  const [sessionIdleTimeoutMinutes, setSessionIdleTimeoutMinutes] = useState(30);
  const [sessionAbsoluteTimeoutHours, setSessionAbsoluteTimeoutHours] = useState(24);
  const [maximumConcurrentSessions, setMaximumConcurrentSessions] = useState(5);
  const [reason, setReason] = useState('');
  const [selectedUser, setSelectedUser] = useState('');
  const [linkReason, setLinkReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');

  const load = useCallback(async () => {
    const [configResult, providerResult] = await Promise.all([
      getAuthenticationConfiguration(), getAuthenticationProviders(),
    ]);
    const config = configResult.data ?? null;
    setConfiguration(config); setProviders(providerResult.data ?? []);
    if (config) {
      setMode(config.mode); setProviderCode(config.providerRegistrationCode ?? ''); setDisplayName(config.displayName);
      setEffectiveFrom(successorLocal(config.effectiveFrom));
      if (config.policy) {
        setMinimumPasswordLength(config.policy.minimumPasswordLength);
        setMaximumFailedAttempts(config.policy.maximumFailedAttempts);
        setLockoutMinutes(config.policy.lockoutMinutes);
        setRequireMfaForPrivilegedLocalUsers(config.policy.requireMfaForPrivilegedLocalUsers);
        setRequireMfaForAllLocalUsers(config.policy.requireMfaForAllLocalUsers);
        setRequireFirstLoginPasswordChange(config.policy.requireFirstLoginPasswordChange);
        setSessionIdleTimeoutMinutes(config.policy.sessionIdleTimeoutMinutes);
        setSessionAbsoluteTimeoutHours(config.policy.sessionAbsoluteTimeoutHours);
        setMaximumConcurrentSessions(config.policy.maximumConcurrentSessions);
      }
    }
  }, []);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    const timeout = window.setTimeout(() => {
      void getAuthenticationConfigurationHistoryPage({
        page: configurationVersionPage, pageSize: 10, search: configurationVersionSearch,
        sortBy: 'version', sortDirection: 'desc',
      }).then(result => {
        setConfigurationVersions(result.data?.items ?? []);
        setConfigurationVersionTotalCount(result.data?.totalCount ?? 0);
        setConfigurationVersionTotalPages(result.data?.totalPages ?? 0);
      });
    }, 250);
    return () => window.clearTimeout(timeout);
  }, [configurationVersionPage, configurationVersionSearch]);
  useEffect(() => {
    const timeout = window.setTimeout(() => {
      void getUserAuthenticatorsPage({
        page: authenticatorPage, pageSize: 25, search: authenticatorSearch,
        sortBy: canReadExpectedEmail ? 'email' : 'createdAt', sortDirection: 'asc',
        active: authenticatorStatus === 'all' ? undefined : authenticatorStatus === 'active',
      }).then(result => {
        setAuthenticators(result.data?.items ?? []);
        setAuthenticatorTotalCount(result.data?.totalCount ?? 0);
        setAuthenticatorTotalPages(result.data?.totalPages ?? 0);
      });
    }, 250);
    return () => window.clearTimeout(timeout);
  }, [authenticatorPage, authenticatorRevision, authenticatorSearch, authenticatorStatus, canReadExpectedEmail]);
  useEffect(() => {
    const timeout = window.setTimeout(() => {
      void getAuthenticationEventsPage({
        page: eventPage, pageSize: 25, search: eventSearch, sortBy: 'occurredAt', sortDirection: 'desc',
        success: eventResult === 'all' ? undefined : eventResult === 'success',
      }).then(result => {
        setEvents(result.data?.items ?? []);
        setEventTotalCount(result.data?.totalCount ?? 0);
        setEventTotalPages(result.data?.totalPages ?? 0);
      });
    }, 250);
    return () => window.clearTimeout(timeout);
  }, [eventPage, eventResult, eventSearch]);
  useEffect(() => {
    void getSecurityUsersPage({ page: userPage, pageSize: 25, search: userSearch, sortBy: 'name', sortDirection: 'asc' }).then(result => {
      setUsers(result.data?.items ?? []);
      setUserTotalPages(result.data?.totalPages ?? 0);
    });
  }, [userPage, userSearch]);
  const selectedUserRecord = useMemo(() => users.find(item => item.publicId === selectedUser), [selectedUser, users]);

  const save = async () => {
    setBusy(true); setMessage('');
    const result = await saveAuthenticationConfiguration({
      mode, providerRegistrationCode: mode === 1 ? null : providerCode, displayName, isActive: true,
      effectiveFrom: new Date(effectiveFrom).toISOString(), effectiveTo: null, reason, rowVersion: configuration?.rowVersion ?? null,
      policy: {
        minimumPasswordLength, maximumFailedAttempts, lockoutMinutes,
        requireMfaForPrivilegedLocalUsers, requireMfaForAllLocalUsers, requireFirstLoginPasswordChange,
        sessionIdleTimeoutMinutes, sessionAbsoluteTimeoutHours, maximumConcurrentSessions,
        rowVersion: configuration?.policy?.rowVersion ?? null,
      },
    });
    setMessage(result.success ? 'Authentication configuration version created.' : result.message ?? 'Unable to save authentication configuration.');
    if (result.success) { setReason(''); setConfigurationVersionPage(1); await load(); }
    setBusy(false);
  };

  const provision = async () => {
    if (!selectedUserRecord?.email || !providerCode) return;
    setBusy(true); setMessage('');
    const result = await provisionUserAuthenticator({ userPublicId: selectedUserRecord.publicId, providerRegistrationCode: providerCode, expectedEmail: selectedUserRecord.email, reason: linkReason });
    setMessage(result.success ? 'Enterprise identity pre-provisioned.' : result.message ?? 'Unable to provision the identity.');
    if (result.success) { setLinkReason(''); setAuthenticatorPage(1); setAuthenticatorRevision(value => value + 1); }
    setBusy(false);
  };

  const toggle = async (item: UserAuthenticator) => {
    setBusy(true); setMessage('');
    const result = await setUserAuthenticatorStatus(item.publicId, !item.isActive, 'Administrator changed enterprise identity access', item.rowVersion);
    setMessage(result.success ? 'Identity status updated.' : result.message ?? 'Unable to update identity status.');
    if (result.success) setAuthenticatorRevision(value => value + 1);
    setBusy(false);
  };

  return <div className="p-6 space-y-6">
    <header className="flex items-center gap-3"><ShieldCheck className="w-7 h-7 text-primary-600" /><div><h1 className="text-2xl font-semibold">Authentication governance</h1><p className="text-sm text-secondary-500">Configure municipality sign-in modes and pre-provision external identities. Provider secrets remain deployment-managed.</p></div></header>
    {message ? <p role="status" className="rounded border border-secondary-200 p-3 text-sm">{message}</p> : null}
    <section className="rounded-xl border border-secondary-200 dark:border-secondary-700 p-5 space-y-4">
      <div className="flex items-center justify-between gap-3"><h2 className="font-semibold">Municipality authentication mode</h2>{configuration ? <span className="text-xs text-secondary-500">Current version {configuration.versionNumber}</span> : null}</div>
      <div className="grid md:grid-cols-2 gap-4">
        <Select label="Mode" options={modeOptions} value={mode} onChange={event => setMode(Number(event.target.value) as 1 | 2 | 3 | 4)} />
        <Select label="Enterprise provider" options={providers.map(item => ({ value: item.code, label: `${item.displayName} (${item.kind})` }))} placeholder="Select provider" value={providerCode} disabled={mode === 1} onChange={event => setProviderCode(event.target.value)} />
        <Input label="Display name" value={displayName} onChange={event => setDisplayName(event.target.value)} />
        <Input label={configuration ? 'New version effective from' : 'Effective from'} type="datetime-local" value={effectiveFrom} onChange={event => setEffectiveFrom(event.target.value)} />
      </div>
      <div className="border-t border-secondary-200 pt-4 dark:border-secondary-700">
        <h3 className="mb-3 text-sm font-semibold">Local authentication policy</h3>
        <div className="grid gap-4 md:grid-cols-3">
          <Input label="Minimum password length" type="number" min={12} max={128} value={minimumPasswordLength} onChange={event => setMinimumPasswordLength(Number(event.target.value))} />
          <Input label="Maximum failed attempts" type="number" min={1} max={20} value={maximumFailedAttempts} onChange={event => setMaximumFailedAttempts(Number(event.target.value))} />
          <Input label="Lockout duration (minutes)" type="number" min={1} max={1440} value={lockoutMinutes} onChange={event => setLockoutMinutes(Number(event.target.value))} />
          <Input label="Idle timeout (minutes)" type="number" min={5} max={1440} value={sessionIdleTimeoutMinutes} onChange={event => setSessionIdleTimeoutMinutes(Number(event.target.value))} />
          <Input label="Absolute timeout (hours)" type="number" min={1} max={720} value={sessionAbsoluteTimeoutHours} onChange={event => setSessionAbsoluteTimeoutHours(Number(event.target.value))} />
          <Input label="Maximum concurrent sessions" type="number" min={1} max={50} value={maximumConcurrentSessions} onChange={event => setMaximumConcurrentSessions(Number(event.target.value))} />
        </div>
        <div className="mt-4 grid gap-3 md:grid-cols-3">
          <Checkbox label="MFA for privileged local users" checked={requireMfaForPrivilegedLocalUsers} onChange={event => setRequireMfaForPrivilegedLocalUsers(event.target.checked)} />
          <Checkbox label="MFA for all local users" checked={requireMfaForAllLocalUsers} onChange={event => setRequireMfaForAllLocalUsers(event.target.checked)} />
          <Checkbox label="Password change on first login" checked={requireFirstLoginPasswordChange} onChange={event => setRequireFirstLoginPasswordChange(event.target.checked)} />
        </div>
      </div>
      <Input id="authentication-policy-reason" label="Governance reason" value={reason} onChange={event => setReason(event.target.value)} required />
      <Button onClick={save} loading={busy} disabled={busy || reason.trim().length < 5 || (mode !== 1 && !providerCode)}>{configuration ? 'Create configuration version' : 'Create configuration'}</Button>
    </section>
    <section className="rounded-xl border border-secondary-200 p-5 dark:border-secondary-700">
      <div className="flex flex-wrap items-end justify-between gap-3"><div><h2 className="font-semibold">Configuration version history</h2><p className="text-xs text-secondary-500">Append-preserved authentication modes and local security policies.</p></div><Input label="Search configuration versions" value={configurationVersionSearch} onChange={event => { setConfigurationVersionSearch(event.target.value); setConfigurationVersionPage(1); }} /></div>
      <p className="mt-3 text-xs text-secondary-500">{configurationVersionTotalCount} versions</p>
      <div className="mt-3 space-y-2">{configurationVersions.map(item => <div key={item.publicId} className="rounded border border-secondary-200 p-3 text-sm dark:border-secondary-700"><div className="flex justify-between gap-3"><strong>Version {item.versionNumber} · {modeOptions.find(option => option.value === item.mode)?.label ?? item.mode}</strong><span className="text-xs text-secondary-500">{item.isCurrent ? 'Current' : 'Historic'}</span></div><p className="text-xs text-secondary-500">{item.displayName} · effective {new Date(item.effectiveFrom).toLocaleString()} — {item.effectiveTo ? new Date(item.effectiveTo).toLocaleString() : 'open-ended'}</p><p className="mt-1 text-xs">Minimum password {item.policy?.minimumPasswordLength ?? '—'} · failed attempts {item.policy?.maximumFailedAttempts ?? '—'} · idle timeout {item.policy?.sessionIdleTimeoutMinutes ?? '—'} minutes</p></div>)}{!configurationVersions.length ? <p className="text-sm text-secondary-500">No authentication configuration versions.</p> : null}</div>
      {configurationVersionTotalPages > 1 ? <div className="mt-4 flex items-center justify-end gap-2 text-xs"><Button size="sm" variant="outline" disabled={configurationVersionPage <= 1} onClick={() => setConfigurationVersionPage(value => value - 1)}>Previous versions</Button><span>{configurationVersionPage}/{configurationVersionTotalPages}</span><Button size="sm" variant="outline" disabled={configurationVersionPage >= configurationVersionTotalPages} onClick={() => setConfigurationVersionPage(value => value + 1)}>Next versions</Button></div> : null}
    </section>
    <section className="rounded-xl border border-secondary-200 dark:border-secondary-700 p-5 space-y-4">
      <h2 className="font-semibold">Pre-provision enterprise identity</h2>
      <div className="grid md:grid-cols-2 gap-4">
        <Input label="Search users" value={userSearch} onChange={event => { setUserSearch(event.target.value); setUserPage(1); }} />
        <Select label="User" options={users.map(item => ({ value: item.publicId, label: item.email ? `${item.fullName} — ${item.email}` : item.fullName }))} placeholder="Select user" value={selectedUser} onChange={event => setSelectedUser(event.target.value)} />
        {canReadExpectedEmail ? <Input label="Verified email" value={selectedUserRecord?.email ?? ''} disabled /> : null}
      </div>
      {userTotalPages > 1 && <div className="flex items-center gap-2 text-xs text-secondary-500"><Button variant="outline" size="sm" disabled={userPage <= 1} onClick={() => setUserPage(value => Math.max(1, value - 1))}>Previous users</Button><span>Page {userPage} of {userTotalPages}</span><Button variant="outline" size="sm" disabled={userPage >= userTotalPages} onClick={() => setUserPage(value => value + 1)}>Next users</Button></div>}
      <Input id="identity-provisioning-reason" label="Governance reason" value={linkReason} onChange={event => setLinkReason(event.target.value)} required />
      <Button onClick={provision} loading={busy} disabled={busy || !canEditExpectedEmail || !selectedUserRecord?.email || !providerCode || linkReason.trim().length < 5}>Pre-provision identity</Button>
      {!canEditExpectedEmail ? <p className="text-xs text-secondary-500">Expected enterprise email is protected by member security.</p> : null}
      <div className="grid gap-4 border-t border-secondary-200 pt-4 dark:border-secondary-700 md:grid-cols-2">
        <Input label="Search enterprise identities" value={authenticatorSearch} onChange={event => { setAuthenticatorSearch(event.target.value); setAuthenticatorPage(1); }} />
        <Select label="Identity status" value={authenticatorStatus} options={[{ value: 'all', label: 'All statuses' }, { value: 'active', label: 'Active' }, { value: 'inactive', label: 'Disabled' }]} onChange={event => { setAuthenticatorStatus(event.target.value as 'all' | 'active' | 'inactive'); setAuthenticatorPage(1); }} />
      </div>
      <p className="text-xs text-secondary-500">{authenticatorTotalCount} enterprise identities</p>
      <div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="text-left">{canReadUserEmail ? <th className="p-2">User</th> : null}<th className="p-2">Provider</th><th className="p-2">Link state</th><th className="p-2">Status</th><th className="p-2">Action</th></tr></thead><tbody>{authenticators.map(item => <tr key={item.publicId} className="border-t">{canReadUserEmail ? <td className="p-2">{item.userEmail ?? 'Protected'}</td> : null}<td className="p-2">{item.providerRegistrationCode}</td><td className="p-2">{item.linkedAt ? 'Bound' : 'Awaiting first validated sign-in'}</td><td className="p-2">{item.isActive ? 'Active' : 'Disabled'}</td><td className="p-2"><Button variant="secondary" size="sm" onClick={() => toggle(item)} disabled={busy}>{item.isActive ? 'Disable' : 'Enable'}</Button></td></tr>)}</tbody></table></div>
      {authenticatorTotalPages > 1 && <div className="flex items-center justify-between gap-2 text-xs text-secondary-500"><span>Page {authenticatorPage} of {authenticatorTotalPages}</span><div className="flex gap-2"><Button variant="outline" size="sm" disabled={authenticatorPage <= 1} onClick={() => setAuthenticatorPage(value => Math.max(1, value - 1))}>Previous identities</Button><Button variant="outline" size="sm" disabled={authenticatorPage >= authenticatorTotalPages} onClick={() => setAuthenticatorPage(value => value + 1)}>Next identities</Button></div></div>}
    </section>
    <section className="space-y-4 rounded-xl border border-secondary-200 p-5 dark:border-secondary-700">
      <h2 className="font-semibold">Authentication events</h2>
      <div className="grid gap-4 md:grid-cols-2">
        <Input label="Search authentication events" value={eventSearch} onChange={event => { setEventSearch(event.target.value); setEventPage(1); }} />
        <Select label="Event result" value={eventResult} options={[{ value: 'all', label: 'All results' }, { value: 'success', label: 'Successful' }, { value: 'failure', label: 'Failed' }]} onChange={event => { setEventResult(event.target.value as 'all' | 'success' | 'failure'); setEventPage(1); }} />
      </div>
      <p className="text-xs text-secondary-500">{eventTotalCount} authentication events</p>
      <div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="text-left"><th className="p-2">Time</th><th className="p-2">Provider</th><th className="p-2">Event</th><th className="p-2">Result</th>{canReadEventUser ? <th className="p-2">User</th> : null}{canReadEventIp ? <th className="p-2">IP address</th> : null}<th className="p-2">Correlation</th></tr></thead><tbody>{events.map(item => <tr key={item.publicId} className="border-t"><td className="p-2">{new Date(item.occurredAt).toLocaleString()}</td><td className="p-2">{item.providerCode}</td><td className="p-2">{item.eventType}</td><td className="p-2">{item.success ? 'Success' : item.failureCode ?? 'Failed'}</td>{canReadEventUser ? <td className="p-2">{item.userPublicId ?? '-'}</td> : null}{canReadEventIp ? <td className="p-2">{item.ipAddress ?? '-'}</td> : null}<td className="p-2 font-mono text-xs">{item.correlationId}</td></tr>)}</tbody></table></div>
      {eventTotalPages > 1 && <div className="flex items-center justify-between gap-2 text-xs text-secondary-500"><span>Page {eventPage} of {eventTotalPages}</span><div className="flex gap-2"><Button variant="outline" size="sm" disabled={eventPage <= 1} onClick={() => setEventPage(value => Math.max(1, value - 1))}>Previous events</Button><Button variant="outline" size="sm" disabled={eventPage >= eventTotalPages} onClick={() => setEventPage(value => value + 1)}>Next events</Button></div></div>}
    </section>
  </div>;
}
