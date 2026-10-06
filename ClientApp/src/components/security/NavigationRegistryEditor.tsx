import { useEffect, useState } from 'react';
import { createSecurityNavigationItem, getSecurityNavigationRegistryPage, getSecurityPermissionDefinitionsPage, updateSecurityNavigationItem } from '../../api/api';
import type { SecurityNavigationItemDto, SecurityPermissionDefinition } from '../../types';

type Draft = { code: string; parentPublicId: string; name: string; route: string; iconKey: string; displayOrder: string; requiredPermissionCode: string; isActive: boolean; reason: string };
const blank = (): Draft => ({ code: '', parentPublicId: '', name: '', route: '', iconKey: '', displayOrder: '100', requiredPermissionCode: '', isActive: true, reason: '' });

export function NavigationRegistryEditor({ refreshToken = 0 }: { refreshToken?: number }) {
  const [items, setItems] = useState<SecurityNavigationItemDto[]>([]);
  const [navigationPage, setNavigationPage] = useState(1);
  const [navigationSearch, setNavigationSearch] = useState('');
  const [navigationTotalCount, setNavigationTotalCount] = useState(0);
  const [navigationTotalPages, setNavigationTotalPages] = useState(0);
  const [navigationRefreshToken, setNavigationRefreshToken] = useState(0);
  const [parentOptions, setParentOptions] = useState<SecurityNavigationItemDto[]>([]);
  const [parentPage, setParentPage] = useState(1);
  const [parentSearch, setParentSearch] = useState('');
  const [parentTotalCount, setParentTotalCount] = useState(0);
  const [parentTotalPages, setParentTotalPages] = useState(0);
  const [permissionOptions, setPermissionOptions] = useState<SecurityPermissionDefinition[]>([]);
  const [permissionPage, setPermissionPage] = useState(1);
  const [permissionSearch, setPermissionSearch] = useState('');
  const [permissionTotalCount, setPermissionTotalCount] = useState(0);
  const [permissionTotalPages, setPermissionTotalPages] = useState(0);
  const [selected, setSelected] = useState<SecurityNavigationItemDto | null>(null);
  const [draft, setDraft] = useState<Draft>(blank);
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    void getSecurityNavigationRegistryPage({ page: navigationPage, pageSize: 25, search: navigationSearch, sortBy: 'order', sortDirection: 'asc' }).then(result => {
      setItems(result.data?.items ?? []);
      setNavigationTotalCount(result.data?.totalCount ?? 0);
      setNavigationTotalPages(result.data?.totalPages ?? 0);
      if (!result.success) setMessage(result.message ?? 'Navigation registry could not be loaded.');
    });
  }, [navigationPage, navigationSearch, navigationRefreshToken]);
  useEffect(() => {
    void getSecurityNavigationRegistryPage({ page: parentPage, pageSize: 25, search: parentSearch, sortBy: 'name', sortDirection: 'asc' }, true).then(result => {
      setParentOptions(result.data?.items ?? []);
      setParentTotalCount(result.data?.totalCount ?? 0);
      setParentTotalPages(result.data?.totalPages ?? 0);
      if (!result.success) setMessage(result.message ?? 'Navigation parent choices could not be loaded.');
    });
  }, [parentPage, parentSearch, navigationRefreshToken]);
  useEffect(() => {
    void getSecurityPermissionDefinitionsPage({ page: permissionPage, pageSize: 25, search: permissionSearch, sortBy: 'code', sortDirection: 'asc' }, ['Navigation', 'Action']).then(result => {
      setPermissionOptions(result.data?.items ?? []);
      setPermissionTotalCount(result.data?.totalCount ?? 0);
      setPermissionTotalPages(result.data?.totalPages ?? 0);
      if (!result.success) setMessage(result.message ?? 'Navigation permission choices could not be loaded.');
    });
  }, [permissionPage, permissionSearch, refreshToken]);

  const edit = (item: SecurityNavigationItemDto | null) => {
    setSelected(item);
    setDraft(item ? { code: item.code, parentPublicId: item.parentPublicId ?? '', name: item.name, route: item.route ?? '', iconKey: item.iconKey ?? '', displayOrder: String(item.displayOrder), requiredPermissionCode: item.requiredPermissionCode ?? '', isActive: item.isActive, reason: '' } : blank());
    setMessage('');
  };

  const save = async () => {
    setBusy(true); setMessage('');
    const common = { parentPublicId: draft.parentPublicId || null, name: draft.name, route: draft.route || null, iconKey: draft.iconKey || null, displayOrder: Number(draft.displayOrder), requiredPermissionCode: draft.requiredPermissionCode || null, reason: draft.reason };
    const result = selected
      ? await updateSecurityNavigationItem(selected, { ...common, isActive: draft.isActive })
      : await createSecurityNavigationItem({ ...common, code: draft.code });
    if (!result.success) setMessage(result.message ?? 'Navigation item could not be saved.');
    else {
      edit(result.data ?? null);
      if (result.data) { setNavigationSearch(result.data.code); setNavigationPage(1); }
      setNavigationRefreshToken(value => value + 1);
      setMessage('Navigation registry saved and audited.');
    }
    setBusy(false);
  };

  return <section className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
    <div className="flex items-center justify-between"><div><h2 className="text-lg font-semibold">Navigation registry</h2><p className="text-sm text-gray-600">Edit the authoritative hierarchy used by login and My Menu. Stable codes are immutable.</p></div><button type="button" onClick={() => edit(null)} className="rounded border border-blue-700 px-3 py-2 text-sm text-blue-700">New item</button></div>
    <div className="mt-4 grid gap-5 xl:grid-cols-[0.9fr_1.1fr]">
      <div><input aria-label="Search navigation registry" placeholder="Search code, name, parent, route or permission" className="mb-2 w-full rounded border border-gray-300 p-2 text-sm" value={navigationSearch} onChange={event => { setNavigationSearch(event.target.value); setNavigationPage(1); edit(null); }} /><div className="max-h-[28rem] overflow-auto rounded border border-gray-200">{items.map(item => <button type="button" key={item.publicId} onClick={() => edit(item)} className={`block w-full border-b border-gray-100 px-3 py-2 text-left text-sm ${selected?.publicId === item.publicId ? 'bg-blue-50' : 'hover:bg-gray-50'}`}><span className={item.isActive ? 'font-medium' : 'text-gray-400 line-through'}>{item.name}</span><span className="ml-2 font-mono text-xs text-gray-500">{item.code}</span><span className="block text-xs text-gray-500">Parent: {item.parentName ?? 'Root'} · Order {item.displayOrder}</span></button>)}{!items.length && <p className="p-4 text-sm text-gray-500">No navigation definitions.</p>}</div><div className="mt-2 flex items-center justify-between gap-2 text-xs text-gray-500"><span>{navigationTotalCount} items · Page {navigationPage} of {Math.max(navigationTotalPages, 1)}</span><span className="flex gap-1"><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={navigationPage <= 1} onClick={() => { setNavigationPage(value => Math.max(1, value - 1)); edit(null); }}>Previous navigation items</button><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={navigationPage >= navigationTotalPages} onClick={() => { setNavigationPage(value => value + 1); edit(null); }}>Next navigation items</button></span></div></div>
      <div className="grid gap-3 md:grid-cols-2">
        <label className="text-sm">Stable code<input aria-label="Navigation code" disabled={!!selected} className="mt-1 w-full rounded border border-gray-300 p-2 font-mono disabled:bg-gray-100" value={draft.code} onChange={event => setDraft(value => ({ ...value, code: event.target.value.toUpperCase() }))} /></label>
        <label className="text-sm">Name<input aria-label="Navigation name" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.name} onChange={event => setDraft(value => ({ ...value, name: event.target.value }))} /></label>
        <div className="text-sm"><label htmlFor="navigation-parent-search">Parent</label><input id="navigation-parent-search" aria-label="Search navigation parents" placeholder="Search active parent items" className="mt-1 w-full rounded border border-gray-300 p-2" value={parentSearch} onChange={event => { setParentSearch(event.target.value); setParentPage(1); }} /><select aria-label="Navigation parent" className="mt-2 w-full rounded border border-gray-300 p-2" value={draft.parentPublicId} onChange={event => setDraft(value => ({ ...value, parentPublicId: event.target.value }))}><option value="">Root</option>{draft.parentPublicId && !parentOptions.some(item => item.publicId === draft.parentPublicId) && <option value={draft.parentPublicId}>{selected?.parentName ?? selected?.parentCode ?? draft.parentPublicId}</option>}{parentOptions.filter(item => item.publicId !== selected?.publicId).map(item => <option key={item.publicId} value={item.publicId}>{item.name} ({item.code})</option>)}</select><div className="mt-2 flex items-center justify-between gap-2 text-xs text-gray-500"><span>{parentTotalCount} active · Page {parentPage} of {Math.max(parentTotalPages, 1)}</span><span className="flex gap-1"><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={parentPage <= 1} onClick={() => setParentPage(value => Math.max(1, value - 1))}>Previous parents</button><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={parentPage >= parentTotalPages} onClick={() => setParentPage(value => value + 1)}>Next parents</button></span></div></div>
        <label className="text-sm">Display order<input aria-label="Navigation display order" type="number" min="0" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.displayOrder} onChange={event => setDraft(value => ({ ...value, displayOrder: event.target.value }))} /></label>
        <label className="text-sm">Route<input aria-label="Navigation route" placeholder="/reports" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.route} onChange={event => setDraft(value => ({ ...value, route: event.target.value }))} /></label>
        <label className="text-sm">Icon key<input aria-label="Navigation icon" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.iconKey} onChange={event => setDraft(value => ({ ...value, iconKey: event.target.value }))} /></label>
        <div className="text-sm md:col-span-2"><label htmlFor="navigation-permission-search">Required permission</label><input id="navigation-permission-search" aria-label="Search navigation permissions" placeholder="Search navigation or action permissions" className="mt-1 w-full rounded border border-gray-300 p-2" value={permissionSearch} onChange={event => { setPermissionSearch(event.target.value); setPermissionPage(1); }} /><select aria-label="Navigation permission" className="mt-2 w-full rounded border border-gray-300 p-2" value={draft.requiredPermissionCode} onChange={event => setDraft(value => ({ ...value, requiredPermissionCode: event.target.value }))}><option value="">Container (child-derived visibility)</option>{draft.requiredPermissionCode && !permissionOptions.some(item => item.code === draft.requiredPermissionCode) && <option value={draft.requiredPermissionCode}>{draft.requiredPermissionCode}</option>}{permissionOptions.map(item => <option key={item.code} value={item.code}>{item.code}</option>)}</select><div className="mt-2 flex items-center justify-between gap-2 text-xs text-gray-500"><span>{permissionTotalCount} permissions · Page {permissionPage} of {Math.max(permissionTotalPages, 1)}</span><span className="flex gap-1"><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={permissionPage <= 1} onClick={() => setPermissionPage(value => Math.max(1, value - 1))}>Previous permission choices</button><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={permissionPage >= permissionTotalPages} onClick={() => setPermissionPage(value => value + 1)}>Next permission choices</button></span></div></div>
        {selected && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={draft.isActive} onChange={event => setDraft(value => ({ ...value, isActive: event.target.checked }))} /> Active</label>}
        <label className="text-sm md:col-span-2">Audit reason<textarea aria-label="Navigation audit reason" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.reason} onChange={event => setDraft(value => ({ ...value, reason: event.target.value }))} /></label>
        <div className="flex items-center justify-between md:col-span-2"><span role="status" className="text-sm text-gray-600">{message}</span><button type="button" onClick={() => void save()} disabled={busy || draft.code.trim().length < 3 || draft.name.trim().length < 2 || draft.reason.trim().length < 5} className="rounded bg-blue-700 px-4 py-2 text-sm text-white disabled:opacity-50">{busy ? 'Saving…' : 'Save navigation item'}</button></div>
      </div>
    </div>
  </section>;
}
