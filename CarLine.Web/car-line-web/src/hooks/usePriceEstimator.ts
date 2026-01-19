import { useQuery } from '@tanstack/react-query';

import { estimatePrice } from '../services/price-estimator-service';

export function usePriceEstimator(requestBody, options = {}) {
  const queryKey = ['priceEstimate', requestBody || {}];

  const query = useQuery({
    queryKey,
    queryFn: () => estimatePrice(requestBody),
    enabled: !!requestBody,
    ...options,
  });

  return query;
}
