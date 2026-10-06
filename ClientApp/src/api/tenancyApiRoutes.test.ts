import { getMyTenantContextsPage } from './api';

describe('tenant context API routes', () => {
  it('encodes bounded directory paging and exact selected-context recovery', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getMyTenantContextsPage({ page: 2, pageSize: 25, search: ' metro ', sortBy: 'name', sortDirection: 'asc' });
    await getMyTenantContextsPage({ page: 1, pageSize: 1, sortBy: 'name', sortDirection: 'asc' }, 42);

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/tenancy/my-contexts/page?page=2&pageSize=25&search=metro&sortBy=name&sortDirection=asc'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/tenancy/my-contexts/page?page=1&pageSize=1&sortBy=name&sortDirection=asc&municipalityId=42'), expect.objectContaining({ credentials: 'include' }));
  });
});
