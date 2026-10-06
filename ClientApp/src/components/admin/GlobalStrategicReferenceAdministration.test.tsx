import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { GlobalStrategicReferenceAdministration } from './GlobalStrategicReferenceAdministration';

const api = vi.hoisted(() => ({
  getGlobalStrategicReferencesPage: vi.fn(), saveGlobalStrategicReference: vi.fn(), setGlobalStrategicReferenceAvailability: vi.fn(),
}));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canCreate: () => true, canUpdate: () => true }) }));

describe('GlobalStrategicReferenceAdministration', () => {
  beforeEach(() => {
    api.getGlobalStrategicReferencesPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'kpa-1', code: 'BSD', name: 'Basic Service Delivery', description: 'National KPA', displayOrder: 10, isActive: true, isEnabledForMunicipality: true, availabilityPublicId: null, availabilityRowVersion: null, rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    api.saveGlobalStrategicReference.mockResolvedValue({ success: true, data: {} });
    api.setGlobalStrategicReferenceAvailability.mockResolvedValue({ success: true, data: {} });
  });

  it('edits the persisted global record with its RowVersion and governance reason', async () => {
    render(<GlobalStrategicReferenceAdministration kind="national-kpas" />);
    expect(await screen.findByText('Basic Service Delivery')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Edit' }));
    const governanceReason = screen.getAllByRole('textbox').filter(element => element.tagName === 'TEXTAREA')[1];
    fireEvent.change(governanceReason, { target: { value: 'Correct official wording' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));
    await waitFor(() => expect(api.saveGlobalStrategicReference).toHaveBeenCalledWith('national-kpas', 'kpa-1', expect.objectContaining({ code: 'BSD', rowVersion: 'AQ==', reason: 'Correct official wording' })));
  });

  it('creates an audited municipality availability override', async () => {
    render(<GlobalStrategicReferenceAdministration kind="national-kpas" />);
    await screen.findByText('Basic Service Delivery');
    fireEvent.change(screen.getByLabelText('Municipality availability reason'), { target: { value: 'Not applicable to current SDBIP' } });
    fireEvent.click(screen.getByRole('button', { name: 'Hide for municipality' }));
    await waitFor(() => expect(api.setGlobalStrategicReferenceAvailability).toHaveBeenCalledWith('national-kpas', 'kpa-1', { isEnabled: false, reason: 'Not applicable to current SDBIP', rowVersion: null }));
  });

  it('searches and pages the authoritative global register on the server', async () => {
    render(<GlobalStrategicReferenceAdministration kind="national-kpas" />);
    await screen.findByText('Basic Service Delivery');
    expect(api.getGlobalStrategicReferencesPage).toHaveBeenCalledWith('national-kpas', { page: 1, pageSize: 25, search: undefined, sortBy: 'displayOrder', sortDirection: 'asc' });
    fireEvent.change(screen.getByLabelText('Search register'), { target: { value: 'service' } });
    await waitFor(() => expect(api.getGlobalStrategicReferencesPage).toHaveBeenLastCalledWith('national-kpas', expect.objectContaining({ page: 1, search: 'service' })));
    fireEvent.click(screen.getByRole('button', { name: 'Next references' }));
    await waitFor(() => expect(api.getGlobalStrategicReferencesPage).toHaveBeenLastCalledWith('national-kpas', expect.objectContaining({ page: 2, search: 'service' })));
  });
});
