import { useEffect, useMemo, useState } from 'react';
import { BarChart3, FileText, Layers, Map, Plus, RefreshCcw, Users } from 'lucide-react';
import {
  createIdpComment,
  createIdpPlan,
  createIdpPlanVersion,
  getIdpAlignmentMatrix,
  getIdpDashboard,
  getIdpPlans,
  getIdpPlanHierarchy,
  getIdpReport,
  createIdpCommunitySession,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card, EmptyState } from '../ui';
import type {
  IdpAlignmentMatrixItem,
  IdpDashboard,
  IdpHierarchy,
  IdpPlanSummary,
  IdpPlanVersion,
  IdpReportDocument,
} from '../../types';

function metricCard(title: string, value: string | number, caption?: string) {
  return (
    <Card key={title}>
      <p className="text-xs uppercase tracking-wide text-secondary-500 dark:text-secondary-400">{title}</p>
      <p className="mt-2 text-2xl font-bold text-secondary-900 dark:text-white">{value}</p>
      {caption ? <p className="mt-1 text-xs text-secondary-500 dark:text-secondary-400">{caption}</p> : null}
    </Card>
  );
}

export function IdpPlanningDashboardPage() {
  const { pushToast } = useApp();
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [dashboard, setDashboard] = useState<IdpDashboard | null>(null);
  const [busy, setBusy] = useState(false);

  const load = async () => {
    setBusy(true);
    try {
      const plansResult = await getIdpPlans();
      const loadedPlans = plansResult.data ?? [];
      setPlans(loadedPlans);
      const planId = selectedPlanId ?? loadedPlans[0]?.id ?? null;
      setSelectedPlanId(planId);

      if (planId) {
        const dashboardResult = await getIdpDashboard(planId);
        setDashboard(dashboardResult.data ?? null);
      } else {
        setDashboard(null);
      }
    } finally {
      setBusy(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <AppShell title="IDP Dashboard" subtitle="Executive strategic planning and implementation view">
      <div className="space-y-5">
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="outline" icon={<RefreshCcw className="h-4 w-4" />} onClick={() => void load()}>
            Refresh
          </Button>
          <Button
            variant="primary"
            icon={<Plus className="h-4 w-4" />}
            onClick={async () => {
              const year = new Date().getFullYear();
              const result = await createIdpPlan({
                municipalityName: 'Blue Hills Municipality',
                planTitle: `Integrated Development Plan ${year}-${year + 5}`,
                planCode: `IDP-${year}`,
                startFinancialYear: year,
                endFinancialYear: year + 5,
              });

              if (result.success) {
                pushToast('success', 'IDP plan created successfully.');
                await load();
              } else {
                pushToast('error', result.message ?? 'Failed to create IDP plan.');
              }
            }}
          >
            New IDP Plan
          </Button>
          <select
            value={selectedPlanId ?? ''}
            onChange={(event) => {
              const value = Number(event.target.value);
              setSelectedPlanId(Number.isNaN(value) ? null : value);
            }}
            className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
          >
            {plans.map(plan => (
              <option key={plan.id} value={plan.id}>{plan.planCode} - {plan.planTitle}</option>
            ))}
          </select>
          <Button
            variant="outline"
            onClick={async () => {
              if (!selectedPlanId) return;
              const year = new Date().getFullYear();
              const result = await createIdpPlanVersion(selectedPlanId, {
                versionType: 'AnnualReview',
                versionLabel: `Annual Review ${year}`,
                reviewYear: `${year}/${year + 1}`,
                summaryOfChanges: 'Annual review generated from planning dashboard',
              });

              if (result.success) {
                pushToast('success', 'Annual review version created.');
              } else {
                pushToast('error', result.message ?? 'Unable to create annual review version.');
              }
            }}
          >
            Annual Review
          </Button>
        </div>

        {busy ? <Card><p className="text-sm text-secondary-500">Loading IDP dashboard...</p></Card> : null}

        {!busy && !dashboard ? (
          <EmptyState
            icon={<Map className="h-6 w-6" />}
            title="No IDP dashboard data"
            description="Create an IDP plan to unlock strategic progress, participation, and budget analytics."
          />
        ) : null}

        {dashboard ? (
          <>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              {metricCard('Strategic Outcomes', dashboard.outcomes)}
              {metricCard('Strategic Objectives', dashboard.objectives)}
              {metricCard('Projects', dashboard.projects)}
              {metricCard('KPIs', dashboard.kpis)}
              {metricCard('Community Sessions', dashboard.communitySessions)}
              {metricCard('Risk Items', dashboard.risks)}
              {metricCard('KPI Achievement', `${dashboard.kpiAchievementRate.toFixed(2)}%`)}
              {metricCard('Budget Utilization', `${dashboard.approvedBudget > 0 ? ((dashboard.actualExpenditure / dashboard.approvedBudget) * 100).toFixed(2) : '0.00'}%`)}
            </div>

            <div className="grid gap-4 lg:grid-cols-3">
              <Card>
                <div className="flex items-center gap-2">
                  <BarChart3 className="h-5 w-5 text-primary-600" />
                  <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Budget Integration</h3>
                </div>
                <div className="mt-4 space-y-2 text-sm text-secondary-700 dark:text-secondary-300">
                  <p>Planned Budget: <span className="font-semibold">R {dashboard.plannedBudget.toLocaleString()}</span></p>
                  <p>Approved Budget: <span className="font-semibold">R {dashboard.approvedBudget.toLocaleString()}</span></p>
                  <p>Actual Expenditure: <span className="font-semibold">R {dashboard.actualExpenditure.toLocaleString()}</span></p>
                </div>
              </Card>

              <Card>
                <div className="flex items-center gap-2">
                  <Layers className="h-5 w-5 text-primary-600" />
                  <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Top Strategic Risks</h3>
                </div>
                <div className="mt-3 flex flex-wrap gap-2">
                  {dashboard.topRiskTitles.length ? dashboard.topRiskTitles.map(risk => <Badge key={risk} variant="warning">{risk}</Badge>) : <p className="text-sm text-secondary-500">No risks linked.</p>}
                </div>
              </Card>

              <Card>
                <div className="flex items-center gap-2">
                  <Users className="h-5 w-5 text-primary-600" />
                  <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Ward Participation</h3>
                </div>
                <div className="mt-3 space-y-2">
                  {dashboard.wardParticipation.slice(0, 5).map(ward => (
                    <div key={ward.wardId} className="rounded border border-secondary-200 px-2 py-1 text-sm dark:border-secondary-700">
                      <p className="font-medium text-secondary-900 dark:text-secondary-100">{ward.wardName}</p>
                      <p className="text-xs text-secondary-500">Meetings: {ward.meetingCount} | Participants: {ward.participantsCount} | Needs: {ward.needsCaptured}</p>
                    </div>
                  ))}
                </div>
              </Card>
            </div>
          </>
        ) : null}
      </div>
    </AppShell>
  );
}

export function IdpPlanManagementPage() {
  const { pushToast } = useApp();
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [versions, setVersions] = useState<IdpPlanVersion[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);

  const load = async () => {
    const plansResult = await getIdpPlans();
    const loadedPlans = plansResult.data ?? [];
    setPlans(loadedPlans);
    const planId = selectedPlanId ?? loadedPlans[0]?.id ?? null;
    setSelectedPlanId(planId);

    if (planId) {
      const hierarchyResult = await getIdpPlanHierarchy(planId);
      const hierarchy = hierarchyResult.data;
      setVersions(hierarchy?.versions ?? []);
    } else {
      setVersions([]);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <AppShell title="IDP Plans" subtitle="Five-year plan lifecycle, annual reviews, and version governance">
      <div className="space-y-4">
        <Card>
          <div className="flex flex-wrap items-center gap-2">
            <Button variant="outline" onClick={() => void load()}>Refresh</Button>
            <Button
              variant="primary"
              onClick={async () => {
                const year = new Date().getFullYear();
                const result = await createIdpPlan({
                  municipalityName: 'Blue Hills Municipality',
                  planTitle: `Integrated Development Plan ${year}-${year + 5}`,
                  planCode: `IDP-${year}`,
                  startFinancialYear: year,
                  endFinancialYear: year + 5,
                });
                if (result.success) {
                  pushToast('success', 'IDP plan created.');
                  await load();
                } else {
                  pushToast('error', result.message ?? 'Failed to create plan.');
                }
              }}
            >
              Create Plan
            </Button>
          </div>
        </Card>

        <div className="grid gap-4 lg:grid-cols-2">
          <Card>
            <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Plan Register</h3>
            <div className="mt-3 space-y-2">
              {plans.map(plan => (
                <button
                  key={plan.id}
                  onClick={() => setSelectedPlanId(plan.id)}
                  className={`w-full rounded border px-3 py-2 text-left ${selectedPlanId === plan.id ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20' : 'border-secondary-200 dark:border-secondary-700'}`}
                >
                  <p className="font-medium text-secondary-900 dark:text-secondary-100">{plan.planCode} - {plan.planTitle}</p>
                  <p className="text-xs text-secondary-500">{plan.startFinancialYear}/{plan.startFinancialYear + 1} to {plan.endFinancialYear}/{plan.endFinancialYear + 1} | Status: {plan.status}</p>
                </button>
              ))}
            </div>
          </Card>

          <Card>
            <div className="flex items-center justify-between">
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Version Control</h3>
              <Button
                variant="outline"
                onClick={async () => {
                  if (!selectedPlanId) return;
                  const now = new Date();
                  const result = await createIdpPlanVersion(selectedPlanId, {
                    versionType: 'Revised',
                    versionLabel: `Revised IDP ${now.getFullYear()}`,
                    reviewYear: `${now.getFullYear()}/${now.getFullYear() + 1}`,
                    summaryOfChanges: 'Revision initiated from IDP plan management workspace',
                  });

                  if (result.success) {
                    pushToast('success', 'New revised IDP version created.');
                    await load();
                  } else {
                    pushToast('error', result.message ?? 'Failed to create version.');
                  }
                }}
              >
                Create Revision
              </Button>
            </div>
            <div className="mt-3 space-y-2">
              {versions.map(version => (
                <div key={version.id} className="rounded border border-secondary-200 px-3 py-2 text-sm dark:border-secondary-700">
                  <p className="font-medium text-secondary-900 dark:text-secondary-100">v{version.versionNumber} - {version.versionLabel}</p>
                  <p className="text-xs text-secondary-500">Type: {version.versionType} | Review Year: {version.reviewYear ?? 'N/A'} | Active: {version.isActive ? 'Yes' : 'No'}</p>
                </div>
              ))}
              {!versions.length ? <p className="text-sm text-secondary-500">No versions available.</p> : null}
            </div>
          </Card>
        </div>
      </div>
    </AppShell>
  );
}

export function IdpHierarchyPage() {
  const { pushToast } = useApp();
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [hierarchy, setHierarchy] = useState<IdpHierarchy | null>(null);

  const load = async () => {
    const plansResult = await getIdpPlans();
    const loadedPlans = plansResult.data ?? [];
    setPlans(loadedPlans);
    const planId = selectedPlanId ?? loadedPlans[0]?.id ?? null;
    setSelectedPlanId(planId);

    if (planId) {
      const hierarchyResult = await getIdpPlanHierarchy(planId);
      setHierarchy(hierarchyResult.data ?? null);
    } else {
      setHierarchy(null);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const pathRows = useMemo(() => {
    if (!hierarchy) return [];

    return hierarchy.outcomes.flatMap(outcome => {
      const objectives = hierarchy.objectives.filter(objective => objective.idpStrategicOutcomeId === outcome.id);
      return objectives.flatMap(objective => {
        const priorities = hierarchy.priorities.filter(priority => priority.idpStrategicObjectiveId === objective.id);
        return priorities.flatMap(priority => {
          const programmes = hierarchy.programmes.filter(programme => programme.idpDevelopmentPriorityId === priority.id);
          return programmes.flatMap(programme => {
            const projects = hierarchy.projects.filter(project => project.idpProgrammeId === programme.id);
            return projects.flatMap(project => {
              const kpis = hierarchy.kpis.filter(kpi => kpi.idpProjectId === project.id);
              return kpis.map(kpi => ({ outcome, objective, priority, programme, project, kpi }));
            });
          });
        });
      });
    });
  }, [hierarchy]);

  return (
    <AppShell title="Planning Hierarchy" subtitle="Outcome to annual target traceability across the full IDP chain">
      <div className="space-y-4">
        <Card>
          <div className="flex flex-wrap items-center gap-2">
            <Button variant="outline" onClick={() => void load()}>Refresh</Button>
            <select
              value={selectedPlanId ?? ''}
              onChange={event => setSelectedPlanId(Number(event.target.value))}
              className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
            >
              {plans.map(plan => <option key={plan.id} value={plan.id}>{plan.planCode}</option>)}
            </select>
            <Button
              variant="outline"
              onClick={async () => {
                if (!selectedPlanId) return;
                const result = await createIdpComment({
                  idpPlanId: selectedPlanId,
                  idpPlanVersionId: hierarchy?.versions.find(v => v.isActive)?.id ?? null,
                  entityName: 'IdpHierarchy',
                  entityId: selectedPlanId.toString(),
                  comment: 'Hierarchy review checkpoint captured from planning workspace',
                });

                pushToast(result.success ? 'success' : 'error', result.success ? 'Hierarchy review comment added.' : (result.message ?? 'Failed to add comment.'));
              }}
            >
              Add Review Comment
            </Button>
          </div>
        </Card>

        <Card>
          <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Hierarchy Drill-Down</h3>
          <div className="mt-3 overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="border-b border-secondary-200 text-left text-xs uppercase text-secondary-500 dark:border-secondary-700">
                <tr>
                  <th className="px-2 py-2">Outcome</th>
                  <th className="px-2 py-2">Objective</th>
                  <th className="px-2 py-2">Priority</th>
                  <th className="px-2 py-2">Programme</th>
                  <th className="px-2 py-2">Project</th>
                  <th className="px-2 py-2">KPI</th>
                </tr>
              </thead>
              <tbody>
                {pathRows.map(row => (
                  <tr key={`${row.kpi.id}`} className="border-b border-secondary-100 dark:border-secondary-800">
                    <td className="px-2 py-2">{row.outcome.code} - {row.outcome.name}</td>
                    <td className="px-2 py-2">{row.objective.code} - {row.objective.name}</td>
                    <td className="px-2 py-2">{row.priority.name}</td>
                    <td className="px-2 py-2">{row.programme.programmeCode}</td>
                    <td className="px-2 py-2">{row.project.projectCode}</td>
                    <td className="px-2 py-2">{row.kpi.kpiCode}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {!pathRows.length ? <p className="p-3 text-sm text-secondary-500">No hierarchy records available for this plan.</p> : null}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}

export function IdpCommunityParticipationPage() {
  const { pushToast } = useApp();
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [dashboard, setDashboard] = useState<IdpDashboard | null>(null);

  const load = async () => {
    const plansResult = await getIdpPlans();
    const loadedPlans = plansResult.data ?? [];
    setPlans(loadedPlans);
    const planId = selectedPlanId ?? loadedPlans[0]?.id ?? null;
    setSelectedPlanId(planId);

    if (planId) {
      const dashboardResult = await getIdpDashboard(planId);
      setDashboard(dashboardResult.data ?? null);
    } else {
      setDashboard(null);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <AppShell title="Community Participation" subtitle="Ward consultations, public meetings, and stakeholder inputs">
      <div className="space-y-4">
        <Card>
          <div className="flex flex-wrap items-center gap-2">
            <Button variant="outline" onClick={() => void load()}>Refresh</Button>
            <select
              value={selectedPlanId ?? ''}
              onChange={async event => {
                const planId = Number(event.target.value);
                setSelectedPlanId(planId);

                const dashboardResult = await getIdpDashboard(planId);
                setDashboard(dashboardResult.data ?? null);
              }}
              className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
            >
              {plans.map(plan => <option key={plan.id} value={plan.id}>{plan.planCode}</option>)}
            </select>
            <Button
              variant="primary"
              onClick={async () => {
                if (!selectedPlanId) {
                  pushToast('error', 'Select an IDP plan before logging participation.');
                  return;
                }

                const now = new Date();
                const result = await createIdpCommunitySession({
                  idpPlanId: selectedPlanId,
                  participationType: 'PublicMeeting',
                  sessionDate: now.toISOString(),
                  venue: 'Municipal Hall',
                  wardId: null,
                  participantsCount: 120,
                  attendanceRegisterPath: '/documents/idp/public-meeting-attendance.pdf',
                  minutesPath: '/documents/idp/public-meeting-minutes.pdf',
                });

                if (result.success) {
                  pushToast('success', 'Community participation session logged.');
                  await load();
                } else {
                  pushToast('error', result.message ?? 'Failed to log participation session.');
                }
              }}
            >
              Log Public Meeting
            </Button>
          </div>
        </Card>

        <Card>
          <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Ward Participation Overview</h3>
          <div className="mt-3 grid gap-3 md:grid-cols-2">
            {(dashboard?.wardParticipation ?? []).map(item => (
              <div key={item.wardId} className="rounded border border-secondary-200 p-3 dark:border-secondary-700">
                <p className="font-medium text-secondary-900 dark:text-secondary-100">{item.wardName}</p>
                <p className="text-xs text-secondary-500">Meetings: {item.meetingCount} | Participants: {item.participantsCount} | Needs captured: {item.needsCaptured}</p>
              </div>
            ))}
            {!(dashboard?.wardParticipation.length) ? <p className="text-sm text-secondary-500">No participation records available.</p> : null}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}

export function IdpAlignmentMatrixPage() {
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [matrix, setMatrix] = useState<IdpAlignmentMatrixItem[]>([]);

  const load = async () => {
    const plansResult = await getIdpPlans();
    const loadedPlans = plansResult.data ?? [];
    setPlans(loadedPlans);
    const planId = selectedPlanId ?? loadedPlans[0]?.id ?? null;
    setSelectedPlanId(planId);

    if (planId) {
      const matrixResult = await getIdpAlignmentMatrix(planId);
      setMatrix(matrixResult.data ?? []);
    } else {
      setMatrix([]);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <AppShell title="Alignment Matrix" subtitle="NDP, PGDS, DDM, sector, and municipal alignment mapping">
      <div className="space-y-4">
        <Card>
          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={() => void load()}>Refresh</Button>
            <select
              value={selectedPlanId ?? ''}
              onChange={event => setSelectedPlanId(Number(event.target.value))}
              className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
            >
              {plans.map(plan => <option key={plan.id} value={plan.id}>{plan.planCode}</option>)}
            </select>
          </div>
        </Card>

        <Card>
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="border-b border-secondary-200 text-left text-xs uppercase text-secondary-500 dark:border-secondary-700">
                <tr>
                  <th className="px-2 py-2">Strategic Outcome</th>
                  <th className="px-2 py-2">Objective</th>
                  <th className="px-2 py-2">Framework</th>
                  <th className="px-2 py-2">Reference</th>
                </tr>
              </thead>
              <tbody>
                {matrix.map(item => (
                  <tr key={`${item.objectiveCode}-${item.frameworkReferenceCode}`} className="border-b border-secondary-100 dark:border-secondary-800">
                    <td className="px-2 py-2">{item.strategicOutcomeCode} - {item.strategicOutcomeName}</td>
                    <td className="px-2 py-2">{item.objectiveCode} - {item.objectiveName}</td>
                    <td className="px-2 py-2">{item.frameworkType}</td>
                    <td className="px-2 py-2">{item.frameworkReferenceCode} - {item.frameworkReferenceTitle}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {!matrix.length ? <p className="p-3 text-sm text-secondary-500">No alignment links available for this plan.</p> : null}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}

export function IdpReportsPage() {
  const { pushToast } = useApp();
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [lastReport, setLastReport] = useState<IdpReportDocument | null>(null);

  const loadPlans = async () => {
    const plansResult = await getIdpPlans();
    const loadedPlans = plansResult.data ?? [];
    setPlans(loadedPlans);
    setSelectedPlanId(current => current ?? loadedPlans[0]?.id ?? null);
  };

  useEffect(() => {
    void loadPlans();
  }, []);

  const generate = async (reportType: string, format: 'pdf' | 'excel' | 'word') => {
    if (!selectedPlanId) {
      pushToast('error', 'Select an IDP plan first.');
      return;
    }

    const result = await getIdpReport(selectedPlanId, reportType, format);
    if (result.success && result.data) {
      setLastReport(result.data);
      pushToast('success', `${reportType} report generated (${format.toUpperCase()}).`);
      return;
    }

    pushToast('error', result.message ?? 'Failed to generate report.');
  };

  return (
    <AppShell title="IDP Reporting Centre" subtitle="Generate annual, five-year, strategic, ward, provincial and national submissions">
      <div className="space-y-4">
        <Card>
          <div className="flex flex-wrap items-center gap-2">
            <select
              value={selectedPlanId ?? ''}
              onChange={event => setSelectedPlanId(Number(event.target.value))}
              className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
            >
              {plans.map(plan => <option key={plan.id} value={plan.id}>{plan.planCode} - {plan.planTitle}</option>)}
            </select>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate('annual', 'pdf')}>Annual PDF</Button>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate('five-year', 'word')}>Five-Year Word</Button>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate('ward-based', 'excel')}>Ward Excel</Button>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate('provincial-submission', 'pdf')}>Provincial PDF</Button>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate('national-submission', 'pdf')}>National PDF</Button>
          </div>
        </Card>

        <Card>
          <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Last Generated Report</h3>
          {lastReport ? (
            <div className="mt-3 space-y-1 text-sm text-secondary-700 dark:text-secondary-300">
              <p><span className="font-medium">Name:</span> {lastReport.reportName}</p>
              <p><span className="font-medium">File:</span> {lastReport.fileName}</p>
              <p><span className="font-medium">Type:</span> {lastReport.contentType}</p>
              <p><span className="font-medium">Payload Size:</span> {lastReport.content.length} bytes</p>
            </div>
          ) : (
            <p className="mt-3 text-sm text-secondary-500">No report generated in this session.</p>
          )}
        </Card>
      </div>
    </AppShell>
  );
}
