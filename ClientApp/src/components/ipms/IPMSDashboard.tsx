import { useEffect, useState } from 'react';
import { BarChart3, ClipboardList, FileText, Layers } from 'lucide-react';
import { Card, Button } from '../ui';
import { getIpmsPerformanceDashboard } from '../../api/api';
import { AppShell } from '../layout/AppShell';
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

export function IPMSDashboardPage() {
  const { setCurrentPath, userProfile } = useApp();
  const [stats, setStats] = useState<PerformanceDashboardDto>(emptyDashboard);

  useEffect(() => {
    const load = async () => {
      const result = await getIpmsPerformanceDashboard();
      setStats(result.data ?? emptyDashboard);
    };
    void load();
  }, []);

  const quickLinks = [
    { label: 'KPIs', path: '/ipms/targets' },
    { label: 'Submissions', path: '/ipms/submissions' },
    { label: 'Reports', path: '/reports/ipms-performance' },
  ];

  return (
    <AppShell title="IPMS Dashboard" subtitle="Individual performance overview with KPI, submission and review insights">
      <div className="space-y-6">
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {tile('Assigned KPIs', String(stats.totalTargets), () => setCurrentPath('/ipms/targets'))}
          {tile('Achieved KPIs', String(stats.completedTargets), () => setCurrentPath('/ipms/targets'))}
          {tile('At Risk KPIs', String(stats.atRiskTargets), () => setCurrentPath('/ipms/targets'))}
          {tile('Outstanding KPIs', String(stats.outstandingTargets), () => setCurrentPath('/ipms/targets'))}
        </div>

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {tile('Draft', String(stats.draftSubmissions), () => setCurrentPath('/ipms/submissions'))}
          {tile('Submitted', String(stats.submittedSubmissions), () => setCurrentPath('/ipms/submissions'))}
          {tile('Returned', String(stats.returnedSubmissions), () => setCurrentPath('/workflow/returned-submissions'))}
          {tile('Approved', String(stats.approvedSubmissions), () => setCurrentPath('/workflow/approved-closed'))}
        </div>

        <div className="grid gap-4 lg:grid-cols-3">
          <Card>
            <div className="flex items-center gap-2">
              <BarChart3 className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900">Performance Ratings</h3>
            </div>
            <div className="mt-4 grid gap-2 text-sm text-secondary-700">
              <p>Rating distribution is derived from target scores and reviewer comments.</p>
            </div>
          </Card>

          <Card>
            <div className="flex items-center gap-2">
              <Layers className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900">Team Performance</h3>
            </div>
            <div className="mt-4 text-sm text-secondary-700">
              {userProfile?.department ? `${userProfile.department} performance summary available.` : 'Team performance visible to administrators.'}
            </div>
          </Card>

          <Card>
            <div className="flex items-center gap-2">
              <ClipboardList className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900">Review Queue</h3>
            </div>
            <div className="mt-4 text-sm text-secondary-700">{stats.pendingVerification} pending reviews.</div>
          </Card>
        </div>

        <Card>
          <div className="flex items-center gap-2">
            <FileText className="h-5 w-5 text-primary-600" />
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
