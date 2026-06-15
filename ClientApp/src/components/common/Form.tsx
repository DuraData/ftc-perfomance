import React from 'react';
import { AlertCircle, CheckCircle } from 'lucide-react';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  success?: string;
  helpText?: string;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

export function Input({
  label,
  error,
  success,
  helpText,
  leftIcon,
  rightIcon,
  className = '',
  id,
  ...props
}: InputProps) {
  const inputId = id || `input-${Math.random().toString(36).substr(2, 9)}`;

  return (
    <div className="space-y-0.5">
      {label && (
        <label htmlFor={inputId} className="block text-xs font-medium text-secondary-700 dark:text-secondary-300">
          {label}
          {props.required && <span className="text-error-500 ml-0.5">*</span>}
        </label>
      )}
      <div className="relative">
        {leftIcon && (
          <div className="absolute left-2.5 top-1/2 -translate-y-1/2 text-secondary-400">
            {leftIcon}
          </div>
        )}
        <input
          id={inputId}
          className={`w-full px-2.5 py-1.5 text-sm bg-white dark:bg-secondary-800 border rounded transition-colors focus:outline-none focus:ring-1.5 ${
            error
              ? 'border-error-500 focus:ring-error-500'
              : success
              ? 'border-success-500 focus:ring-success-500'
              : 'border-secondary-300 dark:border-secondary-600 focus:ring-primary-500 focus:border-primary-500'
          } ${leftIcon ? 'pl-8' : ''} ${rightIcon ? 'pr-8' : ''} ${className}`}
          {...props}
        />
        {rightIcon && (
          <div className="absolute right-2.5 top-1/2 -translate-y-1/2 text-secondary-400">
            {rightIcon}
          </div>
        )}
        {error && !rightIcon && (
          <div className="absolute right-2.5 top-1/2 -translate-y-1/2">
            <AlertCircle className="w-3.5 h-3.5 text-error-500" />
          </div>
        )}
        {success && !rightIcon && (
          <div className="absolute right-2.5 top-1/2 -translate-y-1/2">
            <CheckCircle className="w-3.5 h-3.5 text-success-500" />
          </div>
        )}
      </div>
      {error && <p className="text-xs text-error-600 dark:text-error-400">{error}</p>}
      {success && <p className="text-xs text-success-600 dark:text-success-400">{success}</p>}
      {helpText && !error && !success && <p className="text-xs text-secondary-500 dark:text-secondary-400">{helpText}</p>}
    </div>
  );
}

interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  error?: string;
  options: { value: string | number; label: string; disabled?: boolean }[];
  placeholder?: string;
}

export function Select({
  label,
  error,
  options,
  placeholder,
  className = '',
  id,
  ...props
}: SelectProps) {
  const selectId = id || `select-${Math.random().toString(36).substr(2, 9)}`;

  return (
    <div className="space-y-0.5">
      {label && (
        <label htmlFor={selectId} className="block text-xs font-medium text-secondary-700 dark:text-secondary-300">
          {label}
          {props.required && <span className="text-error-500 ml-0.5">*</span>}
        </label>
      )}
      <select
        id={selectId}
        className={`w-full px-2.5 py-1.5 text-sm bg-white dark:bg-secondary-800 border rounded transition-colors focus:outline-none focus:ring-1.5 appearance-none bg-[url('data:image/svg+xml;charset=utf-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20viewBox%3D%220%200%2020%2020%22%20fill%3D%22%236b7280%22%3E%3Cpath%20fill-rule%3D%22evenodd%22%20d%3D%22M5.293%207.293a1%201%200%20011.414%200L10%2010.586l3.293-3.293a1%201%200%20111.414%201.414l-4%204a1%201%200%2001-1.414%200l-4-4a1%201%200%20010-1.414z%22%20clip-rule%3D%22evenodd%22%2F%3E%3C%2Fsvg%3E')] bg-[length:1rem] bg-[right_0.5rem_center] bg-no-repeat ${
          error
            ? 'border-error-500 focus:ring-error-500'
            : 'border-secondary-300 dark:border-secondary-600 focus:ring-primary-500 focus:border-primary-500'
        } ${className}`}
        {...props}
      >
        {placeholder && <option value="" disabled>{placeholder}</option>}
        {options.map(option => (
          <option key={option.value} value={option.value} disabled={option.disabled}>
            {option.label}
          </option>
        ))}
      </select>
      {error && <p className="text-xs text-error-600 dark:text-error-400">{error}</p>}
    </div>
  );
}

interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
  helpText?: string;
}

export function Textarea({
  label,
  error,
  helpText,
  className = '',
  id,
  ...props
}: TextareaProps) {
  const textareaId = id || `textarea-${Math.random().toString(36).substr(2, 9)}`;

  return (
    <div className="space-y-0.5">
      {label && (
        <label htmlFor={textareaId} className="block text-xs font-medium text-secondary-700 dark:text-secondary-300">
          {label}
          {props.required && <span className="text-error-500 ml-0.5">*</span>}
        </label>
      )}
      <textarea
        id={textareaId}
        className={`w-full px-2.5 py-1.5 text-sm bg-white dark:bg-secondary-800 border rounded transition-colors focus:outline-none focus:ring-1.5 resize-none ${
          error
            ? 'border-error-500 focus:ring-error-500'
            : 'border-secondary-300 dark:border-secondary-600 focus:ring-primary-500 focus:border-primary-500'
        } ${className}`}
        {...props}
      />
      {error && <p className="text-xs text-error-600 dark:text-error-400">{error}</p>}
      {helpText && !error && <p className="text-xs text-secondary-500 dark:text-secondary-400">{helpText}</p>}
    </div>
  );
}

interface CheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: string;
  description?: string;
}

export function Checkbox({ label, description, className = '', id, ...props }: CheckboxProps) {
  const checkboxId = id || `checkbox-${Math.random().toString(36).substr(2, 9)}`;

  return (
    <div className={`flex items-start gap-2 ${className}`}>
      <input
        type="checkbox"
        id={checkboxId}
        className="mt-0.5 w-3.5 h-3.5 rounded border-secondary-300 dark:border-secondary-600 text-primary-600 focus:ring-primary-500"
        {...props}
      />
      <div>
        <label htmlFor={checkboxId} className="text-xs font-medium text-secondary-700 dark:text-secondary-300 cursor-pointer">{label}</label>
        {description && <p className="text-[10px] text-secondary-500 dark:text-secondary-400">{description}</p>}
      </div>
    </div>
  );
}

interface FormSectionProps {
  title: string;
  description?: string;
  children: React.ReactNode;
  className?: string;
  collapsible?: boolean;
  defaultCollapsed?: boolean;
}

export function FormSection({
  title,
  description,
  children,
  className = '',
}: FormSectionProps) {
  return (
    <div className={`space-y-2 ${className}`}>
      <div className="border-b border-secondary-200 dark:border-secondary-700 pb-1.5">
        <h3 className="text-sm font-semibold text-secondary-900 dark:text-white">{title}</h3>
        {description && <p className="text-xs text-secondary-500 dark:text-secondary-400">{description}</p>}
      </div>
      <div className="space-y-2">
        {children}
      </div>
    </div>
  );
}

interface FormRowProps {
  children: React.ReactNode;
  cols?: 1 | 2 | 3 | 4;
}

export function FormRow({ children, cols = 2 }: FormRowProps) {
  const colClasses = {
    1: 'grid-cols-1',
    2: 'grid-cols-1 sm:grid-cols-2',
    3: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3',
    4: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-4',
  };

  return <div className={`grid gap-2 ${colClasses[cols]}`}>{children}</div>;
}
