import React, {
  useEffect,
  useMemo,
  useState,
} from 'react';

import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  TextField,
  Typography,
} from '@mui/material';

import {
  deleteSubscription,
  getUserSubscriptions,
} from '../services/subscription-service';

function formatSubscriptionLabel(s) {
  const make = s?.manufacturer || '';
  const model = s?.model || '';
  const region = s?.region || '';
  const years = (s?.yearFrom != null && s?.yearTo != null) ? `${s.yearFrom}-${s.yearTo}` : '';
  return [
    [make, model].filter(Boolean).join(' '),
    region,
    years,
  ].filter(Boolean).join(' · ');
}

function SubscriptionsByEmailModal({ open, onClose }) {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [subscriptions, setSubscriptions] = useState([]);
  const [deletingId, setDeletingId] = useState(null);

  const hasResults = subscriptions.length > 0;

  const titleText = useMemo(() => {
    if (!email) return 'My subscriptions';
    return `My subscriptions for ${email}`;
  }, [email]);

  useEffect(() => {
    if (!open) {
      setEmail('');
      setLoading(false);
      setError(null);
      setSubscriptions([]);
    }
  }, [open]);

  const load = async () => {
    const trimmed = (email || '').trim();
    if (!trimmed) return;

    setLoading(true);
    setError(null);

    try {
      const list = await getUserSubscriptions(trimmed);
      setSubscriptions(Array.isArray(list) ? list : []);
    } catch (e) {
      setError(e?.message || 'Failed to load subscriptions');
      setSubscriptions([]);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id) => {
    if (!id) return;

    setDeletingId(id);
    setError(null);
    try {
      await deleteSubscription(id);
      await load();
    } catch (e) {
      setError(e?.message || 'Failed to delete subscription');
    } finally {
      setDeletingId(null);
    }
  };

  const handleClose = () => {
    if (onClose) onClose();
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Typography variant="h6">{titleText}</Typography>
        <Typography variant="body2" color="text.secondary">
          Enter your email to fetch your subscriptions
        </Typography>
      </DialogTitle>

      <DialogContent>
        <Box sx={{ pt: 2 }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} alignItems={{ xs: 'stretch', sm: 'flex-start' }}>
            <TextField
              fullWidth
              label="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') load();
              }}
              size="small"
              placeholder="your.email@example.com"
              InputLabelProps={{ shrink: true }}
            />
            <Button
              variant="contained"
              onClick={load}
              disabled={!email || loading}
              sx={{ whiteSpace: 'nowrap' }}
            >
              {loading ? 'Loading...' : 'Load'}
            </Button>
          </Stack>

          {error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {error}
            </Alert>
          )}

          <Divider sx={{ my: 2 }} />

          {hasResults ? (
            <Box sx={{ display: 'grid', gap: 1 }}>
              {subscriptions.map((s) => (
                <Box
                  key={s.id}
                  sx={{
                    p: 1.5,
                    border: '1px solid',
                    borderColor: 'divider',
                    borderRadius: 1,
                  }}
                >
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2 }}>
                    <Typography variant="body1" sx={{ fontWeight: 600, minWidth: 0 }} noWrap>
                      {formatSubscriptionLabel(s) || 'Subscription'}
                    </Typography>

                    <Button
                      variant="outlined"
                      color="error"
                      size="small"
                      onClick={() => handleDelete(s.id)}
                      disabled={!s?.id || deletingId === s.id}
                      sx={{ whiteSpace: 'nowrap' }}
                    >
                      {deletingId === s.id ? 'Deleting...' : 'Delete'}
                    </Button>
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    {s?.fuel ? `Fuel: ${s.fuel}` : ''}
                    {s?.transmission ? `${s?.fuel ? ' · ' : ''}Transmission: ${s.transmission}` : ''}
                    {s?.condition ? `${(s?.fuel || s?.transmission) ? ' · ' : ''}Condition: ${s.condition}` : ''}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {s?.createdAtUtc ? `Created: ${new Date(s.createdAtUtc).toLocaleString()}` : ''}
                  </Typography>
                </Box>
              ))}
            </Box>
          ) : (
            <Typography variant="body2" color="text.secondary">
              {email ? 'No subscriptions found (or not loaded yet).' : 'Enter an email and click Load.'}
            </Typography>
          )}
        </Box>
      </DialogContent>

      <DialogActions>
        <Button onClick={handleClose} variant="outlined">Close</Button>
      </DialogActions>
    </Dialog>
  );
}

export default SubscriptionsByEmailModal;
