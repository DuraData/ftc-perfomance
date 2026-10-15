import type { IPMSTarget } from '../../types';

export function relatedOpmsDisplayLabel(target: Pick<IPMSTarget, 'relatedOpmsTargetPublicId' | 'relatedOPMSTarget'>): string | null {
  return target.relatedOPMSTarget?.indicatorNumber ?? (target.relatedOpmsTargetPublicId ? 'Linked' : null);
}
