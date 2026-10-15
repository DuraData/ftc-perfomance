import { describe, expect, it } from 'vitest';
import { relatedOpmsDisplayLabel } from './ipmsRelationship';

describe('relatedOpmsDisplayLabel', () => {
  it('does not report a public-id-only OPMS relationship as unlinked', () => {
    expect(relatedOpmsDisplayLabel({ relatedOpmsTargetPublicId: 'opms-public-id' })).toBe('Linked');
  });

  it('prefers the related OPMS indicator number when the expanded relationship is available', () => {
    expect(relatedOpmsDisplayLabel({
      relatedOpmsTargetPublicId: 'opms-public-id',
      relatedOPMSTarget: { indicatorNumber: 'OPMS-C88-001' } as never,
    })).toBe('OPMS-C88-001');
  });

  it('returns no label when the target is not linked', () => {
    expect(relatedOpmsDisplayLabel({})).toBeNull();
  });
});
