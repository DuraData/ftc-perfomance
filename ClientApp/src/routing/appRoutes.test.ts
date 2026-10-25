import { describe, expect, it } from 'vitest';
import appSource from '../app.tsx?raw';
import { EXACT_APPLICATION_PATHS, isKnownApplicationPath } from './appRoutes';

describe('application route catalogue', () => {
  it('stays synchronized with every exact route rendered by App', () => {
    const renderedPaths = [...appSource.matchAll(/case '([^']+)':/g)].map(match => match[1]).sort();
    expect([...new Set(renderedPaths)]).toEqual([...EXACT_APPLICATION_PATHS].sort());
    expect(EXACT_APPLICATION_PATHS).toHaveLength(108);
  });

  it('recognizes governed dynamic detail, create, and edit routes', () => {
    for (const path of [
      '/opms/targets/new',
      '/opms/targets/target-public-id',
      '/opms/targets/target-public-id/edit',
      '/ipms/targets/target-public-id',
      '/ipms/targets/target-public-id/edit',
      '/opms/library/new',
      '/opms/library/template-public-id',
      '/opms/library/template-public-id/edit',
      '/ipms/library/template-public-id',
      '/ipms/library/template-public-id/edit',
    ]) expect(isKnownApplicationPath(path)).toBe(true);
  });

  it('rejects unknown, malformed, and prefix-confusable routes', () => {
    for (const path of [
      '/reports/not-a-report',
      '/dashboard/forged-child',
      '/opms/targets/item/edit/extra',
      '/opms/targets/new/edit',
      '/opms/targets/',
      '/opms/library/item/unknown',
      '/ipms/library/new/edit',
      '/unknown',
      '/',
    ]) expect(isKnownApplicationPath(path)).toBe(false);
  });
});
