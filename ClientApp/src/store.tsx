import {
  createContext,
  useContext,
  useMemo,
  useState,
  type PropsWithChildren,
} from "react";
import {
  QueryClient,
  QueryClientProvider,
  useQuery,
} from "@tanstack/react-query";
import { createInitialStore } from "./mockData";
import { roles } from "./config";
import type {
  AppContextValue,
  AppRole,
  EntityKey,
  EntityRecord,
  EntityStore,
  SearchResult,
  ThemeMode,
  ToastMessage,
} from "./types";

const AppContext = createContext<AppContextValue | null>(null);

const queryClient = new QueryClient();

const createToastId = () => `toast-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;

export const AppProviders = ({ children }: PropsWithChildren) => {
  const [role, setRole] = useState<AppRole>("performance-manager");
  const [theme, setTheme] = useState<ThemeMode>("light");
  const [loggedIn, setLoggedIn] = useState(false);
  const [dataVersion, setDataVersion] = useState(0);
  const [store, setStore] = useState<EntityStore>(() => createInitialStore());
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  const addToast = (toast: Omit<ToastMessage, "id">) => {
    const next = { ...toast, id: createToastId() };
    setToasts((current) => [...current, next]);
    window.setTimeout(() => {
      setToasts((current) => current.filter((item) => item.id !== next.id));
    }, 2800);
  };

  const bumpData = () => {
    setDataVersion((value) => value + 1);
    queryClient.invalidateQueries();
  };

  const createRecord = <T extends EntityRecord>(key: EntityKey, record: T) => {
    setStore((current) => ({
      ...current,
      [key]: [record, ...(current[key] as T[])],
    }));
    bumpData();
    addToast({
      title: "Saved successfully",
      description: `${record.name} was created.`,
      tone: "success",
    });
  };

  const updateRecord = <T extends EntityRecord>(key: EntityKey, id: string, record: T) => {
    setStore((current) => ({
      ...current,
      [key]: (current[key] as T[]).map((item) => (item.id === id ? record : item)),
    }));
    bumpData();
    addToast({
      title: "Updated successfully",
      description: `${record.name} was updated.`,
      tone: "success",
    });
  };

  const deleteRecord = (key: EntityKey, id: string) => {
    const existing = (store[key] as EntityRecord[]).find((item) => item.id === id);
    setStore((current) => ({
      ...current,
      [key]: (current[key] as EntityRecord[]).filter((item) => item.id !== id),
    }));
    bumpData();
    addToast({
      title: "Deleted successfully",
      description: `${existing?.name ?? "Record"} was removed.`,
      tone: "success",
    });
  };

  const value = useMemo<AppContextValue>(
    () => ({
      role,
      theme,
      loggedIn,
      toasts,
      dataVersion,
      store,
      setRole,
      toggleTheme: () => setTheme((current) => (current === "light" ? "dark" : "light")),
      login: () => setLoggedIn(true),
      logout: () => setLoggedIn(false),
      createRecord,
      updateRecord,
      deleteRecord,
      addToast,
    }),
    [role, theme, loggedIn, toasts, dataVersion, store],
  );

  return (
    <QueryClientProvider client={queryClient}>
      <AppContext.Provider value={value}>{children}</AppContext.Provider>
    </QueryClientProvider>
  );
};

export const useAppContext = () => {
  const context = useContext(AppContext);
  if (!context) {
    throw new Error("App context is not available.");
  }
  return context;
};

export const useEntityRecords = <T extends EntityRecord>(key: EntityKey) => {
  const { store, dataVersion } = useAppContext();
  return useQuery({
    queryKey: ["entity-records", key, dataVersion],
    queryFn: async () => store[key] as T[],
    initialData: store[key] as T[],
  });
};

export const useEntityRecord = <T extends EntityRecord>(key: EntityKey, id?: string) => {
  const { store, dataVersion } = useAppContext();
  return useQuery({
    queryKey: ["entity-record", key, id, dataVersion],
    queryFn: async () => (store[key] as T[]).find((item) => item.id === id) ?? null,
    initialData: (store[key] as T[]).find((item) => item.id === id) ?? null,
    enabled: Boolean(id),
  });
};

export const useGlobalSearch = (term: string): SearchResult[] => {
  const { store } = useAppContext();

  return useMemo(() => {
    const normalized = term.trim().toLowerCase();
    if (!normalized) {
      return [];
    }

    const entityResults = Object.entries(store).flatMap(([key, value]) =>
      (value as EntityRecord[])
        .filter((record) => {
          const text = `${record.name} ${record.code ?? ""} ${record.description ?? ""}`.toLowerCase();
          return text.includes(normalized);
        })
        .slice(0, 3)
        .map((record) => ({
          id: `${key}-${record.id}`,
          label: record.name,
          sublabel: record.code ?? key,
          path: guessPath(key as EntityKey, record.id),
          icon: "search",
        })),
    );

    const roleResults = roles
      .filter((item) => item.label.toLowerCase().includes(normalized))
      .map((item) => ({
        id: item.id,
        label: item.label,
        sublabel: item.dashboardSummary,
        path: "/app/dashboard",
        icon: "person",
      }));

    return [...entityResults, ...roleResults].slice(0, 10);
  }, [store, term]);
};

const guessPath = (key: EntityKey, id: string) => {
  const map: Record<EntityKey, string> = {
    opmsTargets: `/app/opms/targets/${id}`,
    ipmsTargets: `/app/ipms/targets/${id}`,
    opmsSubmissions: `/app/opms/submissions/${id}`,
    ipmsSubmissions: `/app/ipms/submissions/${id}`,
    voteNumbers: `/app/opms/vote-numbers/${id}`,
    kpiLibrary: `/app/kpi-library/${id}`,
    employees: `/app/human-resources/employees/${id}`,
    departments: `/app/human-resources/departments/${id}`,
    departmentUnits: `/app/human-resources/department-units/${id}`,
    positions: `/app/human-resources/positions/${id}`,
    occupations: `/app/human-resources/occupations/${id}`,
    contacts: `/app/human-resources/contacts/${id}`,
    resumes: `/app/human-resources/resumes/${id}`,
    portfolioFiles: `/app/human-resources/portfolio-files/${id}`,
    tasks: `/app/human-resources/tasks/${id}`,
    periods: `/app/administration/periods/${id}`,
    organisations: `/app/administration/organisations/${id}`,
    industries: `/app/administration/industries/${id}`,
    approvalSetup: `/app/administration/approval-setup/${id}`,
    budgetSources: `/app/administration/budget-sources/${id}`,
    budgetTypes: `/app/administration/budget-types/${id}`,
    strategicGoals: `/app/administration/strategic-goals/${id}`,
    strategicObjectives: `/app/administration/strategic-objectives/${id}`,
    unitsOfMeasure: `/app/administration/unit-of-measure/${id}`,
    kpas: `/app/administration/kpas/${id}`,
    municipalKpas: `/app/administration/municipal-kpas/${id}`,
    departmentalObjectives: `/app/administration/departmental-objectives/${id}`,
    outputs: `/app/administration/outputs/${id}`,
    performanceObjectives: `/app/administration/performance-objectives/${id}`,
    priorityIssues: `/app/administration/priority-issues/${id}`,
    countries: `/app/location-setup/countries/${id}`,
    provinces: `/app/location-setup/provinces/${id}`,
    cities: `/app/location-setup/cities/${id}`,
    suburbs: `/app/location-setup/suburbs/${id}`,
    addresses: `/app/location-setup/addresses/${id}`,
    reports: `/app/reports`,
  };
  return map[key];
};
