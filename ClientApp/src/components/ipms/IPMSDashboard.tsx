import { useEffect, useMemo, useState } from 'react';
import { BarChart3, ClipboardList, FileText, Layers } from 'lucide-react';
import { Card, Button } from '../ui';
import { getIpmsTargets, getIpmsSubmissions } from '../../api/api';
import { AppShell } from '../layout/AppShell';
import { useApp } from '../../context/AppContext';
import type { IPMSTarget, IPMSSubmission } from '../../types';

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
  const [targets, setTargets] = useState<IPMSTarget[]>([]);
  const [submissions, setSubmissions] = useState<IPMSSubmission[]>([]);

  useEffect(() => {
    const load = async () => {
      const [targetsResult, submissionsResult] = await Promise.all([getIpmsTargets(), getIpmsSubmissions()]);
      setTargets(targetsResult.data ?? []);
      setSubmissions(submissionsResult.data ?? []);
    };
    void load();
  }, []);

  const stats = useMemo(() => {
    const assigned = targets.length;
    const achieved = targets.filter(t => t.submissions.some(s => s.status === 'approved')).length;
    const atRisk = targets.filter(t => t.submissions.some(s => s.status === 'returned_for_info' || s.status === 'rejected' || s.status === 'verify_rejected')).length;
    const outstanding = targets.filter(t => t.submissions.every(s => s.status === 'draft' || s.status === 'submitted')).length;
    const draft = submissions.filter(s => s.status === 'draft').length;
    const submitted = submissions.filter(s => s.status === 'submitted').length;
    const returned = submissions.filter(s => s.status === 'returned_for_info' || s.status === 'rejected' || s.status === 'verify_rejected').length;
    const approved = submissions.filter(s => s.status === 'approved').length;
    return { assigned, achieved, atRisk, outstanding, draft, submitted, returned, approved };
  }, [targets, submissions]);

  const quickLinks = [
    { label: 'KPIs', path: '/ipms/targets' },
    { label: 'Submissions', path: '/ipms/submissions' },
    { label: 'Reports', path: '/reports/ipms-performance' },
  ];

  return (
    <AppShell title="IPMS Dashboard" subtitle="Individual performance overview with KPI, submission and review insights">
      <div className="space-y-6">
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {tile('Assigned KPIs', String(stats.assigned), () => setCurrentPath('/ipms/targets'))}
          {tile('Achieved KPIs', String(stats.achieved), () => setCurrentPath('/ipms/targets'))}
          {tile('At Risk KPIs', String(stats.atRisk), () => setCurrentPath('/ipms/targets'))}
          {tile('Outstanding KPIs', String(stats.outstanding), () => setCurrentPath('/ipms/targets'))}
        </div>

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {tile('Draft', String(stats.draft), () => setCurrentPath('/ipms/submissions'))}
          {tile('Submitted', String(stats.submitted), () => setCurrentPath('/ipms/submissions'))}
          {tile('Returned', String(stats.returned), () => setCurrentPath('/workflow/returned-submissions'))}
          {tile('Approved', String(stats.approved), () => setCurrentPath('/workflow/approved-closed'))}
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
            <div className="mt-4 text-sm text-secondary-700">{submissions.filter(s => s.status === 'submitted').length} pending reviews.</div>
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
