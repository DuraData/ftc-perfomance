import { useEffect, useMemo, useState } from 'react';
import { createSecurityNavigationItem, getSecurityNavigationRegistry, updateSecurityNavigationItem } from '../../api/api';
import type { SecurityNavigationItemDto, SecurityPermissionDefinition } from '../../types';

type Draft = { code: string; parentPublicId: string; name: string; route: string; iconKey: string; displayOrder: string; requiredPermissionCode: string; isActive: boolean; reason: string };
const blank = (): Draft => ({ code: '', parentPublicId: '', name: '', route: '', iconKey: '', displayOrder: '100', requiredPermissionCode: '', isActive: true, reason: '' });

export function NavigationRegistryEditor({ permissions }: { permissions: SecurityPermissionDefinition[] }) {
  const [items, setItems] = useState<SecurityNavigationItemDto[]>([]);
  const [selected, setSelected] = useState<SecurityNavigationItemDto | null>(null);
  const [draft, setDraft] = useState<Draft>(blank);
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);

  const load = async () => {
    const result = await getSecurityNavigationRegistry();
    if (!result.success) setMessage(result.message ?? 'Navigation registry could not be loaded.');
    else setItems(result.data ?? []);
  };
  useEffect(() => { void load(); }, []);

  const ordered = useMemo(() => {
    const byParent = new Map<string, SecurityNavigationItemDto[]>();
    for (const item of items) {
      const key = item.parentPublicId ?? '';
      byParent.set(key, [...(byParent.get(key) ?? []), item]);
    }
    for (const values of byParent.values()) values.sort((a, b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name));
    const rows: Array<{ item: SecurityNavigationItemDto; depth: number }> = [];
    const visit = (parent: string, depth: number) => { for (const item of byParent.get(parent) ?? []) { rows.push({ item, depth }); visit(item.publicId, depth + 1); } };
    visit('', 0);
    return rows;
  }, [items]);

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
    else { setMessage('Navigation registry saved and audited.'); edit(result.data ?? null); await load(); }
    setBusy(false);
  };

  const permissionOptions = permissions.filter(item => item.kind === 'Navigation' || item.kind === 'Action').sort((a, b) => a.code.localeCompare(b.code));
  return <section className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
    <div className="flex items-center justify-between"><div><h2 className="text-lg font-semibold">Navigation registry</h2><p className="text-sm text-gray-600">Edit the authoritative hierarchy used by login and My Menu. Stable codes are immutable.</p></div><button type="button" onClick={() => edit(null)} className="rounded border border-blue-700 px-3 py-2 text-sm text-blue-700">New item</button></div>
    <div className="mt-4 grid gap-5 xl:grid-cols-[0.9fr_1.1fr]">
      <div className="max-h-[32rem] overflow-auto rounded border border-gray-200">{ordered.map(({ item, depth }) => <button type="button" key={item.publicId} onClick={() => edit(item)} className={`block w-full border-b border-gray-100 px-3 py-2 text-left text-sm ${selected?.publicId === item.publicId ? 'bg-blue-50' : 'hover:bg-gray-50'}`} style={{ paddingLeft: `${12 + depth * 20}px` }}><span className={item.isActive ? 'font-medium' : 'text-gray-400 line-through'}>{item.name}</span><span className="ml-2 font-mono text-xs text-gray-500">{item.code}</span></button>)}{!ordered.length && <p className="p-4 text-sm text-gray-500">No navigation definitions.</p>}</div>
      <div className="grid gap-3 md:grid-cols-2">
        <label className="text-sm">Stable code<input aria-label="Navigation code" disabled={!!selected} className="mt-1 w-full rounded border border-gray-300 p-2 font-mono disabled:bg-gray-100" value={draft.code} onChange={event => setDraft(value => ({ ...value, code: event.target.value.toUpperCase() }))} /></label>
        <label className="text-sm">Name<input aria-label="Navigation name" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.name} onChange={event => setDraft(value => ({ ...value, name: event.target.value }))} /></label>
        <label className="text-sm">Parent<select aria-label="Navigation parent" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.parentPublicId} onChange={event => setDraft(value => ({ ...value, parentPublicId: event.target.value }))}><option value="">Root</option>{ordered.filter(row => row.item.publicId !== selected?.publicId && row.item.isActive).map(({ item, depth }) => <option key={item.publicId} value={item.publicId}>{'—'.repeat(depth)} {item.name}</option>)}</select></label>
        <label className="text-sm">Display order<input aria-label="Navigation display order" type="number" min="0" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.displayOrder} onChange={event => setDraft(value => ({ ...value, displayOrder: event.target.value }))} /></label>
        <label className="text-sm">Route<input aria-label="Navigation route" placeholder="/reports" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.route} onChange={event => setDraft(value => ({ ...value, route: event.target.value }))} /></label>
        <label className="text-sm">Icon key<input aria-label="Navigation icon" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.iconKey} onChange={event => setDraft(value => ({ ...value, iconKey: event.target.value }))} /></label>
        <label className="text-sm md:col-span-2">Required permission<select aria-label="Navigation permission" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.requiredPermissionCode} onChange={event => setDraft(value => ({ ...value, requiredPermissionCode: event.target.value }))}><option value="">Container (child-derived visibility)</option>{permissionOptions.map(item => <option key={item.code} value={item.code}>{item.code}</option>)}</select></label>
        {selected && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={draft.isActive} onChange={event => setDraft(value => ({ ...value, isActive: event.target.checked }))} /> Active</label>}
        <label className="text-sm md:col-span-2">Audit reason<textarea aria-label="Navigation audit reason" className="mt-1 w-full rounded border border-gray-300 p-2" value={draft.reason} onChange={event => setDraft(value => ({ ...value, reason: event.target.value }))} /></label>
        <div className="flex items-center justify-between md:col-span-2"><span role="status" className="text-sm text-gray-600">{message}</span><button type="button" onClick={() => void save()} disabled={busy || draft.code.trim().length < 3 || draft.name.trim().length < 2 || draft.reason.trim().length < 5} className="rounded bg-blue-700 px-4 py-2 text-sm text-white disabled:opacity-50">{busy ? 'Saving…' : 'Save navigation item'}</button></div>
      </div>
    </div>
  </section>;
}
