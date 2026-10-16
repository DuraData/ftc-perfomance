import { fireEvent, render, screen } from '@testing-library/react';
import { PerformanceValueInput } from './PerformanceValueInput';
import type { PerformanceValueConfiguration } from '../../types';

function configuration(overrides: Partial<PerformanceValueConfiguration>): PerformanceValueConfiguration {
  return {
    code: 'NUMBER',
    name: 'Number',
    inputControlType: 'NUMERIC',
    valueDataType: 'DECIMAL',
    decimalPlaces: 2,
    minValue: 0,
    maxValue: null,
    supportsAutoVariance: true,
    requiresComponentUi: false,
    isQualitative: false,
    engineUnitKind: 2,
    ...overrides,
  };
}

describe('PerformanceValueInput', () => {
  it.each([
    ['NUMBER', configuration({ code: 'NUMBER', name: 'Number' }), 'number'],
    ['PERCENT', configuration({ code: 'PERCENT', name: 'Percentage', symbol: '%', maxValue: 100, engineUnitKind: 1 }), 'number'],
    ['DATE', configuration({ code: 'DATE', name: 'Date', inputControlType: 'DATE', valueDataType: 'DATE', decimalPlaces: null, engineUnitKind: 10 }), 'date'],
  ])('renders the configured %s control', (_code, unit, expectedType) => {
    render(<PerformanceValueInput configuration={unit} value="" onChange={vi.fn()} />);

    expect(screen.getByLabelText('Actual Performance')).toHaveAttribute('type', expectedType);
  });

  it('renders controlled Yes/No values and emits the canonical boolean string', () => {
    const onChange = vi.fn();
    render(<PerformanceValueInput configuration={configuration({ code: 'YES_NO', name: 'Yes / No', inputControlType: 'SELECT', valueDataType: 'BOOLEAN', decimalPlaces: null, engineUnitKind: 9 })} value="" onChange={onChange} />);

    fireEvent.change(screen.getByRole('combobox', { name: 'Actual Performance' }), { target: { value: 'true' } });
    expect(onChange).toHaveBeenCalledWith('true');
    expect(screen.getByRole('option', { name: 'Yes' })).toHaveValue('true');
  });

  it('renders qualitative Actual Performance as narrative text', () => {
    render(<PerformanceValueInput configuration={configuration({ code: 'QUALITATIVE', name: 'Qualitative', inputControlType: 'TEXT', valueDataType: 'TEXT', decimalPlaces: null, supportsAutoVariance: false, isQualitative: true, engineUnitKind: 13 })} value="Policy adopted" onChange={vi.fn()} />);

    expect(screen.getByRole('textbox', { name: 'Actual Performance' }).tagName).toBe('TEXTAREA');
  });

  it.each([2, 3, 4])('renders %i separately validated ratio segments', (componentCount) => {
    const onChange = vi.fn();
    const values = Array.from({ length: componentCount }, (_, index) => String(index + 1));
    render(<PerformanceValueInput configuration={configuration({ code: 'RATIO', name: 'Ratio', inputControlType: 'RATIO', valueDataType: 'TEXT', decimalPlaces: 6, requiresComponentUi: true, engineUnitKind: 8, componentCount })} value={values.join(':')} onChange={onChange} />);

    const segments = screen.getAllByLabelText(/Actual Performance segment/);
    expect(segments).toHaveLength(componentCount);
    fireEvent.change(segments[componentCount - 1], { target: { value: '9' } });
    expect(onChange).toHaveBeenCalledWith([...values.slice(0, -1), '9'].join(':'));
    expect(screen.getByText(/one canonical colon-separated Actual Performance value/i)).toBeInTheDocument();
  });

  it('renders administrator-configured ratio labels and segment constraints', () => {
    render(<PerformanceValueInput configuration={configuration({
      code: 'RATIO', name: 'Ratio', inputControlType: 'RATIO', valueDataType: 'TEXT', requiresComponentUi: true, engineUnitKind: 8,
      componentCount: 2,
      ratioComparisonMethodCode: 'NORMALIZED_RATIO',
      segments: [
        { position: 1, label: 'Officers', decimalPlaces: 0, minValue: 1, maxValue: 50 },
        { position: 2, label: 'Population', decimalPlaces: 0, minValue: 1, maxValue: 10000 },
      ],
    })} value="4:3000" onChange={vi.fn()} />);

    expect(screen.getByRole('spinbutton', { name: 'Actual Performance Officers' })).toHaveAttribute('min', '1');
    expect(screen.getByRole('spinbutton', { name: 'Actual Performance Officers' })).toHaveAttribute('max', '50');
    expect(screen.getByRole('spinbutton', { name: 'Actual Performance Population' })).toHaveValue(3000);
  });
});
