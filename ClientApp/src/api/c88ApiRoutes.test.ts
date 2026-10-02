import { createC88Mapping, finalSubmitC88Report, getC88Workspace, submitC88Report, verifyC88Report } from './api';

describe('Circular 88 API routes', () => {
  it('uses only versioned C88 routes and keeps workflow evidence in POST bodies', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: {} }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getC88Workspace('year-1');
    await createC88Mapping({ configurationPublicId: 'config-1', indicatorPublicId: 'indicator-1', opmsTargetPublicId: 'target-1', mappingType: 'Direct', reason: 'Alignment only' });
    await submitC88Report('report-1', 'AQ==', 'Submit');
    await verifyC88Report('report-1', 'Ag==', 'Verify');
    await finalSubmitC88Report('report-1', 'Aw==', 'Final');

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/c88/workspace?municipalityFinancialYearPublicId=year-1'), expect.objectContaining({ credentials: 'include' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/c88/mappings'), expect.objectContaining({ method: 'POST', body: expect.stringContaining('"opmsTargetPublicId":"target-1"') }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/c88/reports/report-1/submit'), expect.objectContaining({ method: 'POST', body: JSON.stringify({ rowVersion: 'AQ==', reason: 'Submit' }) }));
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/c88/reports/report-1/verify'), expect.objectContaining({ method: 'POST' }));
    expect(fetchMock).toHaveBeenNthCalledWith(5, expect.stringContaining('/v1/c88/reports/report-1/final-submit'), expect.objectContaining({ method: 'POST' }));
  });
});
