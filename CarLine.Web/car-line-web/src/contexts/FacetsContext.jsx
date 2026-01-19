import React, {
  createContext,
  useCallback,
  useContext,
  useState,
} from 'react';

const FacetsContext = createContext(null);

export function FacetsProvider({ children }) {
  const [facets, setFacets] = useState({});

  const replaceFacets = useCallback((next) => setFacets(next || {}), []);

  const mergeFacets = useCallback((next) => {
    setFacets((prev) => ({ ...prev, ...(next || {}) }));
  }, []);

  const resetFacets = useCallback(() => setFacets({}), []);

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
