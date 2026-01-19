import CalendarTodayIcon from '@mui/icons-material/CalendarToday';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import DirectionsCarIcon from '@mui/icons-material/DirectionsCar';
import LocalGasStationIcon from '@mui/icons-material/LocalGasStation';
import PlaceIcon from '@mui/icons-material/Place';
import SettingsIcon from '@mui/icons-material/Settings';
import SpeedIcon from '@mui/icons-material/Speed';
import {
  Box,
  Card,
  CardContent,
  Chip,
  Grid,
  Pagination,
  Stack,
  Typography,
} from '@mui/material';

function getStatusColor(status) {
  const normalized = status?.toLowerCase?.();
  if (normalized === 'low') return 'success';
  if (normalized === 'normal') return 'warning';
  if (normalized === 'high') return 'error';
  return 'default';
}

function CarList({ cars, total = 0, page = 1, pageSize = 9, onPageChange }) {
  if (!cars || cars.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 8 }}>
        <DirectionsCarIcon sx={{ fontSize: 80, color: '#ccc', mb: 2 }} />
        <Typography variant="h6" color="text.secondary">
          No cars found matching your criteria
        </Typography>
      </Box>
    );
  }
  const pageCount = Math.max(1, Math.ceil(total / pageSize));

  const handlePageChange = (event, value) => {
    if (onPageChange) onPageChange(value);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <Box sx={{ mt: 4 }}>
      <Typography variant="h5" gutterBottom sx={{ mb: 3, fontWeight: 'bold' }}>
        Available Cars ({total})
      </Typography>
      <Box sx={{ display: 'grid', gap: 3, gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, minmax(0, 1fr))', md: 'repeat(3, minmax(0, 1fr))' }, width: '100%' }}>
        {cars.map((car) => (
          <Box key={car.id} sx={{ minWidth: 0 }}>
            <Card
              sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                cursor: 'pointer',
                transition: 'transform 0.2s, box-shadow 0.2s',
                '&:hover': {
                  transform: 'translateY(-4px)',
                  boxShadow: 6
                }
              }}
              onClick={() => window.open(car.url, '_blank')}
            >
              <CardContent sx={{ flexGrow: 1 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                  <Typography variant="h6" component="div" sx={{ fontWeight: 'bold' }}>
                    {car.manufacturer} {car.model}
                  </Typography>
                  <Chip
                    label={car.price_classification}
                    color={getStatusColor(car.price_classification)}
                    size="small"
                    sx={{ fontWeight: 'bold' }}
                  />
                </Box>

                <Box sx={{ mb: 2 }}>
                  <Grid container spacing={1} sx={{ display: 'grid', gap: 3, gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))', md: 'repeat(2, minmax(0, 1fr))' }, width: '100%' }}>
                    <Grid item xs={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <CalendarTodayIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Year: {car.year}</Typography>
                      </Box>
                    </Grid>

                    <Grid item xs={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <PlaceIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Region: {car.region || '—'}</Typography>
                      </Box>
                    </Grid>

                    <Grid item xs={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <SpeedIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Odometer: {(() => {
                          const v = car.odometer || car.meter || car.meterFrom;
                          return v ? Number(v).toLocaleString() : '—';
                        })()}</Typography>
                      </Box>
                    </Grid>

                    <Grid item xs={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <LocalGasStationIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Fuel: {car.fuel || '—'}</Typography>
                      </Box>
                    </Grid>

                    <Grid item xs={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <CheckCircleOutlineIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Condition: {car.condition || '—'}</Typography>
                      </Box>
                    </Grid>

                    <Grid item xs={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <DirectionsCarIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Type: {car.type || '—'}</Typography>
                      </Box>
                    </Grid>

                    <Grid item xs={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <SettingsIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Gearbox: {car.transmission || '—'}</Typography>
                      </Box>
                    </Grid>

                    <Grid item xs={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <CalendarTodayIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Posted: {(() => {
                          const posted = car.posting_date;
                          try {
                            return posted ? (isNaN(Date.parse(posted)) ? posted : new Date(posted).toLocaleDateString()) : '—';
                          } catch (e) { return posted || '—'; }
                        })()}</Typography>
                      </Box>
                    </Grid>
                  </Grid>
                </Box>

                <Box sx={{ 
                  pt: 2, 
                  borderTop: '1px solid #e0e0e0',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center'
                }}>
                  <Typography variant="h5" color="primary" sx={{ fontWeight: 'bold' }}>
                    ${car.price.toLocaleString()}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {car.source}
                  </Typography>
                </Box>
              </CardContent>
            </Card>
          </Box>
        ))}
      </Box>

      <Stack alignItems="center" sx={{ mt: 4 }}>
        <Pagination count={pageCount} page={page} onChange={handlePageChange} color="primary" />
      </Stack>
    </Box>
  );
}

export default CarList;

