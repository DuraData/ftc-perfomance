import { Input, Select, Textarea } from '../common/Form';
import type { PerformanceValueConfiguration } from '../../types';

interface PerformanceValueInputProps {
  configuration?: PerformanceValueConfiguration | null;
  value: string;
  onChange: (value: string) => void;
  label?: string;
  required?: boolean;
  disabled?: boolean;
  helpText?: string;
  error?: string;
}

function inputStep(decimalPlaces?: number | null): string {
  if (!decimalPlaces || decimalPlaces <= 0) return '1';
  return (1 / (10 ** decimalPlaces)).toFixed(decimalPlaces);
}

function configuredHelp(configuration: PerformanceValueConfiguration, helpText?: string): string {
  const details = [configuration.name];
  if (configuration.symbol) details.push(`Unit: ${configuration.symbol}`);
  if (configuration.minValue !== null && configuration.minValue !== undefined) details.push(`Minimum: ${configuration.minValue}`);
  if (configuration.maxValue !== null && configuration.maxValue !== undefined) details.push(`Maximum: ${configuration.maxValue}`);
  return [details.join(' · '), helpText].filter(Boolean).join('. ');
}

export function PerformanceValueInput({
  configuration,
  value,
  onChange,
  label = 'Actual Performance',
  required,
  disabled,
  helpText,
  error,
}: PerformanceValueInputProps) {
  if (!configuration) {
    return <Input label={label} value={value} onChange={event => onChange(event.target.value)} required={required} disabled={disabled} helpText={helpText} error={error} />;
  }

  const control = configuration.inputControlType.trim().toUpperCase();
  const dataType = configuration.valueDataType.trim().toUpperCase();
  const code = configuration.code.trim().toUpperCase();
  const configuredDescription = configuredHelp(configuration, helpText);

  if (control === 'DATE' || dataType === 'DATE' || code === 'DATE') {
    return <Input type="date" label={label} value={value} onChange={event => onChange(event.target.value)} required={required} disabled={disabled} helpText={configuredDescription} error={error} />;
  }

  if (control === 'RATIO' || code === 'RATIO' || configuration.requiresComponentUi) {
    const configuredSegments = [...(configuration.segments ?? [])].sort((left, right) => left.position - right.position);
    const segmentCount = configuredSegments.length >= 2 && configuredSegments.length <= 4
      ? configuredSegments.length
      : configuration.componentCount && configuration.componentCount >= 2 && configuration.componentCount <= 4
        ? configuration.componentCount
      : 2;
    const segments = value.split(':');
    while (segments.length < segmentCount) segments.push('');
    const visibleSegments = segments.slice(0, segmentCount);

    return (
      <fieldset className="space-y-2" disabled={disabled}>
        <legend className="block text-xs font-semibold text-secondary-700 dark:text-secondary-200">
          {label}{required && <span className="ml-0.5 text-error-500">*</span>}
        </legend>
        <div className={`grid gap-2 ${segmentCount >= 3 ? 'sm:grid-cols-2 lg:grid-cols-4' : 'sm:grid-cols-2'}`}>
          {visibleSegments.map((segment, index) => (
            <Input
              key={configuredSegments[index]?.position ?? index}
              type="number"
              label={configuredSegments[index]?.label ?? `Segment ${index + 1}`}
              aria-label={`${label} ${configuredSegments[index]?.label ?? `segment ${index + 1}`}`}
              value={segment}
              min={configuredSegments[index]?.minValue ?? configuration.minValue ?? 0}
              max={configuredSegments[index]?.maxValue ?? configuration.maxValue ?? undefined}
              step={inputStep(configuredSegments[index]?.decimalPlaces ?? configuration.decimalPlaces)}
              required={required}
              onChange={event => {
                const next = [...visibleSegments];
                next[index] = event.target.value;
                onChange(next.join(':'));
              }}
            />
          ))}
        </div>
        {error
          ? <p role="alert" className="text-xs text-error-600 dark:text-error-400">{error}</p>
          : <p className="text-xs text-secondary-500 dark:text-secondary-400">{configuredDescription}. Values are stored as one canonical colon-separated Actual Performance value.</p>}
      </fieldset>
    );
  }

  if (control === 'SELECT' && (dataType === 'BOOLEAN' || code === 'YES_NO')) {
    return (
      <div className="space-y-0.5">
        <Select
          label={label}
          value={value}
          onChange={event => onChange(event.target.value)}
          required={required}
          disabled={disabled}
          error={error}
          options={[
            { value: '', label: 'Select Yes or No' },
            { value: 'true', label: 'Yes' },
            { value: 'false', label: 'No' },
          ]}
        />
        {!error && <p className="text-xs text-secondary-500 dark:text-secondary-400">{configuredDescription}</p>}
      </div>
    );
  }

  if (control === 'SELECT') {
    const minimum = Math.ceil(configuration.minValue ?? 1);
    const maximum = Math.floor(configuration.maxValue ?? 5);
    const options = maximum >= minimum && maximum - minimum <= 20
      ? Array.from({ length: maximum - minimum + 1 }, (_, index) => ({ value: String(minimum + index), label: String(minimum + index) }))
      : [];
    return (
      <div className="space-y-0.5">
        <Select
          label={label}
          value={value}
          onChange={event => onChange(event.target.value)}
          required={required}
          disabled={disabled}
          error={error}
          options={[{ value: '', label: `Select ${configuration.name}` }, ...options]}
        />
        {!error && <p className="text-xs text-secondary-500 dark:text-secondary-400">{configuredDescription}</p>}
      </div>
    );
  }

  if (control === 'TEXT' || dataType === 'TEXT' || configuration.isQualitative) {
    return <Textarea label={label} value={value} onChange={event => onChange(event.target.value)} required={required} disabled={disabled} rows={4} maxLength={4000} helpText={configuredDescription} error={error} />;
  }

  if (['NUMERIC', 'NUMERIC_UNIT', 'CURRENCY'].includes(control) || ['DECIMAL', 'INTEGER'].includes(dataType)) {
    return (
      <Input
        type="number"
        label={label}
        value={value}
        onChange={event => onChange(event.target.value)}
        required={required}
        disabled={disabled}
        min={configuration.minValue ?? undefined}
        max={configuration.maxValue ?? undefined}
        step={dataType === 'INTEGER' ? '1' : inputStep(configuration.decimalPlaces)}
        helpText={configuredDescription}
        error={error}
      />
    );
  }

  return <Input label={label} value={value} onChange={event => onChange(event.target.value)} required={required} disabled={disabled} helpText={configuredDescription} error={error} />;
}
