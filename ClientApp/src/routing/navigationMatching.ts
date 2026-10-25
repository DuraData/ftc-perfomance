import type { MenuItem, NavigationMatchMode } from '../types';
import { canonicalNavigationPath, normalizeRoutePath } from './routeAliases';

type NavigationCandidate = {
  item: MenuItem;
  ancestors: MenuItem[];
  normalizedRoute: string;
  score: number;
};

export type NavigationSelection = {
  activeCode: string | null;
  activeItem: MenuItem | null;
  activeAncestorCodes: ReadonlySet<string>;
};

function itemIdentity(item: MenuItem) {
  return item.code ?? item.path ?? item.label;
}

function matchMode(item: MenuItem): NavigationMatchMode {
  const value = item.matchMode?.toLowerCase();
  if (value === 'exact') return 'Exact';
  if (value === 'pattern') return 'Pattern';
  return 'Prefix';
}

function patternMatches(pattern: string, pathname: string) {
  const escaped = pattern
    .split('/')
    .map(segment => {
      if (segment === '*') return '.*';
      if (segment.startsWith(':')) return '[^/]+';
      return segment.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    })
    .join('/');
  return new RegExp(`^${escaped}/?$`, 'i').test(pathname);
}

export function isNavigationRouteMatch(item: MenuItem, currentLocation: string) {
  if (!item.path) return false;
  const pathname = canonicalNavigationPath(currentLocation);
  const route = canonicalNavigationPath(item.path);
  switch (matchMode(item)) {
    case 'Exact':
      return pathname === route;
    case 'Pattern':
      return patternMatches(route, pathname);
    case 'Prefix':
      return pathname === route || (route !== '/' && pathname.startsWith(`${route}/`));
  }
}

function candidateScore(item: MenuItem) {
  const route = canonicalNavigationPath(item.path ?? '/');
  const staticSegments = route.split('/').filter(segment => segment && !segment.startsWith(':') && segment !== '*').length;
  const modeWeight = matchMode(item) === 'Exact' ? 3 : matchMode(item) === 'Pattern' ? 2 : 1;
  return staticSegments * 10000 + route.length * 10 + modeWeight;
}

function collectLeafCandidates(items: MenuItem[], currentLocation: string, ancestors: MenuItem[] = []): NavigationCandidate[] {
  return items.flatMap(item => {
    const children = item.children ?? [];
    if (children.length > 0) return collectLeafCandidates(children, currentLocation, [...ancestors, item]);
    if (!isNavigationRouteMatch(item, currentLocation)) return [];
    return [{ item, ancestors, normalizedRoute: canonicalNavigationPath(item.path ?? '/'), score: candidateScore(item) }];
  });
}

export function resolveNavigationSelection(items: MenuItem[], currentLocation: string): NavigationSelection {
  const candidates = collectLeafCandidates(items, currentLocation).sort((left, right) =>
    right.score - left.score || itemIdentity(left.item).localeCompare(itemIdentity(right.item)));
  const winner = candidates[0] ?? null;

  if (import.meta.env.DEV && winner && candidates[1]?.score === winner.score) {
    console.warn('Ambiguous navigation ownership detected.', {
      pathname: normalizeRoutePath(currentLocation),
      navigationCodes: candidates.filter(candidate => candidate.score === winner.score).map(candidate => itemIdentity(candidate.item)),
      routes: candidates.filter(candidate => candidate.score === winner.score).map(candidate => candidate.normalizedRoute),
    });
  }

  return {
    activeCode: winner ? itemIdentity(winner.item) : null,
    activeItem: winner?.item ?? null,
    activeAncestorCodes: new Set(winner?.ancestors.map(itemIdentity) ?? []),
  };
}

export function isNavigationItemActive(item: MenuItem, selection: NavigationSelection) {
  return selection.activeCode === itemIdentity(item);
}

export function hasActiveNavigationDescendant(item: MenuItem, selection: NavigationSelection) {
  return selection.activeAncestorCodes.has(itemIdentity(item));
}
