export interface CarListing {
  id: string | number;
  classification_date?: string;
  condition?: string;
  first_seen?: string;
  fuel?: string;
  gearbox?: string;
  image_url?: string;
  last_seen?: string;
  manufacturer?: string;
  meter?: number;
  meterFrom?: number;
  meterTo?: number;
  model?: string;
  odometer?: number;
  paint_color?: string;
  posting_date?: string;
  predicted_price?: number;
  price: number;
  price_classification?: 'low' | 'normal' | 'high' | string;
  price_difference_percent?: number;
  region?: string;
  source?: string;
  sourceUrl?: string;
  status?: string;
  transmission?: string;
  type?: string;
  url?: string;
  vin?: string;
  year?: number;
}

export interface CarsSearchResponse {
  cars: CarListing[];
  total: number;
  facets?: Record<string, unknown>;
}
