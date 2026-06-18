import { useState } from 'react';
import { FileText, Download, BarChart3, TrendingUp, CheckCircle, Clock } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { Modal } from '../common/Modal';
import { Select } from '../common/Form';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

const reportCategories = [
  { id: 'performance', title: 'Performance', icon: <TrendingUp className="w-5 h-5" />, reports: [
    { id: 'kpi-summary', title: 'KPI Summary', description: 'All KPI performance across departments' },
    { id: 'dept-performance', title: 'Department', description: 'Performance analysis by department' },
    { id: 'opms-ipms-alignment', title: 'OPMS/IPMS Alignment', description: 'Target vs achievement comparison' },
  ]},
  { id: 'submission', title: 'Submissions', icon: <FileText className="w-5 h-5" />, reports: [
    { id: 'quarterly-status', title: 'Quarterly Status', description: 'Submission status by quarter' },
    { id: 'overdue', title: 'Overdue', description: 'Overdue submissions requiring action' },
  ]},
  { id: 'workflow', title: 'Workflow', icon: <Clock className="w-5 h-5" />, reports: [
    { id: 'turnaround', title: 'Turnaround', description: 'Approval processing times' },
    { id: 'variance', title: 'Variance Analysis', description: 'Target variance details' },
  ]},
  { id: 'audit', title: 'Audit', icon: <CheckCircle className="w-5 h-5" />, reports: [
    { id: 'audit-findings', title: 'Audit Findings', description: 'Findings and recommendations' },
    { id: 'annual-report', title: 'Annual Report', description: 'Comprehensive annual review' },
  ]},
];

const sampleChartData = [
  { department: 'Infra', target: 85, actual: 82 },
  { department: 'Community', target: 90, actual: 91 },
  { department: 'Finance', target: 95, actual: 88 },
  { department: 'Corporate', target: 88, actual: 86 },
];

function ReportPreviewModal({ isOpen, onClose, report }: { isOpen: boolean; onClose: () => void; report: { id: string; title: string; description: string } | null }) {
  if (!report) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={report.title} size="lg">
      <div className="space-y-3">
        {/* Filters */}
        <div className="flex items-center gap-2 flex-wrap">
          <Select label="" options={[{ value: 'q1', label: 'Q1' }, { value: 'q2', label: 'Q2' }, { value: 'q3', label: 'Q3' }, { value: 'q4', label: 'Q4' }]} defaultValue="q1" />
          <Select label="" options={[{ value: 'all', label: 'All Depts' }, { value: 'infra', label: 'Infrastructure' }]} defaultValue="all" />
        </div>

        {/* Content */}
        {report.id === 'kpi-summary' && (
          <>
            <div className="grid grid-cols-4 gap-2">
              {[{ label: 'Targets', value: 48 }, { label: 'On Track', value: 39 }, { label: 'At Risk', value: 6 }, { label: 'Behind', value: 3 }].map((s, i) => (
                <Card key={i} className="p-2 text-center">
                  <p className="text-lg font-bold text-secondary-900 dark:text-white">{s.value}</p>
                  <p className="text-[10px] text-secondary-500">{s.label}</p>
                </Card>
              ))}
            </div>
            <Card className="p-3">
              <p className="text-xs font-medium mb-2">Department Performance</p>
              <div className="h-40">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={sampleChartData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="department" tick={{ fontSize: 10 }} />
                    <YAxis tick={{ fontSize: 10 }} />
                    <Tooltip />
                    <Bar dataKey="target" fill="#94a3b8" radius={[2, 2, 0, 0]} />
                    <Bar dataKey="actual" fill="#3b82f6" radius={[2, 2, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </Card>
          </>
        )}

        {report.id === 'overdue' && (
          <div className="space-y-2">
            {[1, 2, 3].map(i => (
              <Card key={i} className="border-l-2 border-error-500 p-3">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium">Roads Maintenance - Q{i}</p>
                    <p className="text-[10px] text-secondary-500">Infrastructure Services • Due: 2025-06-{15 - i * 5}</p>
                  </div>
                  <Badge variant="error" size="sm">{i * 2}d overdue</Badge>
                </div>
              </Card>
            ))}
          </div>
        )}

        {report.id !== 'kpi-summary' && report.id !== 'overdue' && (
          <div className="text-center py-8">
            <BarChart3 className="w-8 h-8 mx-auto text-secondary-400 mb-2" />
            <p className="text-xs text-secondary-500">Report preview</p>
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center justify-between pt-3 border-t text-[10px] text-secondary-500">
          <span>Generated: {new Date().toLocaleDateString()}</span>
          <div className="flex gap-1">
            <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>PDF</Button>
            <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Excel</Button>
          </div>
        </div>
      </div>
    </Modal>
  );
}

export function Reports() {
  const [selectedReport, setSelectedReport] = useState<{ id: string; title: string; description: string } | null>(null);

  return (
    <AppShell title="Reports" subtitle="Performance analytics">
      <div className="space-y-4">
        {reportCategories.map(category => (
          <div key={category.id}>
            <div className="flex items-center gap-2 mb-2">
              <div className="p-1.5 bg-primary-50 dark:bg-primary-900/30 rounded text-primary-600">{category.icon}</div>
              <h2 className="text-sm font-semibold text-secondary-900 dark:text-white">{category.title}</h2>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
              {category.reports.map(report => (
                <button key={report.id} type="button" className="text-left" onClick={() => setSelectedReport(report)}>
                  <Card className="hover:shadow cursor-pointer p-3">
                    <div className="flex items-start justify-between mb-1.5">
                      <div className="p-1.5 bg-secondary-50 rounded">
                        <FileText className="w-4 h-4 text-secondary-400" />
                      </div>
                    </div>
                    <h3 className="text-sm font-medium text-secondary-900 dark:text-white">{report.title}</h3>
                    <p className="text-xs text-secondary-500 mt-0.5">{report.description}</p>
                  </Card>
                </button>
              ))}
            </div>
          </div>
        ))}
        <ReportPreviewModal isOpen={!!selectedReport} onClose={() => setSelectedReport(null)} report={selectedReport} />
      </div>
    </AppShell>
  );
}
