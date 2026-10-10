import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { IdpPlanPicker } from './IdpPlanPicker';

const api = vi.hoisted(() => ({ getIdpPlansPage: vi.fn() }));
vi.mock('../../api/api', () => api);

const plan = (id: number) => ({
  publicId: `plan-public-${id}`,
  municipalityName: 'Blue Hills',
  planTitle: `Plan ${id}`,
  planCode: `IDP-${id}`,
  startFinancialYear: 2026,
  endFinancialYear: 2031,
  status: 'Published',
  currentVersionNumber: 1,
  createdAt: '2026-07-01T00:00:00Z',
  rowVersion: 'AQ==',
  planFamilyId: `family-${id}`,
  effectiveFrom: '2026-07-01T00:00:00Z',
});

describe('IdpPlanPicker', () => {
  it('searches and pages all plan options while retaining an off-page selection', async () => {
    api.getIdpPlansPage.mockImplementation(async ({ page = 1, search }: { page?: number; search?: string }) => ({
      success: true,
      data: { items: [plan(page)], page, pageSize: 25, totalCount: search ? 1 : 26, totalPages: search ? 1 : 2 },
    }));
    const onChange = vi.fn();
    const { rerender } = render(<IdpPlanPicker label="IDP plan" value="" onChange={onChange} />);

    await waitFor(() => expect(api.getIdpPlansPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: undefined, sortBy: 'createdAt', sortDirection: 'desc' }));
    await screen.findByRole('option', { name: 'IDP-1 - Plan 1' });
    fireEvent.change(screen.getByLabelText('IDP plan'), { target: { value: 'plan-public-1' } });
    expect(onChange).toHaveBeenCalledWith('plan-public-1', expect.objectContaining({ publicId: 'plan-public-1' }));
    rerender(<IdpPlanPicker label="IDP plan" value="plan-public-1" onChange={onChange} />);

    fireEvent.click(screen.getByRole('button', { name: 'Next plans' }));
    await waitFor(() => expect(api.getIdpPlansPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, search: undefined, sortBy: 'createdAt', sortDirection: 'desc' }));
    expect(screen.getByLabelText('IDP plan')).toHaveValue('plan-public-1');
    expect(screen.getByRole('option', { name: 'IDP-1 - Plan 1' })).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('IDP plan search'), { target: { value: 'specific plan' } });
    await waitFor(() => expect(api.getIdpPlansPage).toHaveBeenLastCalledWith({ page: 1, pageSize: 25, search: 'specific plan', sortBy: 'createdAt', sortDirection: 'desc' }), { timeout: 1500 });
  });
});
