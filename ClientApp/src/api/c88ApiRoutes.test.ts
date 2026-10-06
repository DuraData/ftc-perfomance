import { createC88Mapping, finalSubmitC88Report, getC88AssignmentsPage, getC88CalendarsPage, getC88ComplianceQuestionsPage, getC88MappingsPage, getC88PlansPage, getC88WorkflowsPage, getC88Workspace, submitC88Report, verifyC88Report } from './api';

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

  it('uses bounded encoded C88 secondary registers', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ success: true, data: { items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0 } }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetchMock);

    await getC88PlansPage({ page: 2, pageSize: 10, search: ' service ', sortBy: 'indicatorCode', sortDirection: 'asc' }, { municipalityFinancialYearPublicId: 'year/id', configurationPublicId: 'config/id', indicatorPublicId: 'indicator/id' });
    await getC88AssignmentsPage({ page: 2, pageSize: 10, search: ' owner ', sortBy: 'employeeName', sortDirection: 'asc' }, { municipalityFinancialYearPublicId: 'year/id', active: true });
    await getC88MappingsPage({ page: 3, pageSize: 10, search: ' opms ', sortBy: 'opmsIndicator', sortDirection: 'desc' }, { municipalityFinancialYearPublicId: 'year/id', indicatorPublicId: 'indicator/id' });
    await getC88CalendarsPage({ page: 4, pageSize: 10, search: ' quarter ', sortBy: 'dueAt', sortDirection: 'asc' }, { configurationPublicId: 'config/id', active: true });
    await getC88WorkflowsPage({ page: 5, pageSize: 10, search: ' verify ', sortBy: 'versionNumber', sortDirection: 'desc' }, { municipalityFinancialYearPublicId: 'year/id', configurationPublicId: 'config/id', current: true });
    await getC88ComplianceQuestionsPage({ page: 6, pageSize: 10, search: ' source ', sortBy: 'sequence', sortDirection: 'asc' }, { catalogueVersionPublicId: 'version/id', reportTypePublicId: 'report/id', active: true, required: true });

    expect(fetchMock).toHaveBeenNthCalledWith(1, expect.stringContaining('/v1/c88/plans/page?page=2&pageSize=10&search=service&sortBy=indicatorCode&sortDirection=asc&municipalityFinancialYearPublicId=year%2Fid&configurationPublicId=config%2Fid&indicatorPublicId=indicator%2Fid'), expect.anything());
    expect(fetchMock).toHaveBeenNthCalledWith(2, expect.stringContaining('/v1/c88/assignments/page?page=2&pageSize=10&search=owner&sortBy=employeeName&sortDirection=asc&municipalityFinancialYearPublicId=year%2Fid&active=true'), expect.anything());
    expect(fetchMock).toHaveBeenNthCalledWith(3, expect.stringContaining('/v1/c88/mappings/page?page=3&pageSize=10&search=opms&sortBy=opmsIndicator&sortDirection=desc&municipalityFinancialYearPublicId=year%2Fid&indicatorPublicId=indicator%2Fid'), expect.anything());
    expect(fetchMock).toHaveBeenNthCalledWith(4, expect.stringContaining('/v1/c88/calendars/page?page=4&pageSize=10&search=quarter&sortBy=dueAt&sortDirection=asc&configurationPublicId=config%2Fid&active=true'), expect.anything());
    expect(fetchMock).toHaveBeenNthCalledWith(5, expect.stringContaining('/v1/c88/workflows/page?page=5&pageSize=10&search=verify&sortBy=versionNumber&sortDirection=desc&municipalityFinancialYearPublicId=year%2Fid&configurationPublicId=config%2Fid&current=true'), expect.anything());
    expect(fetchMock).toHaveBeenNthCalledWith(6, expect.stringContaining('/v1/c88/compliance-questions/page?page=6&pageSize=10&search=source&sortBy=sequence&sortDirection=asc&catalogueVersionPublicId=version%2Fid&reportTypePublicId=report%2Fid&active=true&required=true'), expect.anything());
  });
});
