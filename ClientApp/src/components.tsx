import clsx from "clsx";
import {
  Fragment,
  useEffect,
  useMemo,
  useState,
  type ChangeEvent,
  type PropsWithChildren,
  type ReactNode,
} from "react";
import { Link, NavLink, useNavigate } from "react-router-dom";
import { reportRoutes, roles, sidebarGroups } from "./config";
import { useAppContext, useGlobalSearch } from "./store";
import type {
  FileAsset,
  ModuleRoute,
  SearchResult,
  SidebarGroup,
  StatusTone,
} from "./types";

const tones: Record<StatusTone, string> = {
  default: "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200",
  success: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300",
  warning: "bg-amber-100 text-amber-700 dark:bg-amber-950/40 dark:text-amber-300",
  danger: "bg-rose-100 text-rose-700 dark:bg-rose-950/40 dark:text-rose-300",
  info: "bg-sky-100 text-sky-700 dark:bg-sky-950/40 dark:text-sky-300",
  accent: "bg-blue-100 text-blue-700 dark:bg-blue-950/40 dark:text-blue-300",
};

const cardBorder: Record<StatusTone, string> = {
  default: "border-slate-200 dark:border-slate-800",
  success: "border-emerald-200 dark:border-emerald-900/50",
  warning: "border-amber-200 dark:border-amber-900/50",
  danger: "border-rose-200 dark:border-rose-900/50",
  info: "border-sky-200 dark:border-sky-900/50",
  accent: "border-blue-200 dark:border-blue-900/50",
};

export const MaterialIcon = ({ icon, className }: { icon: string; className?: string }) => (
  <span className={clsx("material-symbols-rounded text-[20px]", className)}>{icon}</span>
);

export const StatusBadge = ({
  label,
  tone = "default",
}: {
  label: string;
  tone?: StatusTone;
}) => <span className={clsx("inline-flex rounded-full px-2.5 py-1 text-xs font-semibold", tones[tone])}>{label}</span>;

export const DashboardCard = ({
  title,
  value,
  delta,
  icon,
  tone,
}: {
  title: string;
  value: string;
  delta: string;
  icon: string;
  tone: StatusTone;
}) => (
  <div className={clsx("rounded-2xl border bg-white p-5 shadow-sm dark:bg-slate-900", cardBorder[tone])}>
    <div className="flex items-start justify-between">
      <div>
        <p className="text-sm font-medium text-slate-500 dark:text-slate-400">{title}</p>
        <p className="mt-2 text-3xl font-black tracking-tight text-slate-900 dark:text-slate-50">{value}</p>
        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">{delta}</p>
      </div>
      <span className={clsx("rounded-xl p-3", tones[tone])}>
        <MaterialIcon icon={icon} />
      </span>
    </div>
  </div>
);

export const EmptyState = ({
  title,
  description,
  actionLabel,
  onAction,
}: {
  title: string;
  description: string;
  actionLabel: string;
  onAction: () => void;
}) => (
  <div className="rounded-2xl border border-dashed border-slate-300 bg-white px-8 py-16 text-center dark:border-slate-700 dark:bg-slate-900">
    <div className="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-2xl bg-blue-50 text-blue-600 dark:bg-blue-950/40 dark:text-blue-300">
      <MaterialIcon icon="add_circle" />
    </div>
    <h3 className="text-lg font-bold text-slate-900 dark:text-slate-50">{title}</h3>
    <p className="mx-auto mt-2 max-w-2xl text-sm text-slate-500 dark:text-slate-400">{description}</p>
    <button
      onClick={onAction}
      className="mt-5 inline-flex rounded-xl bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700"
    >
      {actionLabel}
    </button>
  </div>
);

export const LoadingSkeleton = () => (
  <div className="space-y-4">
    <div className="h-10 animate-pulse rounded-xl bg-slate-200 dark:bg-slate-800" />
    <div className="h-72 animate-pulse rounded-2xl bg-slate-200 dark:bg-slate-800" />
  </div>
);

export const ConfirmDialog = ({
  open,
  title,
  description,
  confirmLabel,
  onCancel,
  onConfirm,
}: {
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  onCancel: () => void;
  onConfirm: () => void;
}) =>
  open ? (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4">
      <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-2xl dark:border-slate-800 dark:bg-slate-900">
        <h3 className="text-lg font-bold text-slate-900 dark:text-slate-50">{title}</h3>
        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">{description}</p>
        <div className="mt-5 flex justify-end gap-3">
          <button
            onClick={onCancel}
            className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:text-slate-200"
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            className="rounded-xl bg-rose-600 px-4 py-2 text-sm font-semibold text-white hover:bg-rose-700"
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  ) : null;

export const ActionMenu = ({ children }: PropsWithChildren) => (
  <div className="flex flex-wrap items-center gap-2">{children}</div>
);

export const FormTabs = ({
  tabs,
  value,
  onChange,
}: {
  tabs: Array<{ key: string; label: string }>;
  value: string;
  onChange: (value: string) => void;
}) => (
  <div className="scrollbar-none -mx-1 flex overflow-x-auto px-1">
    {tabs.map((tab) => (
      <button
        key={tab.key}
        onClick={() => onChange(tab.key)}
        className={clsx(
          "mr-2 whitespace-nowrap rounded-xl px-3 py-2 text-sm font-semibold",
          value === tab.key
            ? "bg-blue-600 text-white"
            : "bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700",
        )}
      >
        {tab.label}
      </button>
    ))}
  </div>
);

export const FormSection = ({
  title,
  description,
  children,
}: PropsWithChildren<{ title: string; description?: string }>) => (
  <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
    <div className="mb-4">
      <h3 className="text-lg font-bold text-slate-900 dark:text-slate-50">{title}</h3>
      {description ? <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{description}</p> : null}
    </div>
    {children}
  </section>
);

export const ToastProvider = () => {
  const { toasts } = useAppContext();
  return (
    <div className="pointer-events-none fixed bottom-5 right-5 z-50 space-y-3">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={clsx(
            "min-w-[280px] rounded-2xl border bg-white p-4 shadow-2xl dark:bg-slate-900",
            cardBorder[toast.tone],
          )}
        >
          <div className="flex items-start gap-3">
            <span className={clsx("rounded-xl p-2", tones[toast.tone])}>
              <MaterialIcon icon="notifications" />
            </span>
            <div>
              <p className="text-sm font-bold text-slate-900 dark:text-slate-50">{toast.title}</p>
              <p className="text-sm text-slate-500 dark:text-slate-400">{toast.description}</p>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};

export const DataTable = <T extends { id: string }>({
  columns,
  data,
  renderCell,
  rowActions,
}: {
  columns: string[];
  data: T[];
  renderCell: (record: T, column: string) => ReactNode;
  rowActions?: (record: T) => ReactNode;
}) => (
  <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
        <thead className="bg-slate-50 dark:bg-slate-950">
          <tr>
            {columns.map((column) => (
              <th
                key={column}
                className="px-4 py-3 text-left text-xs font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400"
              >
                {column}
              </th>
            ))}
            {rowActions ? (
              <th className="px-4 py-3 text-right text-xs font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">
                Actions
              </th>
            ) : null}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
          {data.map((record) => (
            <tr key={record.id} className="hover:bg-slate-50/80 dark:hover:bg-slate-800/40">
              {columns.map((column) => (
                <td key={column} className="px-4 py-3 align-top text-slate-700 dark:text-slate-200">
                  {renderCell(record, column)}
                </td>
              ))}
              {rowActions ? <td className="px-4 py-3 text-right">{rowActions(record)}</td> : null}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  </div>
);

export const ReportFilters = ({
  children,
  onReset,
  onExportPdf,
  onExportExcel,
}: PropsWithChildren<{
  onReset: () => void;
  onExportPdf: () => void;
  onExportExcel: () => void;
}>) => (
  <FormSection title="Report Filters" description="Filter the current dataset before previewing or exporting the report.">
    <div className="grid gap-4 md:grid-cols-4">{children}</div>
    <div className="mt-4 flex flex-wrap items-center justify-end gap-3">
      <button
        onClick={onReset}
        className="rounded-xl border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:text-slate-200"
      >
        Reset Filters
      </button>
      <button
        onClick={onExportExcel}
        className="rounded-xl border border-emerald-300 px-3 py-2 text-sm font-semibold text-emerald-700 dark:border-emerald-900 dark:text-emerald-300"
      >
        Export Excel
      </button>
      <button
        onClick={onExportPdf}
        className="rounded-xl border border-rose-300 px-3 py-2 text-sm font-semibold text-rose-700 dark:border-rose-900 dark:text-rose-300"
      >
        Export PDF
      </button>
    </div>
  </FormSection>
);

export const FileUploadManager = ({
  files,
  onChange,
}: {
  files: FileAsset[];
  onChange: (files: FileAsset[]) => void;
}) => {
  const { addToast } = useAppContext();
  const [documentType, setDocumentType] = useState("Evidence");

  const updateProgress = (id: string, progress: number) => {
    onChange(
      files.map((item) =>
        item.id === id ? { ...item, progress, uploading: progress < 100 } : item,
      ),
    );
  };

  const addFiles = (selected: FileList | null) => {
    if (!selected?.length) return;

    const created: FileAsset[] = Array.from(selected).map((entry) => ({
      id: `upload-${Date.now()}-${entry.name}`,
      fileName: entry.name,
      documentType,
      sizeLabel: `${Math.max(1, Math.round(entry.size / 1024))} KB`,
      uploadedAt: new Date().toISOString(),
      uploadedBy: "Current User",
      progress: 0,
      uploading: true,
      notes: "Simulated upload",
    }));

    onChange([...created, ...files]);
    addToast({
      title: "Upload started",
      description: `${created.length} file(s) added to the queue.`,
      tone: "info",
    });

    created.forEach((entry) => {
      let current = 0;
      const interval = window.setInterval(() => {
        current += 25;
        updateProgress(entry.id, Math.min(current, 100));
        if (current >= 100) {
          window.clearInterval(interval);
        }
      }, 180);
    });
  };

  const handleInput = (event: ChangeEvent<HTMLInputElement>) => {
    addFiles(event.target.files);
    event.target.value = "";
  };

  return (
    <FormSection title="File Upload Manager" description="Drag and drop, assign document types, and manage uploaded evidence files.">
      <div
        onDragOver={(event) => event.preventDefault()}
        onDrop={(event) => {
          event.preventDefault();
          addFiles(event.dataTransfer.files);
        }}
        className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-6 dark:border-slate-700 dark:bg-slate-950/40"
      >
        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div>
            <h4 className="text-base font-bold text-slate-900 dark:text-slate-50">Drop files here or browse</h4>
            <p className="text-sm text-slate-500 dark:text-slate-400">
              Multiple files, replace/delete actions, metadata and download simulation are supported.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <select
              value={documentType}
              onChange={(event) => setDocumentType(event.target.value)}
              className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
            >
              <option>Evidence</option>
              <option>Planning</option>
              <option>Financial</option>
              <option>Portfolio</option>
              <option>Attachment</option>
            </select>
            <label className="cursor-pointer rounded-xl bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700">
              Upload Files
              <input type="file" multiple className="hidden" onChange={handleInput} />
            </label>
          </div>
        </div>
      </div>

      <div className="mt-4 space-y-3">
        {files.map((item) => (
          <div
            key={item.id}
            className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-slate-50 p-4 dark:border-slate-800 dark:bg-slate-950/40 md:flex-row md:items-center"
          >
            <div className="flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <p className="font-semibold text-slate-900 dark:text-slate-50">{item.fileName}</p>
                <StatusBadge
                  label={item.documentType}
                  tone={item.documentType === "Portfolio" ? "accent" : "info"}
                />
              </div>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                {item.sizeLabel} · {item.uploadedBy} · {new Date(item.uploadedAt).toLocaleDateString()}
              </p>
              {item.uploading ? (
                <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
                  <div className="h-full rounded-full bg-blue-600 transition-all" style={{ width: `${item.progress ?? 0}%` }} />
                </div>
              ) : null}
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <button className="rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold dark:border-slate-700">
                View
              </button>
              <button className="rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold dark:border-slate-700">
                Download
              </button>
              <button className="rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold dark:border-slate-700">
                Replace
              </button>
              <button
                onClick={() => onChange(files.filter((entry) => entry.id !== item.id))}
                className="rounded-xl border border-rose-300 px-3 py-2 text-xs font-semibold text-rose-700 dark:border-rose-900 dark:text-rose-300"
              >
                Delete
              </button>
            </div>
          </div>
        ))}
        {!files.length ? <p className="text-sm text-slate-500 dark:text-slate-400">No files uploaded yet.</p> : null}
      </div>
    </FormSection>
  );
};

export const EntityListPage = ({
  title,
  description,
  primaryAction,
  filters,
  children,
}: PropsWithChildren<{
  title: string;
  description: string;
  primaryAction?: ReactNode;
  filters?: ReactNode;
}>) => (
  <div className="space-y-5">
    <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
      <div>
        <h1 className="text-3xl font-black tracking-tight text-slate-900 dark:text-slate-50">{title}</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{description}</p>
      </div>
      {primaryAction}
    </div>
    {filters}
    {children}
  </div>
);

export const EntityDetailPage = ({
  title,
  description,
  meta,
  actions,
  children,
}: PropsWithChildren<{
  title: string;
  description?: string;
  meta?: Array<{ label: string; value: ReactNode }>;
  actions?: ReactNode;
}>) => (
  <div className="space-y-5">
    <div className="flex flex-col gap-4 rounded-3xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900 lg:flex-row lg:items-start lg:justify-between">
      <div>
        <h1 className="text-3xl font-black tracking-tight text-slate-900 dark:text-slate-50">{title}</h1>
        {description ? <p className="mt-2 max-w-3xl text-sm text-slate-500 dark:text-slate-400">{description}</p> : null}
        {meta?.length ? (
          <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            {meta.map((item) => (
              <div key={item.label} className="rounded-2xl bg-slate-50 p-4 dark:bg-slate-950/40">
                <p className="text-xs font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">{item.label}</p>
                <div className="mt-1 text-sm font-semibold text-slate-900 dark:text-slate-50">{item.value}</div>
              </div>
            ))}
          </div>
        ) : null}
      </div>
      {actions}
    </div>
    {children}
  </div>
);

export const EntityFormPage = ({
  title,
  description,
  actions,
  children,
}: PropsWithChildren<{
  title: string;
  description: string;
  actions?: ReactNode;
}>) => (
  <div className="space-y-5">
    <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
      <div>
        <h1 className="text-3xl font-black tracking-tight text-slate-900 dark:text-slate-50">{title}</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{description}</p>
      </div>
      {actions}
    </div>
    {children}
  </div>
);

const SidebarSection = ({ group, onNavigate }: { group: SidebarGroup; onNavigate?: () => void }) => (
  <div className="space-y-2">
    <p className="px-3 text-xs font-bold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-500">{group.label}</p>
    <div className="space-y-1">
      {group.items.map((item) => (
        <NavLink
          key={item.key}
          to={item.path}
          onClick={onNavigate}
          className={({ isActive }) =>
            clsx(
              "flex items-center gap-3 rounded-xl px-3 py-2 text-sm font-semibold transition",
              isActive
                ? "bg-white text-slate-950 shadow-sm dark:bg-slate-800 dark:text-white"
                : "text-slate-300 hover:bg-slate-800 hover:text-white",
            )
          }
        >
          <MaterialIcon icon={item.icon} className="text-[18px]" />
          <span>{item.label}</span>
        </NavLink>
      ))}
    </div>
  </div>
);

export const Sidebar = ({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) => {
  const { role, logout } = useAppContext();
  const roleInfo = roles.find((item) => item.id === role);

  return (
    <>
      <div
        className={clsx(
          "fixed inset-0 z-30 bg-slate-950/40 transition md:hidden",
          open ? "opacity-100" : "pointer-events-none opacity-0",
        )}
        onClick={onClose}
      />
      <aside
        className={clsx(
          "fixed inset-y-0 left-0 z-40 flex w-80 flex-col border-r border-white/10 bg-[#08111f] text-white transition-transform md:static md:translate-x-0",
          open ? "translate-x-0" : "-translate-x-full",
        )}
      >
        <div className="border-b border-white/10 px-5 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-blue-600 text-xl font-black">F</div>
            <div>
              <p className="text-lg font-black">FTCERP</p>
              <p className="text-xs text-slate-400">Enterprise Resource Planning</p>
            </div>
          </div>
        </div>
        <div className="flex-1 space-y-5 overflow-y-auto px-4 py-5">
          {sidebarGroups.map((group) => (
            <SidebarSection key={group.label} group={group} onNavigate={onClose} />
          ))}
        </div>
        <div className="border-t border-white/10 p-4">
          <div className="rounded-2xl bg-white/5 p-3">
            <p className="text-xs uppercase tracking-[0.18em] text-slate-500">Viewing As</p>
            <p className="mt-1 font-semibold">{roleInfo?.label}</p>
            <button onClick={logout} className="mt-3 rounded-xl bg-slate-800 px-3 py-2 text-sm font-semibold hover:bg-slate-700">
              Logout
            </button>
          </div>
        </div>
      </aside>
    </>
  );
};

const SearchDropdown = ({ results, onSelect }: { results: SearchResult[]; onSelect: () => void }) =>
  results.length ? (
    <div className="absolute left-0 right-0 top-full z-30 mt-2 rounded-2xl border border-slate-200 bg-white p-2 shadow-2xl dark:border-slate-800 dark:bg-slate-900">
      {results.map((result) => (
        <Link
          key={result.id}
          to={result.path}
          onClick={onSelect}
          className="flex items-start gap-3 rounded-xl px-3 py-2 hover:bg-slate-50 dark:hover:bg-slate-800"
        >
          <span className="rounded-xl bg-slate-100 p-2 dark:bg-slate-800">
            <MaterialIcon icon={result.icon} className="text-[16px]" />
          </span>
          <span>
            <span className="block text-sm font-semibold text-slate-900 dark:text-slate-50">{result.label}</span>
            <span className="block text-xs text-slate-500 dark:text-slate-400">{result.sublabel}</span>
          </span>
        </Link>
      ))}
    </div>
  ) : null;

export const Topbar = ({
  onMenuClick,
}: {
  onMenuClick: () => void;
}) => {
  const navigate = useNavigate();
  const { role, setRole, theme, toggleTheme } = useAppContext();
  const [term, setTerm] = useState("");
  const results = useGlobalSearch(term);

  return (
    <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/90 backdrop-blur dark:border-slate-800 dark:bg-slate-950/90">
      <div className="flex flex-wrap items-center gap-3 px-4 py-4 lg:px-8">
        <button onClick={onMenuClick} className="rounded-xl border border-slate-300 p-2 md:hidden dark:border-slate-700">
          <MaterialIcon icon="menu" />
        </button>
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Performance Management</p>
          <h1 className="text-xl font-black text-slate-900 dark:text-slate-50">Dashboard</h1>
        </div>
        <div className="relative ml-auto min-w-[280px] flex-1 lg:max-w-xl">
          <div className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-900">
            <MaterialIcon icon="search" className="text-slate-400" />
            <input
              value={term}
              onChange={(event) => setTerm(event.target.value)}
              placeholder="Search targets, submissions, employees..."
              className="w-full bg-transparent text-sm outline-none placeholder:text-slate-400"
            />
          </div>
          <SearchDropdown results={results} onSelect={() => setTerm("")} />
        </div>
        <button
          onClick={toggleTheme}
          className="rounded-2xl border border-slate-200 px-4 py-3 text-sm font-semibold dark:border-slate-800"
        >
          {theme === "light" ? "Dark mode" : "Light mode"}
        </button>
        <select
          value={role}
          onChange={(event) => {
            setRole(event.target.value as typeof role);
            navigate("/app/dashboard");
          }}
          className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-semibold dark:border-slate-800 dark:bg-slate-900"
        >
          {roles.map((item) => (
            <option key={item.id} value={item.id}>
              {item.label}
            </option>
          ))}
        </select>
      </div>
    </header>
  );
};

export const AppLayout = ({ children }: PropsWithChildren) => {
  const { theme } = useAppContext();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  useEffect(() => {
    document.documentElement.classList.toggle("dark", theme === "dark");
  }, [theme]);

  return (
    <div className={clsx("min-h-screen bg-slate-100 text-slate-900 dark:bg-[#070c15] dark:text-slate-100", theme === "dark" && "dark")}>
      <div className="flex min-h-screen">
        <Sidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)} />
        <div className="min-w-0 flex-1">
          <Topbar onMenuClick={() => setSidebarOpen(true)} />
          <main className="mx-auto max-w-[1680px] px-4 py-6 lg:px-8">{children}</main>
        </div>
      </div>
      <ToastProvider />
    </div>
  );
};

export const OverviewLinks = ({ routes }: { routes: ModuleRoute[] }) => (
  <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
    {routes.map((route) => (
      <Link
        key={route.key}
        to={route.path}
        className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md dark:border-slate-800 dark:bg-slate-900"
      >
        <div className="flex items-start gap-3">
          <span className="rounded-2xl bg-blue-50 p-3 text-blue-600 dark:bg-blue-950/40 dark:text-blue-300">
            <MaterialIcon icon={route.icon} />
          </span>
          <div>
            <h3 className="font-bold text-slate-900 dark:text-slate-50">{route.label}</h3>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{route.category}</p>
          </div>
        </div>
      </Link>
    ))}
  </div>
);

export const ReportsDirectory = () => (
  <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
    {reportRoutes.map((report) => (
      <Link
        key={report.path}
        to={report.path}
        className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:shadow-md dark:border-slate-800 dark:bg-slate-900"
      >
        <div className="flex items-start gap-3">
          <span className="rounded-2xl bg-blue-50 p-3 text-blue-600 dark:bg-blue-950/40 dark:text-blue-300">
            <MaterialIcon icon="monitoring" />
          </span>
          <div>
            <h3 className="font-bold text-slate-900 dark:text-slate-50">{report.label}</h3>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Interactive report with filters and export actions.</p>
          </div>
        </div>
      </Link>
    ))}
  </div>
);
