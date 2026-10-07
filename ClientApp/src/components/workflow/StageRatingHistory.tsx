import { useEffect, useState } from 'react';
import { Star } from 'lucide-react';
import { getSubmissionStageRatingsPage } from '../../api/api';
import type { StageRatingDto } from '../../types';
import { Badge, Button } from '../ui';
import { useSecurity } from '../../context/SecurityContext';

export function StageRatingHistory({ kind, submissionId }: { kind: 1 | 2; submissionId: string }) {
  const security = useSecurity();
  const resource = kind === 1 ? 'OPMS_WORKFLOW' : 'IPMS_WORKFLOW';
  const canReadValue = security.canReadField(resource, 'StageRatingValue');
  const canReadAchievement = security.canReadField(resource, 'StageRatingAchievementPercent');
  const canReadComment = security.canReadField(resource, 'StageRatingComment');
  const canReadActorId = security.canReadField(resource, 'StageRatingRatedByUserId');
  const canReadActorName = security.canReadField(resource, 'StageRatingRatedByName');
  const [ratings, setRatings] = useState<StageRatingDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  useEffect(() => {
    let active = true;
    setError(null);
    void getSubmissionStageRatingsPage(kind, submissionId, { page, pageSize: 10, sortBy: 'ratedAt', sortDirection: 'desc' }).then(result => {
      if (!active) return;
      if (!result.success) setError(result.message ?? 'Stage ratings could not be loaded.');
      else {
        setRatings(result.data?.items ?? []);
        setTotalCount(result.data?.totalCount ?? 0);
        setTotalPages(result.data?.totalPages ?? 0);
      }
    });
    return () => { active = false; };
  }, [kind, submissionId, page]);

  useEffect(() => { setPage(1); }, [kind, submissionId]);

  return <div className="rounded-xl border border-secondary-200 p-4 dark:border-secondary-700">
    <div className="flex items-center gap-2"><Star className="h-4 w-4 text-primary-600" /><h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Immutable stage ratings</h4></div>
    {error && <p role="alert" className="mt-2 text-xs text-error-600">{error}</p>}
    {!error && ratings.length === 0 && <p className="mt-2 text-xs text-secondary-500">No governed stage rating has been recorded.</p>}
    <div className="mt-3 space-y-2">{ratings.map(rating => <div key={rating.publicId} className="flex flex-wrap items-start justify-between gap-2 rounded-lg bg-secondary-50 p-3 text-sm dark:bg-secondary-800">
      <div><p className="font-medium text-secondary-900 dark:text-white">{rating.stageCode}{canReadValue && rating.label ? ` · ${rating.label}` : ''}</p><p className="text-xs text-secondary-500">{rating.ratingSchemeCode}{(canReadActorName && rating.ratedByName) || (canReadActorId && rating.ratedByUserId) ? ` · ${(canReadActorName && rating.ratedByName) || rating.ratedByUserId}` : ''} · {new Date(rating.ratedAt).toLocaleString()}</p>{canReadAchievement && rating.achievementPercent != null && <p className="text-xs text-secondary-500">Achievement: {rating.achievementPercent}%</p>}{canReadComment && rating.comment && <p className="mt-1 text-xs text-secondary-600 dark:text-secondary-300">{rating.comment}</p>}</div>
      {canReadValue && rating.value != null && <Badge variant="info">{rating.value}</Badge>}
    </div>)}</div>
    {!error && totalPages > 1 && <div className="mt-3 flex items-center justify-between text-xs text-secondary-500">
      <span>Page {page} of {totalPages} · {totalCount} ratings</span>
      <div className="flex gap-2"><Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage(value => Math.max(1, value - 1))}>Previous ratings</Button><Button size="sm" variant="outline" disabled={page >= totalPages} onClick={() => setPage(value => value + 1)}>Next ratings</Button></div>
    </div>}
  </div>;
}
