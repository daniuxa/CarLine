export type FacetValueMap = Record<string, unknown>;

export interface FacetsState {
  manufacturer?: FacetValueMap;
  manufacturer_models?: Record<string, FacetValueMap>;
  region?: FacetValueMap;
  fuel?: FacetValueMap;
  transmission?: FacetValueMap;
  condition?: FacetValueMap;
  type?: FacetValueMap;
}