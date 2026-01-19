import Typography from '@mui/material/Typography';

import { useFacets } from '../contexts/FacetsContext';
import { useCarsSearch } from '../hooks/useCarsSearch';
import CarList from './CarList';

function CarsQueryRenderer({ filters, page, pageSize, onPageChange }) {
  const { replaceFacets } = useFacets();
  const { data, isLoading, isError, error } = useCarsSearch(
    filters,
    page,
    pageSize
  );

  if (isError)
    return (
      <Typography color="error">{error?.message || String(error)}</Typography>
    );
  if (isLoading) return <Typography>Loading cars...</Typography>;

  const total = data?.total ?? 0;
  const cars = data?.cars ?? [];

  if (data?.facets) {
    replaceFacets(data.facets);
  }

  return (
    <CarList
      cars={cars}
      total={total}
      page={page}
      pageSize={pageSize}
      onPageChange={(p) => onPageChange && onPageChange(p)}
    />
  );
}

export default CarsQueryRenderer;