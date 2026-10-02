import { useEffect, useState } from 'react';
import { RefreshCw, RotateCcw } from 'lucide-react';
import { getPendingNotificationDeliveries, retryNotificationDelivery } from '../../api/api';
import type { NotificationOutboxItemDto } from '../../types';
import { Badge, Button, Card } from '../ui';
import { Input } from '../common/Form';

export function NotificationDeliveryOperations() {
  const [items, setItems] = useState<NotificationOutboxItemDto[]>([]);
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);
  const load = async () => { setBusy(true); const result = await getPendingNotificationDeliveries(); setItems(result.data ?? []); setMessage(result.success ? '' : result.message ?? 'Delivery queue could not be loaded.'); setBusy(false); };
  useEffect(() => { void load(); }, []);
  const retry = async (item: NotificationOutboxItemDto) => {
    const reason = reasons[item.publicId] ?? '';
    if (reason.trim().length < 5) { setMessage('Enter an audit reason before retrying.'); return; }
    setBusy(true); const result = await retryNotificationDelivery(item, reason); setMessage(result.success ? 'Delivery queued for retry.' : result.message ?? 'Retry failed.'); if (result.success) await load(); setBusy(false);
  };
  return <div className="space-y-3">
    <div className="flex items-center justify-between"><div><h3 className="font-semibold text-secondary-900 dark:text-white">Notification delivery operations</h3><p className="text-sm text-secondary-500">Provider receipts and failures are retained per recipient and channel.</p></div><Button size="sm" variant="outline" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button></div>
    {message && <p role="status" className="text-sm text-secondary-600">{message}</p>}
    {items.map(item => <Card key={item.publicId} className="p-4"><div className="flex flex-wrap justify-between gap-2"><div><p className="font-medium">{item.eventType}</p><p className="text-xs text-secondary-500">{item.aggregateType} · {item.aggregateId} · attempt {item.attemptCount}</p></div><Badge variant={item.isDeadLetter ? 'error' : 'warning'}>{item.isDeadLetter ? 'Dead letter' : 'Pending retry'}</Badge></div>{item.lastError && <p className="mt-2 text-xs text-error-600">{item.lastError}</p>}<div className="mt-3 grid gap-2 md:grid-cols-2">{item.deliveries.map(delivery => <div key={delivery.publicId} className="rounded-lg border border-secondary-200 p-2 text-xs dark:border-secondary-700"><div className="flex justify-between"><span>{delivery.channel} · {delivery.recipientUserId}</span><Badge variant={delivery.status === 'Delivered' ? 'success' : 'error'}>{delivery.status}</Badge></div><p className="mt-1 text-secondary-500">{delivery.provider ?? 'No provider'}{delivery.providerReference ? ` · ${delivery.providerReference}` : ''} · {delivery.attemptCount} attempt(s)</p>{delivery.error && <p className="mt-1 text-error-600">{delivery.error}</p>}</div>)}</div><div className="mt-3 flex items-end gap-2"><Input label="Retry audit reason" value={reasons[item.publicId] ?? ''} onChange={event => setReasons(current => ({ ...current, [item.publicId]: event.target.value }))} /><Button size="sm" variant="primary" icon={<RotateCcw className="h-4 w-4" />} onClick={() => void retry(item)} disabled={busy}>Retry failed channels</Button></div></Card>)}
    {!items.length && !busy && <p className="rounded-lg border border-dashed border-secondary-300 p-8 text-center text-sm text-secondary-500">No pending notification deliveries.</p>}
  </div>;
}
