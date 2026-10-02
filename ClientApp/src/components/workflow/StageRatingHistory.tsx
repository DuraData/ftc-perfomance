import { useEffect, useState } from 'react';
import { Star } from 'lucide-react';
import { getSubmissionStageRatings } from '../../api/api';
import type { StageRatingDto } from '../../types';
import { Badge } from '../ui';

export function StageRatingHistory({ kind, submissionId }: { kind: 1 | 2; submissionId: string }) {
  const [ratings, setRatings] = useState<StageRatingDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    void getSubmissionStageRatings(kind, submissionId).then(result => {
      if (!active) return;
      if (!result.success) setError(result.message ?? 'Stage ratings could not be loaded.');
      else setRatings(result.data ?? []);
    });
    return () => { active = false; };
  }, [kind, submissionId]);

  return <div className="rounded-xl border border-secondary-200 p-4 dark:border-secondary-700">
    <div className="flex items-center gap-2"><Star className="h-4 w-4 text-primary-600" /><h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Immutable stage ratings</h4></div>
    {error && <p role="alert" className="mt-2 text-xs text-error-600">{error}</p>}
    {!error && ratings.length === 0 && <p className="mt-2 text-xs text-secondary-500">No governed stage rating has been recorded.</p>}
    <div className="mt-3 space-y-2">{ratings.map(rating => <div key={rating.publicId} className="flex flex-wrap items-start justify-between gap-2 rounded-lg bg-secondary-50 p-3 text-sm dark:bg-secondary-800">
      <div><p className="font-medium text-secondary-900 dark:text-white">{rating.stageCode} · {rating.label}</p><p className="text-xs text-secondary-500">{rating.ratingSchemeCode} · {rating.ratedByName ?? rating.ratedByUserId} · {new Date(rating.ratedAt).toLocaleString()}</p>{rating.comment && <p className="mt-1 text-xs text-secondary-600 dark:text-secondary-300">{rating.comment}</p>}</div>
      <Badge variant="info">{rating.value}</Badge>
    </div>)}</div>
  </div>;
}
