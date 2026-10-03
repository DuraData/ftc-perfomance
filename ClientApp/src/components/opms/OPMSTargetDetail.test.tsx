import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { OPMSTarget } from '../../types';
import { AssigneesTab, VoteNumbersTab } from './OPMSTargetDetail';

const liveTarget = {
  department: { id: '17', name: 'Live Water Services' },
  voteNumbers: [{ id: 'vote-live', number: 'V-100', name: 'Live Capital Vote', amount: 2500 }],
  assignedTo: { id: 'employee-one', firstName: 'Live', lastName: 'Owner', displayName: 'Live Owner', email: 'owner@example.test' },
  additionalAssignees: [{ id: 'employee-two', firstName: 'Live', lastName: 'Support', displayName: 'Live Support', email: 'support@example.test' }],
} as OPMSTarget;

describe('OPMS target relational tabs', () => {
  it('renders vote numbers from the target API model', () => {
    render(<VoteNumbersTab target={liveTarget} />);

    expect(screen.getByText('V-100')).toBeInTheDocument();
    expect(screen.getByText('Live Capital Vote')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add' })).not.toBeInTheDocument();
  });

  it('renders only target assignees and removes duplicate actors', () => {
    render(<AssigneesTab target={{ ...liveTarget, additionalAssignees: [liveTarget.assignedTo!, ...liveTarget.additionalAssignees] }} />);

    expect(screen.getAllByText('Live Owner')).toHaveLength(1);
    expect(screen.getByText('Live Support')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add' })).not.toBeInTheDocument();
  });
});
