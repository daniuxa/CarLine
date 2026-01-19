import React from 'react';

import FiltersForm from './FiltersForm';

function CarFilters({ onFilter }) {
  return (
    <FiltersForm 
      onFilter={onFilter} 
      onReset={() => onFilter({})} 
    />
  );
}

export default CarFilters;
