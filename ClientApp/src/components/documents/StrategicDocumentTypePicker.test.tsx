import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const api = vi.hoisted(() => ({ getStrategicDocumentTypesPage: vi.fn() }));
vi.mock('../../api/api', () => api);
import { StrategicDocumentTypePicker } from './StrategicDocumentTypePicker';

const type = {
  publicId: 'type-1', code: 'IDP', name: 'Integrated Development Plan', description: null,
  allowsExternalLinks: true, isActive: true, displayOrder: 10, rowVersion: 'AQ==',
};

describe('StrategicDocumentTypePicker', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getStrategicDocumentTypesPage.mockResolvedValue({
      success: true,
      data: { items: [type], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 },
    });
  });

  it('searches and pages active types through the bounded contract', async () => {
    const onChange = vi.fn();
    render(<StrategicDocumentTypePicker value="" onChange={onChange} />);
    await waitFor(() => expect(api.getStrategicDocumentTypesPage).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, pageSize: 25, sortBy: 'displayOrder', sortDirection: 'asc' }),
      { active: true },
    ));
    await waitFor(() => expect(onChange).toHaveBeenCalledWith('type-1', type));
    fireEvent.change(screen.getByLabelText('Strategic document type search'), { target: { value: 'integrated' } });
    await waitFor(() => expect(api.getStrategicDocumentTypesPage).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, search: 'integrated' }),
      { active: true },
    ));
    fireEvent.click(screen.getByRole('button', { name: 'Next document types' }));
    await waitFor(() => expect(api.getStrategicDocumentTypesPage).toHaveBeenCalledWith(
      expect.objectContaining({ page: 2, search: 'integrated' }),
      { active: true },
    ));
  });

  it('hydrates an off-page selected type without loading an array', async () => {
    api.getStrategicDocumentTypesPage.mockImplementation(async (_query: unknown, options?: { publicId?: string }) => options?.publicId
      ? { success: true, data: { items: [type], page: 1, pageSize: 1, totalCount: 1, totalPages: 1 } }
      : { success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    const onChange = vi.fn();
    render(<StrategicDocumentTypePicker value="type-1" selectedLabel="IDP · Integrated Development Plan" onChange={onChange} />);

    expect(await screen.findByRole('option', { name: 'IDP · Integrated Development Plan' })).toHaveValue('type-1');
    await waitFor(() => expect(api.getStrategicDocumentTypesPage).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, pageSize: 1 }),
      { active: true, publicId: 'type-1' },
    ));
    await waitFor(() => expect(onChange).toHaveBeenCalledWith('type-1', type));
  });
});
