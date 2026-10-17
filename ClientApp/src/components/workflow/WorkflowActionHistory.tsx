import { useEffect, useState } from 'react';
import { History } from 'lucide-react';
import { getSubmissionWorkflowActionsPage } from '../../api/api';
import type { WorkflowActionHistoryDto } from '../../types';
import { Badge, Button } from '../ui';
import { useSecurity } from '../../context/SecurityContext';

const outcomeLabels: Record<string, string> = {
  '1': 'Submitted',
  Submit: 'Submitted',
  '2': 'Approved',
  Approve: 'Approved',
  '3': 'Rejected',
  Reject: 'Rejected',
  '4': 'RFI raised',
  RaiseRfi: 'RFI raised',
  '5': 'RFI responded',
  RespondRfi: 'RFI responded',
  '6': 'Bypassed',
  Bypass: 'Bypassed',
  '7': 'Completed',
  Complete: 'Completed',
  '8': 'Reopened',
  Reopen: 'Reopened',
  '9': 'Resubmitted',
  Resubmit: 'Resubmitted',
};

function outcomeLabel(outcome: number | string) {
  const raw = String(outcome);
  return outcomeLabels[raw] ?? raw.replace(/([a-z])([A-Z])/g, '$1 $2');
}

function actionLabel(code: string) {
  const segments = code.split('.');
  return segments[segments.length - 1]?.replace(/_/g, ' ') ?? code;
}

export function WorkflowActionHistory({ kind, submissionId }: { kind: 1 | 2; submissionId: string }) {
  const security = useSecurity();
  const resource = kind === 1 ? 'OPMS_WORKFLOW' : 'IPMS_WORKFLOW';
  const canReadActorId = security.canReadField(resource, 'ActionActorUserId');
  const canReadComment = security.canReadField(resource, 'ActionComment');
  const canReadRating = security.canReadField(resource, 'ActionRatingValue');
  const [actions, setActions] = useState<WorkflowActionHistoryDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  useEffect(() => { setPage(1); }, [kind, submissionId]);
  useEffect(() => {
    let active = true;
    setError(null);
    void getSubmissionWorkflowActionsPage(kind, submissionId, { page, pageSize: 10, sortBy: 'sequence', sortDirection: 'asc' }).then(result => {
      if (!active) return;
      if (!result.success) setError(result.message ?? 'Workflow history could not be loaded.');
      else {
        setActions(result.data?.items ?? []);
        setTotalCount(result.data?.totalCount ?? 0);
        setTotalPages(result.data?.totalPages ?? 0);
      }
    });
    return () => { active = false; };
  }, [kind, submissionId, page]);

  return <div className="rounded-xl border border-secondary-200 p-4 dark:border-secondary-700">
    <div className="flex items-center gap-2"><History className="h-4 w-4 text-primary-600" /><h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Immutable workflow history</h4></div>
    {error && <p role="alert" className="mt-2 text-xs text-error-600">{error}</p>}
    {!error && actions.length === 0 && <p className="mt-2 text-xs text-secondary-500">No governed workflow action has been recorded.</p>}
    <ol className="mt-3 space-y-2">{actions.map(action => <li key={action.publicId} className="flex flex-wrap items-start justify-between gap-2 rounded-lg bg-secondary-50 p-3 text-sm dark:bg-secondary-800">
      <div><p className="font-medium text-secondary-900 dark:text-white">{action.sequence}. {actionLabel(action.actionCode)}</p><p className="text-xs text-secondary-500">{outcomeLabel(action.outcome)}{canReadActorId && (action.actorName || action.actorUserPublicId) ? ` · ${action.actorName || action.actorUserPublicId}` : ''} · {new Date(action.occurredAt).toLocaleString()}</p>{canReadComment && action.comment && <p className="mt-1 text-xs text-secondary-600 dark:text-secondary-300">{action.comment}</p>}</div>
      {canReadRating && action.ratingValue != null && <Badge variant="info">Rating {action.ratingValue}</Badge>}
    </li>)}</ol>
    {!error && totalPages > 1 && <div className="mt-3 flex items-center justify-between text-xs text-secondary-500">
      <span>Page {page} of {totalPages} · {totalCount} actions</span>
      <div className="flex gap-2"><Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage(value => Math.max(1, value - 1))}>Previous actions</Button><Button size="sm" variant="outline" disabled={page >= totalPages} onClick={() => setPage(value => value + 1)}>Next actions</Button></div>
    </div>}
  </div>;
}
