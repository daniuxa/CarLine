import {
  CarListing,
  CarsSearchResponse,
} from '@/types/cars';
import { Filters } from '@/types/filters';

export async function searchCars(
  filters: Filters = {}
): Promise<CarsSearchResponse> {
  const params = new URLSearchParams();
  if (filters.manufacturer) params.append('manufacturer', filters.manufacturer);
  if (filters.model) params.append('q', filters.model);
  if (filters.yearFrom) params.append('minYear', filters.yearFrom);
  if (filters.yearTo) params.append('maxYear', filters.yearTo);
  if (filters.fuel) params.append('fuel', filters.fuel);
  if (filters.transmission) params.append('transmission', filters.transmission);
  if (filters.facets) params.append('facets', 'true');
  if (filters.page) params.append('page', String(filters.page));
  if (filters.pageSize) params.append('pageSize', String(filters.pageSize));
  if (filters.condition) params.append('condition', filters.condition);
  if (filters.odometerFrom) params.append('odometerFrom', filters.odometerFrom);
  if (filters.odometerTo) params.append('odometerTo', filters.odometerTo);
  if (filters.priceFrom) params.append('priceFrom', filters.priceFrom);
  if (filters.priceTo) params.append('priceTo', filters.priceTo);
  if (filters.region) params.append('region', filters.region);
  if (filters.type) params.append('type', filters.type);

  const qs = params.toString();
  const url = `/api/CarsSearch/search${qs ? `?${qs}` : ''}`;

  const res = await fetch(url);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Failed to fetch cars: ${res.status} ${text}`);
  }

  const json = (await res.json()) as CarsSearchResponse;
  const normalizedCars = (json.cars ?? []).map(normalizeCarStrings);

  return {
    ...json,
    cars: normalizedCars,
  };
}

function normalizeCarStrings(car: CarListing): CarListing {
  return {
    ...car,
    manufacturer: capitalizeFirst(car.manufacturer),
    model: capitalizeFirst(car.model),
    region: capitalizeFirst(car.region),
    fuel: capitalizeFirst(car.fuel),
    condition: capitalizeFirst(car.condition),
    type: capitalizeFirst(car.type),
    gearbox: capitalizeFirst(car.gearbox),
    price_classification: capitalizeFirst(car.price_classification),
  };
}

function capitalizeFirst(value?: string): string | undefined {
  if (!value) return value;
  return value.charAt(0).toUpperCase() + value.slice(1);
}

