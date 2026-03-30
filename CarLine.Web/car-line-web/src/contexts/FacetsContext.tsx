import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useState,
} from 'react';

import type { FacetsState } from '../types/facets';

interface FacetsContextValue {
  facets: FacetsState;
  replaceFacets: (next?: FacetsState | null) => void;
  mergeFacets: (next?: FacetsState | null) => void;
  resetFacets: () => void;
}

interface FacetsProviderProps {
  children: ReactNode;
}

const FacetsContext = createContext<FacetsContextValue | null>(null);

export function FacetsProvider({ children }: FacetsProviderProps) {
  const [facets, setFacets] = useState<FacetsState>({});

  const replaceFacets = useCallback((next?: FacetsState | null) => {
    setFacets(next ?? {});
  }, []);

  const mergeFacets = useCallback((next?: FacetsState | null) => {
    setFacets((prev) => ({ ...prev, ...(next ?? {}) }));
  }, []);

  const resetFacets = useCallback(() => {
    setFacets({});
  }, []);

  return (
    <FacetsContext.Provider value={{ facets, replaceFacets, mergeFacets, resetFacets }}>
      {children}
    </FacetsContext.Provider>
  );
}

export function useFacets() {
  const ctx = useContext(FacetsContext);
  if (!ctx) throw new Error('useFacets must be used inside a FacetsProvider');
  return ctx;
}