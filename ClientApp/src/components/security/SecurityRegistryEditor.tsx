import { useEffect, useMemo, useState } from 'react';
import {
  createSecurityAction,
  createSecurityResource,
  getSecurityActionsPage,
  getSecurityMembersPage,
  getSecurityResourcesPage,
  updateSecurityAction,
  updateSecurityMember,
  updateSecurityResource,
} from '../../api/api';
import type {
  SecurityActionDefinitionDto,
  SecurityMemberDefinitionDto,
  SecurityResourceDefinitionDto,
} from '../../types';

type RegistryTab = 'resources' | 'actions' | 'members';
type ResourceDraft = Omit<SecurityResourceDefinitionDto, 'publicId' | 'rowVersion'> & { reason: string };
type ActionDraft = Pick<SecurityActionDefinitionDto, 'code' | 'name' | 'resourceCode' | 'description' | 'isActive'> & { reason: string };
type MemberDraft = Pick<SecurityMemberDefinitionDto, 'displayName' | 'isSensitive' | 'isActive'> & { reason: string };

const emptyResource: ResourceDraft = {
  code: '', name: '', type: 'ENTITY', description: '', canCreate: false, canRead: true, canUpdate: false,
  canDelete: false, canExport: false, canImport: false, supportsMembers: false, supportsCriteria: false,
  isActive: true, reason: '',
};
const emptyAction: ActionDraft = { code: '', name: '', resourceCode: '', description: '', isActive: true, reason: '' };

export function SecurityRegistryEditor({ onDefinitionsChanged }: { onDefinitionsChanged?: () => void }) {
  const [tab, setTab] = useState<RegistryTab>('resources');
  const [resources, setResources] = useState<SecurityResourceDefinitionDto[]>([]);
  const [actions, setActions] = useState<SecurityActionDefinitionDto[]>([]);
  const [members, setMembers] = useState<SecurityMemberDefinitionDto[]>([]);
  const [resourcePage, setResourcePage] = useState(1);
  const [resourceSearch, setResourceSearch] = useState('');
  const [resourceTotalCount, setResourceTotalCount] = useState(0);
  const [resourceTotalPages, setResourceTotalPages] = useState(0);
  const [actionPage, setActionPage] = useState(1);
  const [actionSearch, setActionSearch] = useState('');
  const [actionTotalCount, setActionTotalCount] = useState(0);
  const [actionTotalPages, setActionTotalPages] = useState(0);
  const [memberPage, setMemberPage] = useState(1);
  const [memberSearch, setMemberSearch] = useState('');
  const [memberTotalCount, setMemberTotalCount] = useState(0);
  const [memberTotalPages, setMemberTotalPages] = useState(0);
  const [refreshToken, setRefreshToken] = useState(0);
  const [selectedResourceId, setSelectedResourceId] = useState('');
  const [selectedActionId, setSelectedActionId] = useState('');
  const [selectedMemberId, setSelectedMemberId] = useState('');
  const [resourceDraft, setResourceDraft] = useState<ResourceDraft>(emptyResource);
  const [actionDraft, setActionDraft] = useState<ActionDraft>(emptyAction);
  const [memberDraft, setMemberDraft] = useState<MemberDraft>({ displayName: '', isSensitive: false, isActive: true, reason: '' });
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);

  const selectedResource = useMemo(() => resources.find(item => item.publicId === selectedResourceId), [resources, selectedResourceId]);
  const selectedAction = useMemo(() => actions.find(item => item.publicId === selectedActionId), [actions, selectedActionId]);
  const selectedMember = useMemo(() => members.find(item => item.publicId === selectedMemberId), [members, selectedMemberId]);

  useEffect(() => {
    void getSecurityResourcesPage({ page: resourcePage, pageSize: 25, search: resourceSearch, sortBy: 'code', sortDirection: 'asc' }).then(result => {
      setResources(result.data?.items ?? []); setResourceTotalCount(result.data?.totalCount ?? 0); setResourceTotalPages(result.data?.totalPages ?? 0);
      if (!result.success) setMessage(result.message ?? 'Resource registry could not be loaded.');
    });
  }, [resourcePage, resourceSearch, refreshToken]);
  useEffect(() => {
    void getSecurityActionsPage({ page: actionPage, pageSize: 25, search: actionSearch, sortBy: 'code', sortDirection: 'asc' }).then(result => {
      setActions(result.data?.items ?? []); setActionTotalCount(result.data?.totalCount ?? 0); setActionTotalPages(result.data?.totalPages ?? 0);
      if (!result.success) setMessage(result.message ?? 'Action registry could not be loaded.');
    });
  }, [actionPage, actionSearch, refreshToken]);
  useEffect(() => {
    void getSecurityMembersPage({ page: memberPage, pageSize: 25, search: memberSearch, sortBy: 'resource', sortDirection: 'asc' }).then(result => {
      setMembers(result.data?.items ?? []); setMemberTotalCount(result.data?.totalCount ?? 0); setMemberTotalPages(result.data?.totalPages ?? 0);
      if (!result.success) setMessage(result.message ?? 'Member registry could not be loaded.');
    });
  }, [memberPage, memberSearch, refreshToken]);

  useEffect(() => {
    setResourceDraft(selectedResource ? { ...selectedResource, reason: '' } : emptyResource);
  }, [selectedResource]);
  useEffect(() => {
    setActionDraft(selectedAction ? { ...selectedAction, reason: '' } : emptyAction);
  }, [selectedAction]);
  useEffect(() => {
    setMemberDraft(selectedMember
      ? { displayName: selectedMember.displayName, isSensitive: selectedMember.isSensitive, isActive: selectedMember.isActive, reason: '' }
      : { displayName: '', isSensitive: false, isActive: true, reason: '' });
  }, [selectedMember]);

  const finishSave = (success: boolean, failure?: string) => {
    if (!success) { setMessage(failure ?? 'Registry definition could not be saved.'); setBusy(false); return; }
    setRefreshToken(value => value + 1); onDefinitionsChanged?.(); setMessage('Registry definition and effective permission catalogue saved and audited.'); setBusy(false);
  };

  const saveResource = async () => {
    setBusy(true); setMessage('');
    const result = selectedResource
      ? await updateSecurityResource(selectedResource, resourceDraft)
      : await createSecurityResource(resourceDraft);
    if (result.success && result.data) { setSelectedResourceId(result.data.publicId); setResourceSearch(result.data.code); setResourcePage(1); }
    finishSave(result.success, result.message);
  };

  const saveAction = async () => {
    setBusy(true); setMessage('');
    const result = selectedAction
      ? await updateSecurityAction(selectedAction, actionDraft)
      : await createSecurityAction(actionDraft);
    if (result.success && result.data) { setSelectedActionId(result.data.publicId); setActionSearch(result.data.code); setActionPage(1); }
    finishSave(result.success, result.message);
  };

  const saveMember = async () => {
    if (!selectedMember) return;
    setBusy(true); setMessage('');
    const result = await updateSecurityMember(selectedMember, memberDraft);
    if (result.success && result.data) { setSelectedMemberId(result.data.publicId); setMemberSearch(result.data.memberCode); setMemberPage(1); }
    finishSave(result.success, result.message);
  };

  return <section className="rounded-lg border border-gray-200 bg-white shadow-sm">
    <div className="border-b border-gray-200 p-4">
      <h2 className="text-lg font-semibold text-gray-900">Security resource registry</h2>
      <p className="mt-1 text-sm text-gray-600">Govern stable resources, business actions, and the approved member catalogue. Registry changes require system scope and are effective without recompilation.</p>
      <div className="mt-3 flex gap-2" role="tablist" aria-label="Security registry catalogues">
        {(['resources', 'actions', 'members'] as RegistryTab[]).map(item => <button key={item} type="button" role="tab" aria-selected={tab === item} onClick={() => setTab(item)} className={`rounded px-3 py-2 text-sm capitalize ${tab === item ? 'bg-blue-700 text-white' : 'bg-gray-100 text-gray-700'}`}>{item}</button>)}
      </div>
    </div>

    {tab === 'resources' && <div className="grid gap-5 p-4 lg:grid-cols-[minmax(16rem,1fr)_2fr]">
      <div><RegistryPager label="resources" search={resourceSearch} page={resourcePage} totalCount={resourceTotalCount} totalPages={resourceTotalPages} onSearch={value => { setResourceSearch(value); setResourcePage(1); setSelectedResourceId(''); }} onPage={value => { setResourcePage(value); setSelectedResourceId(''); }} /><RegistryList label="Registered resources" value={selectedResourceId} onChange={setSelectedResourceId} onNew={() => setSelectedResourceId('')} items={resources.map(item => ({ id: item.publicId, label: `${item.name} (${item.code})`, active: item.isActive }))} /></div>
      <div className="grid gap-3 md:grid-cols-2">
        <RegistryInput label="Resource code" value={resourceDraft.code} disabled={!!selectedResource} onChange={value => setResourceDraft(current => ({ ...current, code: value.toUpperCase() }))} />
        <RegistryInput label="Resource name" value={resourceDraft.name} onChange={value => setResourceDraft(current => ({ ...current, name: value }))} />
        <label className="text-sm text-gray-700">Resource type<select aria-label="Resource type" className="mt-1 w-full rounded border border-gray-300 p-2" value={resourceDraft.type} onChange={event => setResourceDraft(current => ({ ...current, type: event.target.value as ResourceDraft['type'] }))}><option>ENTITY</option><option>REPORT</option><option>WORKFLOW</option><option>SERVICE</option></select></label>
        <RegistryInput label="Description" value={resourceDraft.description ?? ''} onChange={value => setResourceDraft(current => ({ ...current, description: value }))} />
        <div className="md:col-span-2 grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
          {([
            ['canCreate', 'Create'], ['canRead', 'Read'], ['canUpdate', 'Update'], ['canDelete', 'Delete'], ['canExport', 'Export'], ['canImport', 'Import'], ['supportsMembers', 'Field security'], ['supportsCriteria', 'Record criteria'], ['isActive', 'Active'],
          ] as Array<[keyof ResourceDraft, string]>).map(([key, label]) => <RegistryCheckbox key={key} label={label} checked={Boolean(resourceDraft[key])} disabled={key === 'isActive' && !selectedResource} onChange={checked => setResourceDraft(current => ({ ...current, [key]: checked }))} />)}
        </div>
        <RegistryReason value={resourceDraft.reason} onChange={reason => setResourceDraft(current => ({ ...current, reason }))} />
        <RegistrySave busy={busy} disabled={resourceDraft.code.trim().length < 3 || resourceDraft.name.trim().length < 2 || resourceDraft.reason.trim().length < 5} onClick={saveResource} />
      </div>
    </div>}

    {tab === 'actions' && <div className="grid gap-5 p-4 lg:grid-cols-[minmax(16rem,1fr)_2fr]">
      <div><RegistryPager label="actions" search={actionSearch} page={actionPage} totalCount={actionTotalCount} totalPages={actionTotalPages} onSearch={value => { setActionSearch(value); setActionPage(1); setSelectedActionId(''); }} onPage={value => { setActionPage(value); setSelectedActionId(''); }} /><RegistryList label="Registered actions" value={selectedActionId} onChange={setSelectedActionId} onNew={() => setSelectedActionId('')} items={actions.map(item => ({ id: item.publicId, label: `${item.name} (${item.code})`, active: item.isActive }))} /></div>
      <div className="grid gap-3 md:grid-cols-2">
        <RegistryInput label="Action code" value={actionDraft.code} disabled={!!selectedAction} onChange={value => setActionDraft(current => ({ ...current, code: value.toUpperCase() }))} />
        <RegistryInput label="Action name" value={actionDraft.name} onChange={value => setActionDraft(current => ({ ...current, name: value }))} />
        <div><label className="text-sm text-gray-700">Resource<select aria-label="Action resource" disabled={!!selectedAction} className="mt-1 w-full rounded border border-gray-300 p-2 disabled:bg-gray-100" value={actionDraft.resourceCode} onChange={event => setActionDraft(current => ({ ...current, resourceCode: event.target.value }))}><option value="">Select resource</option>{actionDraft.resourceCode && !resources.some(item => item.code === actionDraft.resourceCode) && <option value={actionDraft.resourceCode}>{actionDraft.resourceCode}</option>}{resources.filter(item => item.isActive).map(item => <option key={item.code} value={item.code}>{item.name} ({item.code})</option>)}</select></label><RegistryPager label="action resources" search={resourceSearch} page={resourcePage} totalCount={resourceTotalCount} totalPages={resourceTotalPages} onSearch={value => { setResourceSearch(value); setResourcePage(1); }} onPage={setResourcePage} compact /></div>
        <RegistryInput label="Action description" value={actionDraft.description ?? ''} onChange={value => setActionDraft(current => ({ ...current, description: value }))} />
        <RegistryCheckbox label="Active" checked={actionDraft.isActive} disabled={!selectedAction} onChange={isActive => setActionDraft(current => ({ ...current, isActive }))} />
        <RegistryReason value={actionDraft.reason} onChange={reason => setActionDraft(current => ({ ...current, reason }))} />
        <RegistrySave busy={busy} disabled={!actionDraft.resourceCode || actionDraft.code.trim().length < 3 || actionDraft.name.trim().length < 2 || actionDraft.reason.trim().length < 5} onClick={saveAction} />
      </div>
    </div>}

    {tab === 'members' && <div className="grid gap-5 p-4 lg:grid-cols-[minmax(16rem,1fr)_2fr]">
      <div><RegistryPager label="members" search={memberSearch} page={memberPage} totalCount={memberTotalCount} totalPages={memberTotalPages} onSearch={value => { setMemberSearch(value); setMemberPage(1); setSelectedMemberId(''); }} onPage={value => { setMemberPage(value); setSelectedMemberId(''); }} /><label className="text-sm font-medium text-gray-700" htmlFor="security-member-definition">Approved member</label><select id="security-member-definition" className="mt-1 w-full rounded border border-gray-300 p-2" value={selectedMemberId} onChange={event => setSelectedMemberId(event.target.value)}><option value="">Select a registered member</option>{members.map(item => <option key={item.publicId} value={item.publicId}>{item.resourceCode} / {item.displayName}{item.isActive ? '' : ' (inactive)'}</option>)}</select><p className="mt-2 text-xs text-gray-500">New member codes are registered by application code so arbitrary field names can never bypass server enforcement.</p></div>
      <div className="grid gap-3 md:grid-cols-2">
        <RegistryInput label="Member code" value={selectedMember ? `${selectedMember.resourceCode}.${selectedMember.memberCode}` : ''} disabled onChange={() => undefined} />
        <RegistryInput label="Display name" value={memberDraft.displayName} disabled={!selectedMember} onChange={displayName => setMemberDraft(current => ({ ...current, displayName }))} />
        <RegistryCheckbox label="Sensitive value" checked={memberDraft.isSensitive} disabled={!selectedMember} onChange={isSensitive => setMemberDraft(current => ({ ...current, isSensitive }))} />
        <RegistryCheckbox label="System managed" checked={selectedMember?.isSystemManaged ?? false} disabled onChange={() => undefined} />
        <RegistryCheckbox label="Active" checked={memberDraft.isActive} disabled={!selectedMember} onChange={isActive => setMemberDraft(current => ({ ...current, isActive }))} />
        <RegistryReason value={memberDraft.reason} disabled={!selectedMember} onChange={reason => setMemberDraft(current => ({ ...current, reason }))} />
        <RegistrySave busy={busy} disabled={!selectedMember || memberDraft.displayName.trim().length < 2 || memberDraft.reason.trim().length < 5} onClick={saveMember} />
      </div>
    </div>}
    {message && <p role="status" className="border-t border-gray-200 px-4 py-3 text-sm text-gray-700">{message}</p>}
  </section>;
}

function RegistryList({ label, value, onChange, onNew, items }: { label: string; value: string; onChange: (value: string) => void; onNew: () => void; items: Array<{ id: string; label: string; active: boolean }> }) {
  return <div><label className="text-sm font-medium text-gray-700">{label}<select aria-label={label} className="mt-1 w-full rounded border border-gray-300 p-2" value={value} onChange={event => onChange(event.target.value)}><option value="">Create new</option>{items.map(item => <option key={item.id} value={item.id}>{item.label}{item.active ? '' : ' (inactive)'}</option>)}</select></label><button type="button" onClick={onNew} className="mt-2 text-sm text-blue-700">Start a new definition</button></div>;
}
function RegistryPager({ label, search, page, totalCount, totalPages, onSearch, onPage, compact = false }: { label: string; search: string; page: number; totalCount: number; totalPages: number; onSearch: (value: string) => void; onPage: (value: number) => void; compact?: boolean }) {
  return <div className={compact ? 'mt-2 space-y-1' : 'mb-3 space-y-2'}><input aria-label={`Search ${label}`} placeholder={`Search ${label}`} className="w-full rounded border border-gray-300 p-2 text-sm" value={search} onChange={event => onSearch(event.target.value)} /><div className="flex items-center justify-between gap-2 text-xs text-gray-500"><span>{totalCount} · Page {page} of {Math.max(totalPages, 1)}</span><span className="flex gap-1"><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={page <= 1} onClick={() => onPage(Math.max(1, page - 1))}>Previous {label}</button><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={page >= totalPages} onClick={() => onPage(page + 1)}>Next {label}</button></span></div></div>;
}
function RegistryInput({ label, value, disabled, onChange }: { label: string; value: string; disabled?: boolean; onChange: (value: string) => void }) {
  return <label className="text-sm text-gray-700">{label}<input aria-label={label} className="mt-1 w-full rounded border border-gray-300 p-2 disabled:bg-gray-100" value={value} disabled={disabled} onChange={event => onChange(event.target.value)} /></label>;
}
function RegistryCheckbox({ label, checked, disabled, onChange }: { label: string; checked: boolean; disabled?: boolean; onChange: (checked: boolean) => void }) {
  return <label className="flex items-center gap-2 text-sm text-gray-700"><input type="checkbox" aria-label={label} checked={checked} disabled={disabled} onChange={event => onChange(event.target.checked)} />{label}</label>;
}
function RegistryReason({ value, disabled, onChange }: { value: string; disabled?: boolean; onChange: (value: string) => void }) {
  return <label className="text-sm text-gray-700 md:col-span-2">Audit reason<input aria-label="Registry audit reason" className="mt-1 w-full rounded border border-gray-300 p-2 disabled:bg-gray-100" value={value} disabled={disabled} onChange={event => onChange(event.target.value)} /></label>;
}
function RegistrySave({ busy, disabled, onClick }: { busy: boolean; disabled: boolean; onClick: () => Promise<void> }) {
  return <button type="button" disabled={busy || disabled} onClick={() => void onClick()} className="w-fit rounded bg-blue-700 px-4 py-2 text-sm font-medium text-white disabled:opacity-50">{busy ? 'Saving…' : 'Save registry definition'}</button>;
}
