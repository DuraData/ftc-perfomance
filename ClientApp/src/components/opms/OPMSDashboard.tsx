import { useEffect, useState } from 'react';
import { ClipboardList, FileText, Layers, LineChart, Map, Shield } from 'lucide-react';
import { getOpmsPerformanceDashboard } from '../../api/api';
import { AppShell } from '../layout/AppShell';
import { Button, Card } from '../ui';
import { useApp } from '../../context/AppContext';
import type { PerformanceDashboardDto } from '../../types';

const emptyDashboard: PerformanceDashboardDto = {
  totalTargets: 0,
  activeTargets: 0,
  completedTargets: 0,
  overdueTargets: 0,
  atRiskTargets: 0,
  outstandingTargets: 0,
  draftSubmissions: 0,
  submittedSubmissions: 0,
  returnedSubmissions: 0,
  approvedSubmissions: 0,
  pendingVerification: 0,
  pendingApproval: 0,
};

function tile(label: string, value: string, onClick: () => void) {
  return (
    <Button key={label} variant="outline" className="flex-1 flex-col gap-1 rounded-xl border-secondary-200 p-4 text-left hover:border-primary-500" onClick={onClick}>
      <p className="text-xs uppercase tracking-wide text-secondary-500">{label}</p>
      <p className="text-2xl font-semibold text-secondary-900">{value}</p>
    </Button>
  );
}

export function OPMSDashboardPage() {
  const { setCurrentPath, userProfile } = useApp();
  const [totals, setTotals] = useState<PerformanceDashboardDto>(emptyDashboard);

  useEffect(() => {
    const load = async () => {
      const result = await getOpmsPerformanceDashboard();
      setTotals(result.data ?? emptyDashboard);
    };
    void load();
  }, []);

  const quickLinks = [
    { label: 'Targets', path: '/opms/targets' },
    { label: 'Submissions', path: '/opms/submissions' },
    { label: 'Reports', path: '/reports/opms-performance' },
  ];

  return (
    <AppShell title="OPMS Dashboard" subtitle="Operational performance overview for targets, submissions, and workflow queues">
      <div className="space-y-6">
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {tile('Total Targets', String(totals.totalTargets), () => setCurrentPath('/opms/targets'))}
          {tile('Active Targets', String(totals.activeTargets), () => setCurrentPath('/opms/targets'))}
          {tile('Completed Targets', String(totals.completedTargets), () => setCurrentPath('/opms/targets'))}
          {tile('Overdue Targets', String(totals.overdueTargets), () => setCurrentPath('/opms/targets'))}
        </div>

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {tile('Draft Submissions', String(totals.draftSubmissions), () => setCurrentPath('/opms/submissions'))}
          {tile('Submitted', String(totals.submittedSubmissions), () => setCurrentPath('/opms/submissions'))}
          {tile('Returned', String(totals.returnedSubmissions), () => setCurrentPath('/workflow/returned-submissions'))}
          {tile('Approved', String(totals.approvedSubmissions), () => setCurrentPath('/workflow/approved-closed'))}
        </div>

        <div className="grid gap-4 lg:grid-cols-3">
          <Card>
            <div className="flex items-center gap-2">
              <Layers className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900">Department Performance</h3>
            </div>
            <div className="mt-4 space-y-2 text-sm text-secondary-700">
              <p>{userProfile?.department ?? 'All departments'} top performers and department variance are visible in the performance grid.</p>
            </div>
          </Card>

          <Card>
            <div className="flex items-center gap-2">
              <FileText className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900">Verification Queue</h3>
            </div>
            <div className="mt-4 space-y-2 text-sm text-secondary-700">
              <p>{totals.pendingVerification} items awaiting verification.</p>
            </div>
          </Card>

          <Card>
            <div className="flex items-center gap-2">
              <ClipboardList className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900">Approval Queue</h3>
            </div>
            <div className="mt-4 space-y-2 text-sm text-secondary-700">
              <p>{totals.pendingApproval} items ready for approval review.</p>
            </div>
          </Card>
        </div>

        <div className="grid gap-4 xl:grid-cols-3">
          <Card className="xl:col-span-2">
            <div className="flex items-center gap-2">
              <LineChart className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900">Quarterly Performance</h3>
            </div>
            <div className="mt-4 grid gap-3 sm:grid-cols-2">
              {['Q1', 'Q2', 'Mid-Term', 'Q3', 'Q4', 'Annual'].map(period => (
                <div key={period} className="rounded-lg border border-secondary-200 p-3 text-sm text-secondary-700 dark:border-secondary-700">
                  <p className="font-semibold text-secondary-900">{period}</p>
                  <p className="mt-1">Performance summary for {period}.</p>
                </div>
              ))}
            </div>
          </Card>

          <Card>
            <div className="flex items-center gap-2">
              <Shield className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900">Risk Indicators</h3>
            </div>
            <div className="mt-4 space-y-2 text-sm text-secondary-700">
              <p>Linked risk exposure for OPMS targets is surfaced by target risk and workflow health.</p>
            </div>
          </Card>
        </div>

        <Card>
          <div className="flex items-center gap-2">
            <Map className="h-5 w-5 text-primary-600" />
            <h3 className="text-base font-semibold text-secondary-900">Quick Links</h3>
          </div>
          <div className="mt-4 grid gap-3 sm:grid-cols-3">
            {quickLinks.map(link => (
              <Button key={link.path} variant="outline" onClick={() => setCurrentPath(link.path)}>
                {link.label}
              </Button>
            ))}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}
