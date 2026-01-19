import React, {
  useEffect,
  useState,
} from 'react';

import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Grid,
  MenuItem,
  Paper,
  TextField,
  Typography,
} from '@mui/material';

import { useFacets } from '../contexts/FacetsContext';
import { usePriceEstimator } from '../hooks/usePriceEstimator';

function PriceEstimator({ open, onClose }) {
  const [formData, setFormData] = useState({
    manufacturer: "",
    model: "",
    year: "",
    odometer: "",
    condition: "",
    fuel: "",
    transmission: "",
    type: "",
    region: "",
  });
  const [error, setError] = useState(null);
  const [requestBody, setRequestBody] = useState(null);

  const { facets } = useFacets();

  const [modelOptions, setModelOptions] = useState(null);

  const isFormValid = !!(
    formData.manufacturer &&
    formData.model &&
    formData.year &&
    formData.odometer
  );

  useEffect(() => {
    // clear model when manufacturer changes
    setFormData((prev) => ({ ...prev, model: "" }));

    const manu = formData.manufacturer;
    if (!manu || !facets?.manufacturer_models) {
      setModelOptions(null);
      return;
    }

    const modelsForManu = facets.manufacturer_models[manu];
    if (modelsForManu && typeof modelsForManu === "object") {
      setModelOptions(modelsForManu);
    } else {
      setModelOptions(null);
    }
     
  }, [formData.manufacturer, facets]);

  const handleReset = () => {
    setFormData({
      manufacturer: "",
      model: "",
      year: "",
      odometer: "",
      condition: "",
      fuel: "",
      transmission: "",
      type: "",
      region: "",
    });
    setError(null);
    setRequestBody(null);
  };

  const handleClose = () => {
    console.log("Closing PriceEstimator");
    handleReset();
    if (onClose) onClose();
  };

  useEffect(() => {
    // clear form when the dialog is opened or closed
    if (!open) {
      handleReset();
      return;
    }
    // when opened, also reset to ensure no previous values remain
    handleReset();
  }, [open]);

  const { data, isLoading, isError } = usePriceEstimator(requestBody);
  const estimatedPrice = data?.estimatedPrice ?? null;

  const handleSubmit = () => {
    setError(null);

    const body = {
      manufacturer: formData.manufacturer,
      model: formData.model,
      year: parseFloat(formData.year),
      odometer: parseFloat(formData.odometer) || 0,
      condition: formData.condition || "",
      fuel: formData.fuel || "",
      transmission: formData.transmission || "",
      type: formData.type || "",
      region: formData.region || "",
    };

    setRequestBody(body);
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Typography variant="h6">Car Price Estimator</Typography>
      </DialogTitle>
      <DialogContent>
        <Box sx={{ pt: 2 }}>
          <Grid
            container
            spacing={2}
            sx={{
              display: "grid",
              gap: 3,
              gridTemplateColumns: {
                xs: "1fr",
                sm: "repeat(3, minmax(0, 1fr))",
                md: "repeat(3, minmax(0, 1fr))",
              },
              width: "100%",
            }}
          >
            <Grid item xs={12} sm={6}>
              <TextField
                required
                label="Manufacturer"
                value={formData.manufacturer}
                onChange={(e) =>
                  setFormData({ ...formData, manufacturer: e.target.value })
                }
                fullWidth
                size="small"
                select={!!facets?.manufacturer}
                SelectProps={{ displayEmpty: true }}
                InputLabelProps={{ shrink: true }}
              >
                <MenuItem value="" disabled>
                  Select manufacturer
                </MenuItem>
                {facets?.manufacturer
                  ? Object.keys(facets.manufacturer).map((m) => (
                      <MenuItem key={m} value={m}>
                        {m}
                      </MenuItem>
                    ))
                  : null}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={6}>
              <TextField
                required
                label="Model"
                value={formData.model}
                onChange={(e) =>
                  setFormData({ ...formData, model: e.target.value })
                }
                fullWidth
                size="small"
                select={!!modelOptions}
                SelectProps={{ displayEmpty: true }}
                InputLabelProps={{ shrink: true }}
              >
                <MenuItem value="" disabled>
                  Select model
                </MenuItem>
                {modelOptions
                  ? Object.keys(modelOptions).map((m) => (
                      <MenuItem key={m} value={m}>
                        {m}
                      </MenuItem>
                    ))
                  : null}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={6}>
              <TextField
                required
                label="Year"
                type="number"
                value={formData.year}
                onChange={(e) =>
                  setFormData({ ...formData, year: e.target.value })
                }
                fullWidth
                size="small"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                required
                label="Odometer"
                type="number"
                value={formData.odometer}
                onChange={(e) =>
                  setFormData({ ...formData, odometer: e.target.value })
                }
                fullWidth
                size="small"
              />
            </Grid>

            <Grid item xs={12} sm={6}>
              <TextField
                label="Condition"
                value={formData.condition}
                onChange={(e) =>
                  setFormData({ ...formData, condition: e.target.value })
                }
                fullWidth
                size="small"
                select={!!facets?.condition}
                SelectProps={{ displayEmpty: true }}
                InputLabelProps={{ shrink: true }}
              >
                <MenuItem value="" disabled>
                  Select condition
                </MenuItem>
                {facets?.condition
                  ? Object.keys(facets.condition).map((k) => (
                      <MenuItem key={k} value={k}>
                        {k}
                      </MenuItem>
                    ))
                  : null}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                label="Fuel"
                value={formData.fuel}
                onChange={(e) =>
                  setFormData({ ...formData, fuel: e.target.value })
                }
                fullWidth
                size="small"
                select={!!facets?.fuel}
                SelectProps={{ displayEmpty: true }}
                InputLabelProps={{ shrink: true }}
              >
                <MenuItem value="" disabled>
                  Select fuel
                </MenuItem>
                {facets?.fuel
                  ? Object.keys(facets.fuel).map((k) => (
                      <MenuItem key={k} value={k}>
                        {k}
                      </MenuItem>
                    ))
                  : null}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                label="Transmission"
                value={formData.transmission}
                onChange={(e) =>
                  setFormData({ ...formData, transmission: e.target.value })
                }
                fullWidth
                size="small"
                select={!!facets?.transmission}
                SelectProps={{ displayEmpty: true }}
                InputLabelProps={{ shrink: true }}
              >
                <MenuItem value="" disabled>
                  Select transmission
                </MenuItem>
                {facets?.transmission
                  ? Object.keys(facets.transmission).map((k) => (
                      <MenuItem key={k} value={k}>
                        {k}
                      </MenuItem>
                    ))
                  : null}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                label="Type"
                value={formData.type}
                onChange={(e) =>
                  setFormData({ ...formData, type: e.target.value })
                }
                fullWidth
                size="small"
                select={!!facets?.type}
                SelectProps={{ displayEmpty: true }}
                InputLabelProps={{ shrink: true }}
              >
                <MenuItem value="" disabled>
                  Select type
                </MenuItem>
                {facets?.type
                  ? Object.keys(facets.type).map((k) => (
                      <MenuItem key={k} value={k}>
                        {k}
                      </MenuItem>
                    ))
                  : null}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                label="Region"
                value={formData.region}
                onChange={(e) =>
                  setFormData({ ...formData, region: e.target.value })
                }
                fullWidth
                size="small"
                select={!!facets?.region}
                SelectProps={{ displayEmpty: true }}
                InputLabelProps={{ shrink: true }}
              >
                <MenuItem value="" disabled>
                  Select region
                </MenuItem>
                {facets?.region
                  ? Object.keys(facets.region).map((k) => (
                      <MenuItem key={k} value={k}>
                        {k}
                      </MenuItem>
                    ))
                  : null}
              </TextField>
            </Grid>
          </Grid>

          {estimatedPrice !== null && (
            <>
              <Divider sx={{ my: 2 }} />
              <Paper elevation={2} sx={{ p: 3, textAlign: "center" }}>
                <Typography variant="subtitle1">Estimated Price</Typography>
                <Typography variant="h4" sx={{ fontWeight: "bold" }}>
                  ${estimatedPrice}
                </Typography>
              </Paper>
            </>
          )}

          {error && (
            <Typography color="error" sx={{ mt: 2 }}>
              {error}
            </Typography>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} variant="outlined">
          Close
        </Button>
        {estimatedPrice !== null && (
          <Button onClick={handleReset} variant="outlined">
            Reset
          </Button>
        )}
        <Button
          onClick={handleSubmit}
          variant="contained"
          disabled={!isFormValid || isLoading}
        >
          {isLoading ? "Calculating..." : "Calculate Price"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default PriceEstimator;
