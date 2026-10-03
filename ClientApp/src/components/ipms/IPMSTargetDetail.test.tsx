import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { IPMSTarget } from '../../types';
import { GeneralInfoTab } from './IPMSTargetDetail';

describe('IPMS target detail', () => {
  it('renders target relationships from the API model instead of fixture options', () => {
    const target = {
      period: { id: 'period-live', name: 'Live Quarter 1' },
      department: { id: 'department-live', name: 'Live Finance' },
      unit: { id: 'unit-live', name: 'Live Budget Unit' },
      assignedTo: { id: 'employee-live', displayName: 'Live Employee' },
      indicatorNumber: 'IPMS-LIVE-1',
      targetName: 'Live Employee KPI',
      kpiDescription: 'Live KPI description',
      targetUnitType: 'percentage',
      unitOfMeasure: { id: 'uom-live', name: 'Percent' },
      kpiType: 'Quantitative',
      indicatorType: 'Output',
      weight: 20,
    } as IPMSTarget;

    render(<GeneralInfoTab target={target} />);

    expect(screen.getByDisplayValue('Live Quarter 1')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Live Finance')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Live Budget Unit')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Live Employee')).toBeInTheDocument();
    expect(screen.getByDisplayValue('IPMS-LIVE-1')).toBeInTheDocument();
  });
});
