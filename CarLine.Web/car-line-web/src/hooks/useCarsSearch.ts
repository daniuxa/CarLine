import { CarsSearchResponse } from '@/types/cars';
import { useQuery } from '@tanstack/react-query';

import { searchCars } from '../services/cars-search-service';

export function useCarsSearch(filters = {}, page = 1, pageSize = 20) {
  const effectiveFilters = { ...filters, page, pageSize };
  const queryKey = ['cars', effectiveFilters];

  const query = useQuery<CarsSearchResponse, Error>({
    queryKey,
    queryFn: async () => {
      const fetchedData = await searchCars(effectiveFilters);
      return fetchedData;
    },
    staleTime: 1000 * 60 * 2, // 2 minutes
  });

  return query;
}

