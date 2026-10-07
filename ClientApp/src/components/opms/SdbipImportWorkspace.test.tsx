import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { SdbipImportWorkspace } from './SdbipImportWorkspace';

const security = vi.hoisted(() => ({ canReadField: vi.fn(() => false) }));
const api = vi.hoisted(() => ({
  commitOpmsImport: vi.fn(), downloadOpmsImportCsv: vi.fn(), getOpmsImportBatch: vi.fn(),
  getOpmsImportBatchesPage: vi.fn(), stageOpmsImport: vi.fn(),
}));

vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));
vi.mock('../../api/api', () => api);
vi.mock('../common/CalendarMasterPicker', () => ({
  CalendarMasterPicker: ({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) =>
    <label>{label}<select aria-label={label} value={value} onChange={event => onChange(event.target.value)}><option value="">Select</option><option value="layer-1">Top layer</option></select></label>,
}));

const hostileBatch = {
  publicId: 'batch-1', clientRequestId: 'SECRET-REQUEST', sdbipLayerPublicId: 'layer-1',
  sourceFileName: 'SECRET-FILE.csv', sourceSha256: 'SECRET-HASH', status: 'Staged', totalRows: 1,
  newRows: 0, unchangedRows: 0, changedRows: 0, invalidRows: 1,
  createdByUserPublicId: 'actor-public-id', createdByName: 'SECRET-ACTOR', createdAt: '2026-10-07T00:00:00Z',
  committedByUserPublicId: null, committedByName: null, committedAt: null, rowVersion: 'AQ==',
  rows: [{ publicId: 'row-1', sourceRowNumber: 2, reference: 'KPI-1', status: 'Invalid', existingValueJson: 'SECRET-BEFORE', normalizedJson: 'SECRET-AFTER', suppliedValue: 'SECRET-SUPPLIED', errorCode: 'SECRET-CODE', errorPeriod: 'SECRET-PERIOD', errorField: 'SECRET-FIELD', errorMessage: 'SECRET-DIAGNOSTIC' }],
};

describe('SDBIP import workspace member security', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canReadField.mockReturnValue(false);
    api.getOpmsImportBatchesPage.mockResolvedValue({ success: true, data: { items: [hostileBatch], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });
    api.getOpmsImportBatch.mockResolvedValue({ success: true, data: hostileBatch });
    api.downloadOpmsImportCsv.mockResolvedValue({ success: true, data: true });
  });

  it('does not render denied metadata, diagnostics, search or file sorting from a hostile payload', async () => {
    render(<SdbipImportWorkspace />);
    fireEvent.change(screen.getByRole('combobox', { name: 'SDBIP layer' }), { target: { value: 'layer-1' } });
    const historyButton = await screen.findByRole('button', { name: /Staged · 1 rows/i });
    expect(historyButton).not.toHaveTextContent('SECRET-FILE');
    expect(screen.queryByRole('textbox', { name: 'Search SDBIP import history' })).not.toBeInTheDocument();
    expect(screen.getByRole('combobox', { name: 'Sort SDBIP import history' })).not.toHaveTextContent('File name');

    fireEvent.click(historyButton);
    await waitFor(() => expect(api.getOpmsImportBatch).toHaveBeenCalledWith('batch-1'));
    for (const secret of ['SECRET-REQUEST', 'SECRET-FILE', 'SECRET-HASH', 'SECRET-ACTOR', 'SECRET-DIAGNOSTIC', 'SECRET-CODE', 'SECRET-FIELD', 'SECRET-SUPPLIED', 'SECRET-BEFORE', 'SECRET-AFTER'])
      expect(screen.queryByText(new RegExp(secret))).not.toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: 'Diagnostic' })).not.toBeInTheDocument();
  });
});
