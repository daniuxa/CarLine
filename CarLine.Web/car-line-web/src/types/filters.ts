export interface Filters {
  manufacturer?: string;
  model?: string;
  yearFrom?: string;
  yearTo?: string;
  priceFrom?: string;
  priceTo?: string;
  fuel?: string;
  transmission?: string;
  condition?: string;
  type?: string;
  region?: string;
  odometerFrom?: string;
  odometerTo?: string;
  facets?: boolean;
  page?: number;
  pageSize?: number;
}
