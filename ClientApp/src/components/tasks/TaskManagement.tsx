import React, { useState } from 'react';
import {
  Plus,
  Clock,
  CheckCircle,
  AlertTriangle,
  Pause,
  Calendar,
  User,
  Edit2,
  Play,
} from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { Modal } from '../common/Modal';
import { Input, Select, Textarea } from '../common/Form';
import { mockTasks, mockEmployees } from '../../data/mockData';
import type { Task } from '../../types';

const priorityColors = {
  low: 'bg-secondary-100 text-secondary-700',
  medium: 'bg-blue-100 text-blue-700',
  high: 'bg-warning-100 text-warning-700',
  critical: 'bg-error-100 text-error-700',
};

const statusIcons = {
  pending: <Clock className="w-3.5 h-3.5" />,
  in_progress: <Play className="w-3.5 h-3.5 text-blue-500" />,
  completed: <CheckCircle className="w-3.5 h-3.5 text-success-500" />,
  postponed: <Pause className="w-3.5 h-3.5 text-warning-500" />,
  cancelled: <AlertTriangle className="w-3.5 h-3.5 text-error-500" />,
};

function TaskDetailModal({ task, isOpen, onClose }: { task: Task | null; isOpen: boolean; onClose: () => void }) {
  if (!task) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={task.title} size="md">
      <div className="space-y-3">
        <div className="flex items-center gap-2">
          <Badge size="sm" variant={task.priority === 'critical' ? 'error' : task.priority === 'high' ? 'warning' : 'default'}>{task.priority.toUpperCase()}</Badge>
          <div className={`p-1 rounded ${priorityColors[task.priority]}`}>{statusIcons[task.status]}</div>
          <Badge size="sm">{task.status.replace('_', ' ')}</Badge>
        </div>

        <div className="grid grid-cols-2 gap-3 text-xs">
          <div><p className="text-[10px] text-secondary-500">Due</p><p className="font-medium">{new Date(task.dueDate).toLocaleDateString()}</p></div>
          <div><p className="text-[10px] text-secondary-500">Hours</p><p className="font-medium">{task.actualHours ?? 0}/{task.estimatedHours ?? '-'}h</p></div>
        </div>

        {task.description && <div><p className="text-[10px] text-secondary-500">Description</p><p className="text-xs">{task.description}</p></div>}

        <div>
          <p className="text-[10px] text-secondary-500 mb-1">Assigned To</p>
          <div className="flex flex-wrap gap-1">
            {task.assignedTo.map(user => (
              <div key={user.id} className="flex items-center gap-1 bg-secondary-50 dark:bg-secondary-800 rounded px-2 py-1">
                <div className="w-5 h-5 rounded-full bg-primary-100 flex items-center justify-center">
                  <span className="text-[10px] font-medium text-primary-700">{user.firstName[0]}{user.lastName[0]}</span>
                </div>
                <span className="text-xs">{user.displayName}</span>
              </div>
            ))}
          </div>
        </div>

        {task.notes && <div><p className="text-[10px] text-secondary-500">Notes</p><p className="text-xs text-secondary-600">{task.notes}</p></div>}
      </div>

      <div className="flex items-center justify-between pt-3 mt-3 border-t border-secondary-200">
        <div className="flex items-center gap-1">
          {task.status === 'in_progress' && <Button variant="success" size="sm" icon={<CheckCircle className="w-3.5 h-3.5" />}>Complete</Button>}
          {task.status === 'pending' && <Button variant="primary" size="sm" icon={<Play className="w-3.5 h-3.5" />}>Start</Button>}
          {task.status !== 'postponed' && task.status !== 'completed' && task.status !== 'cancelled' && <Button variant="ghost" size="sm" icon={<Pause className="w-3.5 h-3.5" />}>Postpone</Button>}
        </div>
        <Button variant="ghost" size="sm" icon={<Edit2 className="w-3.5 h-3.5" />}>Edit</Button>
      </div>
    </Modal>
  );
}

export function TaskManagement() {
  const [selectedTask, setSelectedTask] = useState<Task | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [filterStatus, setFilterStatus] = useState<string>('all');

  const filteredTasks = filterStatus === 'all' ? mockTasks : mockTasks.filter(t => t.status === filterStatus);

  const statusCounts = {
    all: mockTasks.length,
    pending: mockTasks.filter(t => t.status === 'pending').length,
    in_progress: mockTasks.filter(t => t.status === 'in_progress').length,
    completed: mockTasks.filter(t => t.status === 'completed').length,
    postponed: mockTasks.filter(t => t.status === 'postponed').length,
  };

  return (
    <AppShell title="Tasks" subtitle="Task management">
      <div className="space-y-4">
        {/* Status tabs */}
        <div className="flex flex-wrap gap-1">
          {Object.entries(statusCounts).map(([status, count]) => (
            <button key={status} onClick={() => setFilterStatus(status)} className={`px-3 py-1.5 rounded text-xs font-medium transition-all ${filterStatus === status ? 'bg-primary-600 text-white' : 'bg-white dark:bg-secondary-800 text-secondary-600 hover:bg-secondary-50'}`}>
              {status === 'all' ? 'All' : status.replace('_', ' ')}
              <span className={`ml-1.5 px-1.5 py-0.5 rounded text-[10px] ${filterStatus === status ? 'bg-white/20' : 'bg-secondary-100'}`}>{count}</span>
            </button>
          ))}
        </div>

        {/* Task cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          {filteredTasks.map(task => (
            <Card key={task.id} className="cursor-pointer hover:shadow transition-all p-3" onClick={() => setSelectedTask(task)}>
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-1.5">
                  <div className={`p-1 rounded ${priorityColors[task.priority]}`}>{statusIcons[task.status]}</div>
                  <Badge size="sm" variant={task.priority === 'critical' ? 'error' : task.priority === 'high' ? 'warning' : 'default'}>{task.priority}</Badge>
                </div>
                <Badge size="sm">{task.status.replace('_', ' ')}</Badge>
              </div>
              <h3 className="text-sm font-medium text-secondary-900 dark:text-white mb-1">{task.title}</h3>
              <p className="text-xs text-secondary-500 line-clamp-1 mb-2">{task.description}</p>
              <div className="flex items-center justify-between text-xs text-secondary-500">
                <div className="flex items-center gap-1"><Calendar className="w-3 h-3" />{new Date(task.dueDate).toLocaleDateString()}</div>
                <div className="flex -space-x-1.5">
                  {task.assignedTo.slice(0, 2).map(user => (
                    <div key={user.id} className="w-5 h-5 rounded-full bg-primary-100 border border-white flex items-center justify-center" title={user.displayName}>
                      <span className="text-[8px] font-medium text-primary-700">{user.firstName[0]}{user.lastName[0]}</span>
                    </div>
                  ))}
                  {task.assignedTo.length > 2 && <div className="w-5 h-5 rounded-full bg-secondary-200 border border-white flex items-center justify-center"><span className="text-[8px] text-secondary-600">+{task.assignedTo.length - 2}</span></div>}
                </div>
              </div>
            </Card>
          ))}
        </div>

        {filteredTasks.length === 0 && (
          <Card className="text-center py-8">
            <AlertTriangle className="w-8 h-8 mx-auto text-secondary-400 mb-2" />
            <p className="text-xs text-secondary-500">No tasks found</p>
            <Button variant="primary" size="sm" className="mt-3" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => setShowCreateModal(true)}>Create</Button>
          </Card>
        )}

        {/* Create button */}
        <div className="fixed bottom-4 right-4">
          <Button variant="primary" size="md" className="rounded-full shadow-lg" icon={<Plus className="w-4 h-4" />} onClick={() => setShowCreateModal(true)}>New Task</Button>
        </div>

        <TaskDetailModal task={selectedTask} isOpen={!!selectedTask} onClose={() => setSelectedTask(null)} />

        <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="New Task" size="md">
          <div className="space-y-3">
            <Input label="Title" placeholder="Task title" required />
            <Textarea label="Description" placeholder="Describe the task" rows={2} />
            <div className="grid grid-cols-2 gap-2">
              <Select label="Priority" options={[{ value: 'low', label: 'Low' }, { value: 'medium', label: 'Medium' }, { value: 'high', label: 'High' }, { value: 'critical', label: 'Critical' }]} defaultValue="medium" />
              <Input label="Due Date" type="date" required />
            </div>
            <div className="grid grid-cols-2 gap-2">
              <Input label="Hours" type="number" placeholder="Estimated" />
              <Select label="Assignees" options={mockEmployees.map(e => ({ value: e.id, label: e.displayName }))} placeholder="Select" />
            </div>
          </div>
          <div className="flex justify-end gap-2 mt-4">
            <Button variant="ghost" size="sm" onClick={() => setShowCreateModal(false)}>Cancel</Button>
            <Button variant="primary" size="sm">Create</Button>
          </div>
        </Modal>
      </div>
    </AppShell>
  );
}
