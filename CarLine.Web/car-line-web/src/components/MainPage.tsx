import { useState } from 'react';

import NotificationsIcon from '@mui/icons-material/Notifications';
import {
  AppBar,
  Box,
  Button,
  Container,
  Paper,
  Stack,
  Toolbar,
  Typography,
} from '@mui/material';

import CarFilters from './CarFilters';
import CarsQueryRenderer from './CarsQueryRender';
import PriceEstimator from './PriceEstimator';
import SubscriptionModal from './SubscriptionModal';
import SubscriptionsByEmailModal from './SubscriptionsByEmailModal';

export function MainPage() {
  const [subscriptionOpen, setSubscriptionOpen] = useState(false);
  const [subscriptionsByEmailOpen, setSubscriptionsByEmailOpen] = useState(false);
  const [estimatorOpen, setEstimatorOpen] = useState(false);

  const [filters, setFilters] = useState({ facets: true });
  const [page, setPage] = useState(1);
  const pageSize = 30;

  return (
    <Box sx={{ bgcolor: "background.default", minHeight: "100vh" }}>
      <AppBar position="static" elevation={3}>
        <Toolbar>
          <Typography variant="h5" component="div" sx={{ flexGrow: 1 }}>
            🚗 Car Line Web
          </Typography>
          <Stack direction="row" spacing={2} alignItems="center">
            <Button
              color="inherit"
              startIcon={<NotificationsIcon />}
              onClick={() => setSubscriptionOpen(true)}
              sx={{ textTransform: "none", fontSize: "1rem" }}
            >
              Subscribe on Updates
            </Button>
            <Button
              color="inherit"
              onClick={() => setSubscriptionsByEmailOpen(true)}
              sx={{ textTransform: "none", fontSize: "1rem" }}
            >
              My Subscriptions
            </Button>
            <Button
              variant="contained"
              color="secondary"
              onClick={() => setEstimatorOpen(true)}
              sx={{ textTransform: "none", fontSize: "0.95rem" }}
            >
              Get Estimated Price
            </Button>
          </Stack>
        </Toolbar>
      </AppBar>

      <Container maxWidth="xl">
        <Paper elevation={2} sx={{ p: 3, mt: 4, mb: 4, borderRadius: 3 }}>
          <CarFilters onFilter={(f) => setFilters(f)} />

          <Box sx={{ mt: 3 }}>
            <CarsQueryRenderer
              filters={filters}
              page={page}
              pageSize={pageSize}
              onPageChange={(p) => setPage(p)}
            />
          </Box>
        </Paper>
      </Container>

      <SubscriptionModal
        open={subscriptionOpen}
        onClose={() => setSubscriptionOpen(false)}
      />

      <SubscriptionsByEmailModal
        open={subscriptionsByEmailOpen}
        onClose={() => setSubscriptionsByEmailOpen(false)}
      />

      <PriceEstimator
        open={estimatorOpen}
        onClose={() => setEstimatorOpen(false)}
      />
    </Box>
  );
}
