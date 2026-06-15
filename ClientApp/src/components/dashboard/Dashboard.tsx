import React from 'react';
import {
  Target,
  TrendingUp,
  Clock,
  AlertTriangle,
  CheckCircle,
  Users,
  FileText,
  ArrowUp,
  ArrowDown,
} from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  LineChart,
  Line,
  Legend,
  Area,
  AreaChart,
} from 'recharts';
import { AppShell } from '../layout/AppShell';
import { Card, Badge, ProgressBar } from '../ui';
import { useApp } from '../../context/AppContext';
import { mockDashboardStats, mockDepartmentPerformances, mockOPMSTargets, mockOPMSSubmissions } from '../../data/mockData';

const COLORS = ['#3b82f6', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4'];

const quarterlyData = [
  { quarter: 'Q1', target: 85, actual: 82 },
  { quarter: 'Q2', target: 88, actual: 86 },
  { quarter: 'Mid', target: 90, actual: 87 },
  { quarter: 'Q3', target: 92, actual: 89 },
  { quarter: 'Q4', target: 95, actual: 92 },
];

const statusData = [
  { name: 'On Track', value: 35, color: '#22c55e' },
  { name: 'At Risk', value: 8, color: '#f59e0b' },
  { name: 'Behind', value: 3, color: '#ef4444' },
  { name: 'Completed', value: 39, color: '#3b82f6' },
];

const trendData = [
  { month: 'Jul', score: 78 },
  { month: 'Aug', score: 81 },
  { month: 'Sep', score: 83 },
  { month: 'Oct', score: 85 },
  { month: 'Nov', score: 84 },
  { month: 'Dec', score: 87 },
  { month: 'Jan', score: 86 },
  { month: 'Feb', score: 88 },
  { month: 'Mar', score: 89 },
];

function ActivityFeed() {
  const activities = mockDashboardStats.recentActivity;

  const getActivityIcon = (type: string) => {
    switch (type) {
      case 'approval':
        return <CheckCircle className="w-4 h-4 text-success-500" />;
      case 'submission':
        return <FileText className="w-4 h-4 text-primary-500" />;
      case 'verification':
        return <Users className="w-4 h-4 text-amber-500" />;
      case 'comment':
        return <FileText className="w-4 h-4 text-secondary-500" />;
      case 'target_created':
        return <Target className="w-4 h-4 text-blue-500" />;
      default:
        return <FileText className="w-4 h-4 text-secondary-500" />;
    }
  };

  return (
    <Card>
      <h3 className="text-base font-semibold text-secondary-900 dark:text-white mb-4">
        Recent Activity
      </h3>
      <div className="space-y-4">
        {activities.map(activity => (
          <div key={activity.id} className="flex gap-3">
            <div className="mt-1">{getActivityIcon(activity.type)}</div>
            <div className="flex-1 min-w-0">
              <p className="text-sm text-secondary-700 dark:text-secondary-300">
                {activity.description}
              </p>
              <p className="text-xs text-secondary-500 dark:text-secondary-400 mt-0.5">
                {activity.user.displayName} • {new Date(activity.timestamp).toLocaleString()}
              </p>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}

function DepartmentPerformanceCards() {
  return (
    <div className="space-y-4">
      <h3 className="text-base font-semibold text-secondary-900 dark:text-white">
        Department Performance
      </h3>
      {mockDepartmentPerformances.slice(0, 5).map((dept, index) => (
        <Card key={dept.department.id} className="hover:shadow-md transition-shadow">
          <div className="flex items-center justify-between mb-3">
            <div>
              <h4 className="font-medium text-secondary-900 dark:text-white">
                {dept.department.name}
              </h4>
              <p className="text-sm text-secondary-500 dark:text-secondary-400">
                {dept.completedCount} / {dept.targetCount} targets completed
              </p>
            </div>
            <div className={`text-2xl font-bold ${dept.score >= 90 ? 'text-success-600' : dept.score >= 70 ? 'text-primary-600' : 'text-warning-600'}`}>
              {dept.score.toFixed(1)}%
            </div>
          </div>
          <ProgressBar
            value={dept.completedCount}
            max={dept.targetCount}
            color={dept.score >= 90 ? 'success' : dept.score >= 70 ? 'primary' : 'warning'}
          />
          <div className="flex items-center gap-4 mt-3 text-xs text-secondary-500 dark:text-secondary-400">
            <span className="flex items-center gap-1">
              <Clock className="w-3 h-3" />
              {dept.pendingCount} pending
            </span>
            <span className="flex items-center gap-1">
              <AlertTriangle className="w-3 h-3" />
              {dept.overdueCount} overdue
            </span>
          </div>
        </Card>
      ))}
    </div>
  );
}

function QuickStats() {
  const stats = mockDashboardStats;

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
      <Card className="text-center">
        <Target className="w-8 h-8 text-primary-600 mx-auto mb-2" />
        <p className="text-2xl font-bold text-secondary-900 dark:text-white">{stats.totalTargets}</p>
        <p className="text-xs text-secondary-500 dark:text-secondary-400">Total Targets</p>
      </Card>
      <Card className="text-center">
        <CheckCircle className="w-8 h-8 text-success-600 mx-auto mb-2" />
        <p className="text-2xl font-bold text-secondary-900 dark:text-white">{stats.completedTargets}</p>
        <p className="text-xs text-secondary-500 dark:text-secondary-400">Completed</p>
      </Card>
      <Card className="text-center">
        <Clock className="w-8 h-8 text-warning-600 mx-auto mb-2" />
        <p className="text-2xl font-bold text-secondary-900 dark:text-white">{stats.pendingSubmissions}</p>
        <p className="text-xs text-secondary-500 dark:text-secondary-400">Pending</p>
      </Card>
      <Card className="text-center">
        <AlertTriangle className="w-8 h-8 text-error-600 mx-auto mb-2" />
        <p className="text-2xl font-bold text-secondary-900 dark:text-white">{stats.overdueSubmissions}</p>
        <p className="text-xs text-secondary-500 dark:text-secondary-400">Overdue</p>
      </Card>
    </div>
  );
}

function WorkflowQueues() {
  const { user } = useApp();

  const queues = [
    { label: 'My Submissions', count: 3, color: 'bg-blue-500' },
    { label: 'Pending Verification', count: mockOPMSSubmissions.filter(s => s.status === 'pending_verification').length, color: 'bg-amber-500' },
    { label: 'Pending Approval', count: 8, color: 'bg-orange-500' },
    { label: 'PMS Review', count: 4, color: 'bg-primary-500' },
    { label: 'Auditor Queue', count: 2, color: 'bg-violet-500' },
    { label: 'Returned Items', count: 1, color: 'bg-rose-500' },
  ];

  return (
    <Card>
      <h3 className="text-base font-semibold text-secondary-900 dark:text-white mb-4">
        Work Queues
      </h3>
      <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
        {queues.map(queue => (
          <button
            key={queue.label}
            className="flex items-center gap-3 p-3 rounded-lg border border-secondary-200 dark:border-secondary-700 hover:bg-secondary-50 dark:hover:bg-secondary-800 transition-colors"
          >
            <div className={`w-2 h-8 ${queue.color} rounded-full`} />
            <div className="text-left">
              <p className="text-lg font-semibold text-secondary-900 dark:text-white">{queue.count}</p>
              <p className="text-xs text-secondary-500 dark:text-secondary-400">{queue.label}</p>
            </div>
          </button>
        ))}
      </div>
    </Card>
  );
}

export function Dashboard() {
  const { user } = useApp();

  return (
    <AppShell title="Dashboard" subtitle="Performance Management Overview">
      <div className="space-y-6">
        {/* Welcome section */}
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-xl font-bold text-secondary-900 dark:text-white">
              Welcome back, {user?.firstName}
            </h2>
            <p className="text-secondary-500 dark:text-secondary-400">
              Here's your performance management overview for 2024/2025
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant="success">Q3 Active</Badge>
            <Badge>2024/2025</Badge>
          </div>
        </div>

        {/* Quick stats */}
        <QuickStats />

        {/* Charts row */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Quarterly Progress Chart */}
          <Card>
            <h3 className="text-base font-semibold text-secondary-900 dark:text-white mb-4">
              Quarterly Target vs Actual
            </h3>
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={quarterlyData}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-secondary-200 dark:stroke-secondary-700" />
                  <XAxis dataKey="quarter" className="text-xs" />
                  <YAxis className="text-xs" />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: 'white',
                      border: '1px solid #e5e7eb',
                      borderRadius: '8px',
                    }}
                  />
                  <Legend />
                  <Bar dataKey="target" name="Target %" fill="#94a3b8" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="actual" name="Actual %" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </Card>

          {/* Status Distribution */}
          <Card>
            <h3 className="text-base font-semibold text-secondary-900 dark:text-white mb-4">
              Target Status Distribution
            </h3>
            <div className="h-64 flex items-center justify-center">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={statusData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={80}
                    paddingAngle={5}
                    dataKey="value"
                  >
                    {statusData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </Card>
        </div>

        {/* Trend and Summary */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Performance Trend */}
          <Card className="lg:col-span-2">
            <h3 className="text-base font-semibold text-secondary-900 dark:text-white mb-4">
              Performance Score Trend
            </h3>
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={trendData}>
                  <defs>
                    <linearGradient id="scoreGradient" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.3} />
                      <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-secondary-200 dark:stroke-secondary-700" />
                  <XAxis dataKey="month" className="text-xs" />
                  <YAxis domain={[70, 100]} className="text-xs" />
                  <Tooltip />
                  <Area
                    type="monotone"
                    dataKey="score"
                    name="Score"
                    stroke="#3b82f6"
                    fill="url(#scoreGradient)"
                    strokeWidth={2}
                  />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </Card>

          {/* Average Score Card */}
          <Card className="flex flex-col justify-center items-center text-center bg-gradient-to-br from-primary-600 to-primary-700 text-white">
            <TrendingUp className="w-12 h-12 mb-4 opacity-80" />
            <p className="text-5xl font-bold mb-2">{mockDashboardStats.averageScore.toFixed(1)}%</p>
            <p className="text-primary-100">Average Score</p>
            <div className="flex items-center gap-1 mt-4 text-success-300">
              <ArrowUp className="w-4 h-4" />
              <span className="text-sm">+3.2% vs last year</span>
            </div>
          </Card>
        </div>

        {/* Work Queues */}
        <WorkflowQueues />

        {/* Activity and Department Performance */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <ActivityFeed />
          <div>
            <DepartmentPerformanceCards />
          </div>
        </div>
      </div>
    </AppShell>
  );
}
