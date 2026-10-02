import { useCallback, useEffect, useMemo, useState } from 'react';
import { ShieldCheck } from 'lucide-react';
import { Button } from '../ui';
import { Input, Select } from '../common/Form';
import {
  getAuthenticationConfiguration, getAuthenticationEvents, getAuthenticationProviders, getSecurityUsers,
  getUserAuthenticators, provisionUserAuthenticator, saveAuthenticationConfiguration, setUserAuthenticatorStatus,
} from '../../api/api';
import type { AuthenticationConfiguration, AuthenticationEvent, EnterpriseProviderOption, SecurityUserSummary, UserAuthenticator } from '../../types';

const nowLocal = () => new Date(Date.now() - new Date().getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
const modeOptions = [
  { value: 1, label: 'Local passwords' }, { value: 2, label: 'Microsoft Entra ID' },
  { value: 3, label: 'Active Directory federation' }, { value: 4, label: 'Hybrid' },
];

export function AuthenticationAdministrationPage() {
  const [configuration, setConfiguration] = useState<AuthenticationConfiguration | null>(null);
  const [providers, setProviders] = useState<EnterpriseProviderOption[]>([]);
  const [users, setUsers] = useState<SecurityUserSummary[]>([]);
  const [authenticators, setAuthenticators] = useState<UserAuthenticator[]>([]);
  const [events, setEvents] = useState<AuthenticationEvent[]>([]);
  const [mode, setMode] = useState<1 | 2 | 3 | 4>(1);
  const [providerCode, setProviderCode] = useState('');
  const [displayName, setDisplayName] = useState('Municipal sign-in');
  const [effectiveFrom, setEffectiveFrom] = useState(nowLocal);
  const [reason, setReason] = useState('');
  const [selectedUser, setSelectedUser] = useState('');
  const [linkReason, setLinkReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');

  const load = useCallback(async () => {
    const [configResult, providerResult, userResult, authenticatorResult, eventResult] = await Promise.all([
      getAuthenticationConfiguration(), getAuthenticationProviders(), getSecurityUsers(), getUserAuthenticators(), getAuthenticationEvents(),
    ]);
    const config = configResult.data ?? null;
    setConfiguration(config); setProviders(providerResult.data ?? []); setUsers(userResult.data ?? []);
    setAuthenticators(authenticatorResult.data ?? []); setEvents(eventResult.data ?? []);
    if (config) {
      setMode(config.mode); setProviderCode(config.providerRegistrationCode ?? ''); setDisplayName(config.displayName);
      setEffectiveFrom(new Date(new Date(config.effectiveFrom).getTime() - new Date(config.effectiveFrom).getTimezoneOffset() * 60_000).toISOString().slice(0, 16));
    }
  }, []);

  useEffect(() => { void load(); }, [load]);
  const selectedUserRecord = useMemo(() => users.find(item => item.id === selectedUser), [selectedUser, users]);

  const save = async () => {
    setBusy(true); setMessage('');
    const existingPolicy = configuration?.policy;
    const result = await saveAuthenticationConfiguration({
      mode, providerRegistrationCode: mode === 1 ? null : providerCode, displayName, isActive: true,
      effectiveFrom: new Date(effectiveFrom).toISOString(), effectiveTo: null, reason, rowVersion: configuration?.rowVersion ?? null,
      policy: {
        minimumPasswordLength: existingPolicy?.minimumPasswordLength ?? 12,
        maximumFailedAttempts: existingPolicy?.maximumFailedAttempts ?? 5,
        lockoutMinutes: existingPolicy?.lockoutMinutes ?? 15,
        requireMfaForPrivilegedLocalUsers: existingPolicy?.requireMfaForPrivilegedLocalUsers ?? true,
        requireMfaForAllLocalUsers: existingPolicy?.requireMfaForAllLocalUsers ?? false,
        requireFirstLoginPasswordChange: existingPolicy?.requireFirstLoginPasswordChange ?? true,
        sessionIdleTimeoutMinutes: existingPolicy?.sessionIdleTimeoutMinutes ?? 30,
        sessionAbsoluteTimeoutHours: existingPolicy?.sessionAbsoluteTimeoutHours ?? 24,
        maximumConcurrentSessions: existingPolicy?.maximumConcurrentSessions ?? 5,
        rowVersion: existingPolicy?.rowVersion ?? null,
      },
    });
    setMessage(result.success ? 'Authentication configuration saved.' : result.message ?? 'Unable to save authentication configuration.');
    if (result.success) { setReason(''); await load(); }
    setBusy(false);
  };

  const provision = async () => {
    if (!selectedUserRecord || !providerCode) return;
    setBusy(true); setMessage('');
    const result = await provisionUserAuthenticator({ userId: selectedUserRecord.id, providerRegistrationCode: providerCode, expectedEmail: selectedUserRecord.email, reason: linkReason });
    setMessage(result.success ? 'Enterprise identity pre-provisioned.' : result.message ?? 'Unable to provision the identity.');
    if (result.success) { setLinkReason(''); await load(); }
    setBusy(false);
  };

  const toggle = async (item: UserAuthenticator) => {
    setBusy(true); setMessage('');
    const result = await setUserAuthenticatorStatus(item.publicId, !item.isActive, 'Administrator changed enterprise identity access', item.rowVersion);
    setMessage(result.success ? 'Identity status updated.' : result.message ?? 'Unable to update identity status.');
    if (result.success) await load();
    setBusy(false);
  };

  return <div className="p-6 space-y-6">
    <header className="flex items-center gap-3"><ShieldCheck className="w-7 h-7 text-primary-600" /><div><h1 className="text-2xl font-semibold">Authentication governance</h1><p className="text-sm text-secondary-500">Configure municipality sign-in modes and pre-provision external identities. Provider secrets remain deployment-managed.</p></div></header>
    {message ? <p role="status" className="rounded border border-secondary-200 p-3 text-sm">{message}</p> : null}
    <section className="rounded-xl border border-secondary-200 dark:border-secondary-700 p-5 space-y-4">
      <h2 className="font-semibold">Municipality authentication mode</h2>
      <div className="grid md:grid-cols-2 gap-4">
        <Select label="Mode" options={modeOptions} value={mode} onChange={event => setMode(Number(event.target.value) as 1 | 2 | 3 | 4)} />
        <Select label="Enterprise provider" options={providers.map(item => ({ value: item.code, label: `${item.displayName} (${item.kind})` }))} placeholder="Select provider" value={providerCode} disabled={mode === 1} onChange={event => setProviderCode(event.target.value)} />
        <Input label="Display name" value={displayName} onChange={event => setDisplayName(event.target.value)} />
        <Input label="Effective from" type="datetime-local" value={effectiveFrom} onChange={event => setEffectiveFrom(event.target.value)} />
      </div>
      <Input label="Governance reason" value={reason} onChange={event => setReason(event.target.value)} required />
      <Button onClick={save} loading={busy} disabled={busy || reason.trim().length < 5 || (mode !== 1 && !providerCode)}>Save configuration</Button>
    </section>
    <section className="rounded-xl border border-secondary-200 dark:border-secondary-700 p-5 space-y-4">
      <h2 className="font-semibold">Pre-provision enterprise identity</h2>
      <div className="grid md:grid-cols-2 gap-4">
        <Select label="User" options={users.map(item => ({ value: item.id, label: `${item.fullName} — ${item.email}` }))} placeholder="Select user" value={selectedUser} onChange={event => setSelectedUser(event.target.value)} />
        <Input label="Verified email" value={selectedUserRecord?.email ?? ''} disabled />
      </div>
      <Input label="Governance reason" value={linkReason} onChange={event => setLinkReason(event.target.value)} required />
      <Button onClick={provision} loading={busy} disabled={busy || !selectedUserRecord || !providerCode || linkReason.trim().length < 5}>Pre-provision identity</Button>
      <div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="text-left"><th className="p-2">User</th><th className="p-2">Provider</th><th className="p-2">Link state</th><th className="p-2">Status</th><th className="p-2">Action</th></tr></thead><tbody>{authenticators.map(item => <tr key={item.publicId} className="border-t"><td className="p-2">{item.userEmail}</td><td className="p-2">{item.providerRegistrationCode}</td><td className="p-2">{item.linkedAt ? 'Bound' : 'Awaiting first validated sign-in'}</td><td className="p-2">{item.isActive ? 'Active' : 'Disabled'}</td><td className="p-2"><Button variant="secondary" size="sm" onClick={() => toggle(item)} disabled={busy}>{item.isActive ? 'Disable' : 'Enable'}</Button></td></tr>)}</tbody></table></div>
    </section>
    <section className="rounded-xl border border-secondary-200 dark:border-secondary-700 p-5"><h2 className="font-semibold mb-3">Recent authentication events</h2><div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="text-left"><th className="p-2">Time</th><th className="p-2">Provider</th><th className="p-2">Event</th><th className="p-2">Result</th><th className="p-2">Correlation</th></tr></thead><tbody>{events.map(item => <tr key={item.publicId} className="border-t"><td className="p-2">{new Date(item.occurredAt).toLocaleString()}</td><td className="p-2">{item.providerCode}</td><td className="p-2">{item.eventType}</td><td className="p-2">{item.success ? 'Success' : item.failureCode ?? 'Failed'}</td><td className="p-2 font-mono text-xs">{item.correlationId}</td></tr>)}</tbody></table></div></section>
  </div>;
}
