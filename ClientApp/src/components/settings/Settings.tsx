import { useEffect, useState } from 'react';
import { User, Bell, Shield, Palette, Globe, Save } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Card, Badge } from '../ui';
import { Tabs } from '../common/Tabs';
import { Input, Select, Checkbox, FormSection, FormRow } from '../common/Form';
import { useApp } from '../../context/AppContext';

type SettingsTabId = 'profile' | 'notifications' | 'appearance' | 'security';

const SETTINGS_TAB_STORAGE_KEY = 'settings_active_tab';
const SETTINGS_TABS: { id: SettingsTabId; label: string; icon: JSX.Element }[] = [
  { id: 'profile', label: 'Profile', icon: <User className="w-3.5 h-3.5" /> },
  { id: 'notifications', label: 'Notifications', icon: <Bell className="w-3.5 h-3.5" /> },
  { id: 'appearance', label: 'Appearance', icon: <Palette className="w-3.5 h-3.5" /> },
  { id: 'security', label: 'Security', icon: <Shield className="w-3.5 h-3.5" /> },
];

function readStoredSettingsTab(): SettingsTabId {
  try {
    const storedTab = localStorage.getItem(SETTINGS_TAB_STORAGE_KEY);
    if (storedTab && SETTINGS_TABS.some(tab => tab.id === storedTab)) {
      return storedTab as SettingsTabId;
    }
  } catch {
    // ignore storage issues and fall back to the default tab
  }

  return 'profile';
}

function ProfileSettings() {
  const { userProfile } = useApp();
  const initials = `${userProfile?.firstName?.[0] ?? ''}${userProfile?.lastName?.[0] ?? ''}`.toUpperCase();
  const fullName = userProfile?.fullName
    ?? [userProfile?.firstName, userProfile?.lastName].filter(Boolean).join(' ');

  return (
    <div className="space-y-3">
      <FormSection title="Personal">
        <FormRow cols={2}>
          <Input label="First Name" defaultValue={userProfile?.firstName} />
          <Input label="Last Name" defaultValue={userProfile?.lastName} />
        </FormRow>
        <FormRow cols={2}>
          <Input label="Display Name" defaultValue={userProfile?.firstName && userProfile?.lastName ? `${userProfile.firstName} ${userProfile.lastName}` : ''} />
          <Input label="Email" type="email" defaultValue={userProfile?.email} />
        </FormRow>
      </FormSection>
      <FormSection title="Avatar">
        <div className="flex items-center gap-4">
          <div className="w-12 h-12 rounded-full bg-primary-100 flex items-center justify-center overflow-hidden">
            <span className="text-lg font-bold text-primary-600" aria-label={fullName || 'User avatar'}>
              {initials || 'U'}
            </span>
          </div>
          <Button variant="outline" size="sm">Change</Button>
        </div>
      </FormSection>
    </div>
  );
}

function NotificationSettings() {
  const [settings, setSettings] = useState({
    emailSubmissions: true,
    emailApprovals: true,
    emailOverdue: true,
    pushSubmissions: true,
    pushApprovals: false,
    pushOverdue: true,
    digest: true,
    weeklyReport: false,
  });

  return (
    <div className="space-y-3">
      <FormSection title="Email">
        <div className="space-y-2">
          <Checkbox label="Submissions" description="New submission assignments" checked={settings.emailSubmissions} onChange={(e) => setSettings({ ...settings, emailSubmissions: e.target.checked })} />
          <Checkbox label="Approvals" description="Pending approval items" checked={settings.emailApprovals} onChange={(e) => setSettings({ ...settings, emailApprovals: e.target.checked })} />
          <Checkbox label="Overdue alerts" description="Overdue submission alerts" checked={settings.emailOverdue} onChange={(e) => setSettings({ ...settings, emailOverdue: e.target.checked })} />
        </div>
      </FormSection>
      <FormSection title="In-App">
        <div className="space-y-2">
          <Checkbox label="Submission updates" description="Status changes" checked={settings.pushSubmissions} onChange={(e) => setSettings({ ...settings, pushSubmissions: e.target.checked })} />
          <Checkbox label="Approval requests" checked={settings.pushApprovals} onChange={(e) => setSettings({ ...settings, pushApprovals: e.target.checked })} />
          <Checkbox label="Overdue reminders" checked={settings.pushOverdue} onChange={(e) => setSettings({ ...settings, pushOverdue: e.target.checked })} />
        </div>
      </FormSection>
      <FormSection title="Reports">
        <div className="space-y-2">
          <Checkbox label="Daily digest" checked={settings.digest} onChange={(e) => setSettings({ ...settings, digest: e.target.checked })} />
          <Checkbox label="Weekly summary" checked={settings.weeklyReport} onChange={(e) => setSettings({ ...settings, weeklyReport: e.target.checked })} />
        </div>
      </FormSection>
    </div>
  );
}

function AppearanceSettings() {
  const { darkMode, toggleDarkMode } = useApp();

  return (
    <div className="space-y-3">
      <FormSection title="Theme">
        <div className="grid grid-cols-3 gap-2">
          <button onClick={() => darkMode && toggleDarkMode()} className={`p-3 rounded-lg border-2 transition-all ${!darkMode ? 'border-primary-600 bg-primary-50' : 'border-secondary-200 hover:border-secondary-300'}`}>
            <div className="w-full h-8 bg-white rounded border border-secondary-200 mb-1 flex items-center justify-center"><span className="text-sm">☀️</span></div>
            <p className="text-xs font-medium">Light</p>
          </button>
          <button onClick={() => !darkMode && toggleDarkMode()} className={`p-3 rounded-lg border-2 transition-all ${darkMode ? 'border-primary-600 bg-primary-50' : 'border-secondary-200 hover:border-secondary-300'}`}>
            <div className="w-full h-8 bg-secondary-900 rounded border border-secondary-700 mb-1 flex items-center justify-center"><span className="text-sm">🌙</span></div>
            <p className="text-xs font-medium">Dark</p>
          </button>
          <button className="p-3 rounded-lg border-2 border-secondary-200 hover:border-secondary-300 transition-all">
            <div className="w-full h-8 rounded border border-secondary-200 mb-1 flex items-center justify-center bg-gradient-to-r from-white to-secondary-900"><span className="text-sm">💻</span></div>
            <p className="text-xs font-medium">System</p>
          </button>
        </div>
      </FormSection>
      <FormSection title="Display">
        <FormRow cols={2}>
          <Select label="Date Format" options={[{ value: 'dd/MM/yyyy', label: 'DD/MM/YYYY' }, { value: 'MM/dd/yyyy', label: 'MM/DD/YYYY' }]} defaultValue="dd/MM/yyyy" />
          <Select label="Language" options={[{ value: 'en', label: 'English' }]} defaultValue="en" />
        </FormRow>
      </FormSection>
    </div>
  );
}

function SecuritySettings() {
  return (
    <div className="space-y-3">
      <FormSection title="Password">
        <div className="space-y-2">
          <Input label="Current Password" type="password" placeholder="Enter current" />
          <FormRow cols={2}>
            <Input label="New Password" type="password" placeholder="New password" />
            <Input label="Confirm" type="password" placeholder="Confirm" />
          </FormRow>
          <Button variant="outline" size="sm">Change Password</Button>
        </div>
      </FormSection>
      <FormSection title="Two-Factor">
        <div className="flex items-center justify-between p-3 bg-secondary-50 dark:bg-secondary-800 rounded">
          <div className="flex items-center gap-2">
            <Shield className="w-4 h-4 text-secondary-400" />
            <div>
              <p className="text-xs font-medium">2FA</p>
              <p className="text-[10px] text-secondary-500">Extra security</p>
            </div>
          </div>
          <Badge variant="warning" size="sm">Disabled</Badge>
        </div>
        <Button variant="outline" size="sm">Enable</Button>
      </FormSection>
      <FormSection title="Sessions">
        <div className="flex items-center justify-between p-3 border border-secondary-200 rounded">
          <div className="flex items-center gap-2">
            <Globe className="w-4 h-4 text-secondary-400" />
            <div>
              <p className="text-xs font-medium">Current Session</p>
              <p className="text-[10px] text-secondary-500">Chrome • Active</p>
            </div>
          </div>
          <Badge variant="success" size="sm">Active</Badge>
        </div>
      </FormSection>
    </div>
  );
}

export function Settings() {
  const [activeTab, setActiveTab] = useState<SettingsTabId>(() => readStoredSettingsTab());

  useEffect(() => {
    try {
      localStorage.setItem(SETTINGS_TAB_STORAGE_KEY, activeTab);
    } catch {
      // ignore storage issues and keep the current in-memory tab
    }
  }, [activeTab]);

  const renderTabContent = () => {
    switch (activeTab) {
      case 'profile': return <ProfileSettings />;
      case 'notifications': return <NotificationSettings />;
      case 'appearance': return <AppearanceSettings />;
      case 'security': return <SecuritySettings />;
      default: return <ProfileSettings />;
    }
  };

  return (
    <AppShell title="Settings" subtitle="Account preferences">
      <div className="max-w-3xl mx-auto space-y-4">
        <Tabs tabs={SETTINGS_TABS} activeTab={activeTab} onChange={(tabId) => setActiveTab(tabId as SettingsTabId)} variant="underline" />
        <Card>
          {renderTabContent()}
        </Card>
        <div className="flex items-center justify-end gap-2">
          <Button variant="outline" size="sm">Cancel</Button>
          <Button variant="primary" size="sm" icon={<Save className="w-3.5 h-3.5" />}>Save</Button>
        </div>
      </div>
    </AppShell>
  );
}
