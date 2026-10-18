import { useEffect, useState } from 'react';
import { getC88ConfigurationsPage, getTidConfiguration } from '../../api/api';
import { useSecurity } from '../../context/SecurityContext';
import type { C88Configuration, OPMSTarget } from '../../types';

export interface KpiOptionalModuleState {
  loading: boolean;
  tidVisible: boolean;
  c88Visible: boolean;
  tidError?: string;
  c88Error?: string;
  c88Configuration?: C88Configuration;
}

export function useKpiOptionalModuleState(target: OPMSTarget | null): KpiOptionalModuleState {
  const { canRead } = useSecurity();
  const canReadTid = canRead('TID');
  const canReadC88 = canRead('C88_INDICATOR');
  const [state, setState] = useState<KpiOptionalModuleState>({ loading: true, tidVisible: false, c88Visible: false });

  useEffect(() => {
    let cancelled = false;
    if (!target) {
      setState({ loading: true, tidVisible: false, c88Visible: false });
      return () => { cancelled = true; };
    }

    const load = async () => {
      const [tidResult, c88Result] = await Promise.all([
        canReadTid ? getTidConfiguration() : Promise.resolve(null),
        canReadC88 && target.municipalityFinancialYearPublicId
          ? getC88ConfigurationsPage(
              { page: 1, pageSize: 1, sortBy: 'financialYear', sortDirection: 'desc' },
              { municipalityFinancialYearPublicId: target.municipalityFinancialYearPublicId, enabled: true },
            )
          : Promise.resolve(null),
      ]);
      if (cancelled) return;
      const tidError = tidResult && !tidResult.success ? tidResult.message ?? 'Unable to resolve the municipality TID policy.' : undefined;
      const c88Error = c88Result && !c88Result.success ? c88Result.message ?? 'Unable to resolve the Circular 88 configuration.' : undefined;
      setState({
        loading: false,
        tidVisible: canReadTid && (tidError !== undefined || Boolean(tidResult?.data?.tidEnabled)),
        c88Visible: canReadC88 && (c88Error !== undefined || Boolean(c88Result?.data?.items[0]?.isEnabled)),
        tidError,
        c88Error,
        c88Configuration: c88Result?.success ? c88Result.data?.items[0] : undefined,
      });
    };
    void load();
    return () => { cancelled = true; };
  }, [canReadC88, canReadTid, target]);

  return state;
}
