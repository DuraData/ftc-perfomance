import { render, screen } from '@testing-library/react';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { TenantScopeBoundary } from './TenantScopeBoundary';

function TenantWorkspace({ onMount }: { onMount: () => void }) {
  useState(() => {
    onMount();
    return true;
  });
  return <div>Tenant workspace</div>;
}

describe('TenantScopeBoundary', () => {
  it('remounts tenant-scoped workspaces when the municipality context changes', () => {
    const onMount = vi.fn();
    const { rerender } = render(
      <TenantScopeBoundary municipalityPublicId="municipality-a">
        <TenantWorkspace onMount={onMount} />
      </TenantScopeBoundary>,
    );

    expect(screen.getByText('Tenant workspace')).toBeInTheDocument();
    expect(onMount).toHaveBeenCalledTimes(1);

    rerender(
      <TenantScopeBoundary municipalityPublicId="municipality-b">
        <TenantWorkspace onMount={onMount} />
      </TenantScopeBoundary>,
    );

    expect(onMount).toHaveBeenCalledTimes(2);
  });

  it('does not remount while the municipality context is unchanged', () => {
    const onMount = vi.fn();
    const { rerender } = render(
      <TenantScopeBoundary municipalityPublicId="municipality-a">
        <TenantWorkspace onMount={onMount} />
      </TenantScopeBoundary>,
    );

    rerender(
      <TenantScopeBoundary municipalityPublicId="municipality-a">
        <TenantWorkspace onMount={onMount} />
      </TenantScopeBoundary>,
    );

    expect(onMount).toHaveBeenCalledTimes(1);
  });
});
