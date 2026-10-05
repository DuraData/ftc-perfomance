import { Button } from '../ui';

type Props = {
  label: string;
  page: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
};

export function DetailCollectionPaging({ label, page, totalPages, totalCount, onPageChange }: Props) {
  return <div className="mt-3 flex items-center justify-between gap-3 text-xs text-secondary-500" aria-live="polite">
    <span>{totalCount} {label} · Page {page} of {Math.max(totalPages, 1)}</span>
    <span className="flex gap-2">
      <Button type="button" size="sm" variant="outline" disabled={page <= 1} onClick={() => onPageChange(Math.max(1, page - 1))}>Previous {label}</Button>
      <Button type="button" size="sm" variant="outline" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>Next {label}</Button>
    </span>
  </div>;
}
