import { AppShell } from '../layout/AppShell';
import { Card } from '../ui';

function SectionPage({ title, description }: { title: string; description: string }) {
  return (
    <AppShell title={title} subtitle={description}>
      <Card className="max-w-4xl">
        <div className="space-y-4">
          <p className="text-sm text-secondary-600 dark:text-secondary-400">
            This Risk Management page is part of the integrated risk workspace. It is currently configured as a placeholder for the Risk Management navigation flow.
          </p>
          <div className="rounded-lg border border-secondary-200 bg-secondary-50 p-4 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200">
            Use this page as the basis for implementing the Risk Management dashboard, register, assessments, treatment plans, reviews, heatmap, and reports.
          </div>
        </div>
      </Card>
    </AppShell>
  );
}

export function RiskDashboardPage() {
  return <SectionPage title="Risk Dashboard" description="Track strategic and operational risk performance across IDP and planning hierarchy." />;
}

export function RiskRegisterPage() {
  return <SectionPage title="Risk Register" description="Capture risks and their details for strategic objectives, projects, and KPIs." />;
}

export function RiskAssessmentsPage() {
  return <SectionPage title="Assessments" description="Manage risk assessment reviews and risk scoring across the organization." />;
}

export function RiskTreatmentPlansPage() {
  return <SectionPage title="Treatment Plans" description="Define and monitor risk mitigation actions and ownership for risk treatments." />;
}

export function RiskReviewsPage() {
  return <SectionPage title="Reviews" description="Monitor ongoing risk reviews and validation cycles." />;
}

export function RiskHeatmapPage() {
  return <SectionPage title="Heatmap" description="Visualize risk exposure with heatmaps and severity categories." />;
}

export function RiskReportsPage() {
  return <SectionPage title="Risk Reports" description="Generate risk reports and insights for executive review." />;
}
