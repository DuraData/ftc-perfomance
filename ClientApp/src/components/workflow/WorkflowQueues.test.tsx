import { render, screen, fireEvent } from '@testing-library/react';
import { AppShell } from '../layout/AppShell';
import { WorkflowQueues } from './WorkflowQueues';

vi.mock('../layout/AppShell', () => ({
  AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

describe('WorkflowQueues component', () => {
  it('renders queue cards and opens selected queue details', () => {
    render(<WorkflowQueues />);

    const queueCard = screen.getByText(/My Submissions/i);
    expect(queueCard).toBeInTheDocument();

    fireEvent.click(queueCard);
    expect(screen.getByText(/Close/i)).toBeInTheDocument();
  });
});
