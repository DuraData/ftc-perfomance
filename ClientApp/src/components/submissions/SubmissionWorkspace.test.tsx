import { fireEvent, render, screen } from '@testing-library/react';
import type { OPMSSubmission } from '../../types';
import { SubmissionWorkspace } from './SubmissionWorkspace';

const security = vi.hoisted(() => ({
  canUpdate: vi.fn(),
  canReadField: vi.fn(),
  canEditField: vi.fn(),
  canExecute: vi.fn(),
}));

vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));

const submission = {
  id: '7',
  baseState: 'IN_PROGRESS',
  quarter: 'Q1',
  dueDate: '2026-10-31T00:00:00Z',
  actual: 50,
  actualPerformance: '50',
  variance: 5,
  varianceReason: 'Private reason',
  correctiveMeasure: 'Private corrective action',
  status: 'draft',
  target: {
    targetName: 'Water target',
    period: { fiscalYear: '2026/27' },
    department: { name: 'Infrastructure' },
    unitOfMeasure: { name: 'Percentage', symbol: '%' },
  },
  attachments: [],
  comments: [],
  history: [],
} as unknown as OPMSSubmission;

describe('SubmissionWorkspace member security', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Element.prototype.scrollIntoView = vi.fn();
    security.canUpdate.mockReturnValue(true);
    security.canReadField.mockReturnValue(false);
    security.canEditField.mockReturnValue(false);
    security.canExecute.mockReturnValue(false);
  });

  it('does not render protected values or editing controls when member access is denied', () => {
    render(<SubmissionWorkspace submission={submission} submissionType="OPMS" />);

    expect(screen.getByText('Restricted')).toBeInTheDocument();
    expect(screen.queryByText('Private reason')).not.toBeInTheDocument();
    expect(screen.queryByText('Private corrective action')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument();
  });

  it('renders readable members and enables only fields with member update permission', () => {
    security.canReadField.mockImplementation((_resource: string, member: string) => member === 'VarianceReason' || member === 'CorrectiveMeasure');
    security.canEditField.mockImplementation((_resource: string, member: string) => member === 'VarianceReason');
    render(<SubmissionWorkspace submission={submission} submissionType="OPMS" />);

    fireEvent.click(screen.getByRole('button', { name: 'Edit' }));

    expect(screen.getByDisplayValue('Private reason')).toBeInTheDocument();
    expect(screen.queryByDisplayValue('Private corrective action')).not.toBeInTheDocument();
    expect(screen.getByText('Private corrective action')).toBeInTheDocument();
  });
});
