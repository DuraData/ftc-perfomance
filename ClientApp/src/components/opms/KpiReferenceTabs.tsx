import { useEffect, useState } from 'react';
import {
  getC88AssignmentsPage,
  getC88IndicatorsPage,
  getC88MappingsPage,
  getC88PlansPage,
  getTidHistoryPage,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { C88Assignment, C88Configuration, C88Indicator, C88IndicatorPlan, C88Mapping, OPMSTarget, TidVersion } from '../../types';
import { Badge, Button, Card, EmptyState } from '../ui';

const Value = ({ label, value }: { label: string; value?: string | number | null }) => (
  <div>
    <p className="text-[10px] uppercase tracking-wide text-secondary-500">{label}</p>
    <p className="whitespace-pre-wrap text-sm text-secondary-900 dark:text-white">{value === null || value === undefined || value === '' ? 'Not recorded' : value}</p>
  </div>
);

export function KpiStrategicAlignmentTab({ target }: { target: OPMSTarget }) {
  const { setCurrentPath } = useApp();
  const { canRead } = useSecurity();
  const hasIdpReference = Boolean(target.idpReference?.trim());
  return (
    <div className="space-y-4">
      <Card>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h3 className="font-semibold text-secondary-900 dark:text-white">Strategic traceability</h3>
            <p className="mt-1 text-sm text-secondary-600 dark:text-secondary-300">IDP and Circular 88 mappings provide context only. Missing optional mappings do not block reporting or workflow.</p>
          </div>
          <Badge variant={hasIdpReference ? 'success' : 'default'}>IDP Alignment: {hasIdpReference ? 'Recorded' : 'Not Mapped'}</Badge>
        </div>
        <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          <Value label="National KPA" value={target.nationalKPA} />
          <Value label="Municipal KPA" value={target.municipalKPA} />
          <Value label="Strategic goal" value={target.strategicGoalName ?? target.strategicGoal?.name} />
          <Value label="Strategic intervention" value={target.strategicIntervention} />
          <Value label="Strategic objective" value={target.strategicObjectiveName ?? target.strategicObjective?.name} />
          <Value label="Performance objective" value={target.performanceObjectiveName ?? target.performanceObjective} />
          <Value label="IDP reference" value={target.idpReference} />
        </div>
      </Card>
      <div className="flex flex-wrap gap-2">
        {canRead('IDP_ALIGNMENT') ? <Button variant="outline" size="sm" onClick={() => setCurrentPath('/idp/alignment')}>Open IDP Alignment Matrix</Button> : null}
        {canRead('C88_INDICATOR') ? <Button variant="outline" size="sm" onClick={() => setCurrentPath('/c88/mapping')}>Open Circular 88 Mapping</Button> : null}
      </div>
    </div>
  );
}

export function KpiTidReferenceTab({ targetPublicId, configurationError }: { targetPublicId: string; configurationError?: string }) {
  const [versions, setVersions] = useState<TidVersion[]>([]);
  const [error, setError] = useState(configurationError);
  const [loading, setLoading] = useState(!configurationError);

  useEffect(() => {
    let cancelled = false;
    if (configurationError) {
      setError(configurationError);
      setLoading(false);
      return () => { cancelled = true; };
    }
    setLoading(true);
    void getTidHistoryPage(targetPublicId, { page: 1, pageSize: 25, sortBy: 'versionNumber', sortDirection: 'desc' }).then(result => {
      if (cancelled) return;
      if (!result.success || !result.data) {
        setError(result.message ?? 'Unable to load the technical indicator description.');
        setVersions([]);
      } else {
        setError(undefined);
        setVersions(result.data.items);
      }
      setLoading(false);
    });
    return () => { cancelled = true; };
  }, [configurationError, targetPublicId]);

  if (loading) return <Card><p className="text-sm text-secondary-500">Loading technical indicator description...</p></Card>;
  if (error) return <Card><p role="alert" className="text-sm text-error-700 dark:text-error-300">{error}</p></Card>;
  const current = versions.find(item => item.isCurrent) ?? versions[0];
  if (!current) return <Card><EmptyState title="No TID recorded" description="TID is enabled for this municipality, but this KPI has no current technical indicator description." /></Card>;

  return (
    <div className="space-y-4">
      <Card>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div><h3 className="font-semibold text-secondary-900 dark:text-white">Municipality technical indicator description</h3><p className="text-xs text-secondary-500">Operational reference · view only</p></div>
          <Badge variant="success">Current v{current.versionNumber}</Badge>
        </div>
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          <Value label="Indicator definition" value={current.indicatorDefinition} />
          <Value label="Purpose" value={current.purpose} />
          <Value label="Data source" value={current.dataSource} />
          <Value label="Collection method" value={current.collectionMethod} />
          <Value label="Calculation method" value={current.calculationMethod} />
          <Value label="Verification method" value={current.verificationMethod} />
          <Value label="Numerator" value={current.numeratorDescription} />
          <Value label="Denominator" value={current.denominatorDescription} />
          <Value label="Limitations" value={current.limitations} />
          <Value label="Assumptions" value={current.assumptions} />
          <Value label="Responsible employee" value={current.responsibleEmployeeName} />
          <Value label="Effective from" value={new Date(current.effectiveFrom).toLocaleDateString()} />
        </div>
      </Card>
      <Card>
        <h3 className="font-semibold text-secondary-900 dark:text-white">Version history</h3>
        <div className="mt-3 space-y-2">
          {versions.map(version => <div key={version.publicId} className="rounded border border-secondary-200 p-3 dark:border-secondary-700">
            <div className="flex flex-wrap items-center justify-between gap-2"><span className="text-sm font-medium">Version {version.versionNumber}</span><Badge variant={version.isCurrent ? 'success' : 'default'}>{version.isCurrent ? 'Current' : 'Historical'}</Badge></div>
            <p className="mt-1 text-xs text-secondary-500">Effective {new Date(version.effectiveFrom).toLocaleDateString()}{version.effectiveTo ? ` to ${new Date(version.effectiveTo).toLocaleDateString()}` : ''}</p>
            {version.notes ? <p className="mt-2 text-sm text-secondary-600 dark:text-secondary-300">{version.notes}</p> : null}
            {version.sourceDocuments.length ? <p className="mt-2 text-xs text-secondary-500">{version.sourceDocuments.length} governed source document{version.sourceDocuments.length === 1 ? '' : 's'}</p> : null}
          </div>)}
        </div>
      </Card>
    </div>
  );
}

interface C88Reference {
  mapping: C88Mapping;
  indicator?: C88Indicator;
  assignments: C88Assignment[];
  plan?: C88IndicatorPlan;
}

export function KpiCircular88ReferenceTab({ targetPublicId, configuration, configurationError }: { targetPublicId: string; configuration?: C88Configuration; configurationError?: string }) {
  const [references, setReferences] = useState<C88Reference[]>([]);
  const [error, setError] = useState(configurationError);
  const [loading, setLoading] = useState(!configurationError);

  useEffect(() => {
    let cancelled = false;
    if (configurationError || !configuration) {
      setError(configurationError ?? 'Circular 88 configuration is unavailable for this KPI financial year.');
      setLoading(false);
      return () => { cancelled = true; };
    }
    setLoading(true);
    const load = async () => {
      const mappingResult = await getC88MappingsPage(
        { page: 1, pageSize: 25, sortBy: 'indicatorCode', sortDirection: 'asc' },
        { municipalityFinancialYearPublicId: configuration.municipalityFinancialYearPublicId, opmsTargetPublicId: targetPublicId, active: true },
      );
      if (!mappingResult.success || !mappingResult.data) {
        if (!cancelled) { setError(mappingResult.message ?? 'Unable to load Circular 88 mappings.'); setReferences([]); setLoading(false); }
        return;
      }
      const detail = await Promise.all(mappingResult.data.items.map(async mapping => {
        const [indicatorResult, assignmentResult, planResult] = await Promise.all([
          getC88IndicatorsPage({ page: 1, pageSize: 1, sortBy: 'code', sortDirection: 'asc' }, { indicatorPublicId: mapping.indicatorPublicId, active: true }),
          getC88AssignmentsPage({ page: 1, pageSize: 25, sortBy: 'effectiveFrom', sortDirection: 'desc' }, { municipalityFinancialYearPublicId: configuration.municipalityFinancialYearPublicId, indicatorPublicId: mapping.indicatorPublicId, active: true }),
          getC88PlansPage({ page: 1, pageSize: 1, sortBy: 'indicatorCode', sortDirection: 'asc' }, { configurationPublicId: configuration.publicId, indicatorPublicId: mapping.indicatorPublicId }),
        ]);
        return {
          mapping,
          indicator: indicatorResult.success ? indicatorResult.data?.items[0] : undefined,
          assignments: assignmentResult.success ? assignmentResult.data?.items ?? [] : [],
          plan: planResult.success ? planResult.data?.items[0] : undefined,
        };
      }));
      if (!cancelled) { setReferences(detail); setError(undefined); setLoading(false); }
    };
    void load();
    return () => { cancelled = true; };
  }, [configuration, configurationError, targetPublicId]);

  if (loading) return <Card><p className="text-sm text-secondary-500">Loading Circular 88 reference...</p></Card>;
  if (error) return <Card><p role="alert" className="text-sm text-error-700 dark:text-error-300">{error}</p></Card>;
  return (
    <div className="space-y-4">
      <Card>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div><h3 className="font-semibold text-secondary-900 dark:text-white">Circular 88 reporting context</h3><p className="text-sm text-secondary-600 dark:text-secondary-300">Official Treasury reference data is view only here; capture remains in the dedicated Circular 88 workspace.</p></div>
          <Badge variant="success">{configuration?.financialYearCode} · {configuration?.catalogueVersionCode}</Badge>
        </div>
      </Card>
      {!references.length ? <Card><EmptyState title="Circular 88: Not Mapped" description="Circular 88 is enabled for this financial year, but this KPI has no active mapping. Core OPMS reporting remains available." /></Card> : references.map(reference => (
        <Card key={reference.mapping.publicId}>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div><h3 className="font-semibold text-secondary-900 dark:text-white">{reference.indicator?.code ?? 'Circular 88 indicator'} · {reference.indicator?.name ?? 'Mapped indicator'}</h3><p className="text-xs text-secondary-500">{reference.mapping.mappingType} mapping</p></div>
            <Badge variant={reference.indicator?.isActive === false ? 'warning' : 'info'}>{reference.indicator?.isActive === false ? 'Inactive definition' : 'Applicable reference'}</Badge>
          </div>
          <div className="mt-4 grid gap-4 md:grid-cols-2">
            <Value label="Official definition" value={reference.indicator?.definition} />
            <Value label="Official technical indicator description" value={reference.indicator?.officialTechnicalIndicatorDescription} />
            <Value label="Value type" value={reference.indicator?.valueType} />
            <Value label="Calculation" value={reference.indicator?.officialFormulaText ?? reference.indicator?.calculationOperator} />
            <Value label="Data elements" value={reference.indicator?.dataElements.map(item => `${item.code}: ${item.name}`).join('\n')} />
            <Value label="Applicability / readiness records" value={reference.indicator?.applicability.length ?? 0} />
            <Value label="Baseline plan" value={reference.plan?.baselineValue} />
            <Value label="Annual plan" value={reference.plan?.annualTarget} />
            <Value label="Assigned responsibility" value={reference.assignments.map(item => `${item.role}: ${item.employeeName}`).join('\n')} />
          </div>
        </Card>
      ))}
    </div>
  );
}
