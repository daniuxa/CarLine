import React, {
  useEffect,
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
  Grid,
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';

import { useFacets } from '../contexts/FacetsContext';
import { useSubscription } from '../hooks/useSubscription';

function SubscriptionModal({ open, onClose }) {
  const [formData, setFormData] = useState({
    email: "",
    region: "",
    manufacturer: "",
    model: "",
    yearFrom: "",
    yearTo: "",
    fuel: "",
    transmission: "",
    condition: "",
    type: "",
    odometerFrom: "",
    odometerTo: "",
  });

  const { facets } = useFacets();
  const { submitSubscription, isLoading, isError, error, isSuccess, reset } = useSubscription();

  const [modelOptions, setModelOptions] = useState(null);

  // Validate that all required fields are filled
  const isFormValid = !!(
    formData.email &&
    formData.manufacturer &&
    formData.model &&
    formData.yearFrom &&
    formData.yearTo &&
    formData.odometerFrom &&
    formData.odometerTo &&
    formData.condition &&
    formData.fuel &&
    formData.transmission &&
    formData.type &&
    formData.region
  );

  useEffect(() => {
    // Clear model when manufacturer changes
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
      email: "",
      region: "",
      manufacturer: "",
      model: "",
      yearFrom: "",
      yearTo: "",
      fuel: "",
      transmission: "",
      condition: "",
      type: "",
      odometerFrom: "",
      odometerTo: "",
    });
    reset();
  };

  const handleClose = () => {
    console.log("Closing SubscriptionModal");
    handleReset();
    if (onClose) onClose();
  };

  useEffect(() => {
    // Clear form when the dialog is opened or closed
    if (!open) {
      handleReset();
      return;
    }
    // When opened, also reset to ensure no previous values remain
    handleReset();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const handleChange = (field, value) => {
    setFormData({ ...formData, [field]: value });
  };

  const handleSubmit = async () => {
    if (!isFormValid) {
      return;
    }

    const subscriptionData = {
      email: formData.email,
      manufacturer: formData.manufacturer,
      model: formData.model,
      yearFrom: parseInt(formData.yearFrom, 10),
      yearTo: parseInt(formData.yearTo, 10),
      odometerFrom: parseInt(formData.odometerFrom, 10),
      odometerTo: parseInt(formData.odometerTo, 10),
      condition: formData.condition,
      fuel: formData.fuel,
      transmission: formData.transmission,
      type: formData.type,
      region: formData.region,
    };

    try {
      await submitSubscription(subscriptionData);
      // Auto-close after 2 seconds on success
      setTimeout(() => {
        handleClose();
      }, 2000);
    } catch (err) {
      console.error("Subscription failed:", err);
    }
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>
        <Typography variant="h5">Subscribe to Car Updates</Typography>
        <Typography variant="body2" color="text.secondary">
          Get notified when new cars matching your criteria are available
        </Typography>
      </DialogTitle>
      <DialogContent>
        <Box sx={{ pt: 2 }}>
          {isSuccess ? (
            <Alert severity="success">
              Subscription successful! You'll receive email notifications for
              matching cars.
            </Alert>
          ) : (
            <>
              {isError && (
                <Alert severity="error" sx={{ mb: 2 }}>
                  {error || "Failed to create subscription. Please try again."}
                </Alert>
              )}
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
                <Grid item xs={12} sx={{ gridColumn: "1 / -1" }}>
                  <TextField
                    required
                    fullWidth
                    label="Email Address"
                    type="email"
                    value={formData.email}
                    onChange={(e) => handleChange("email", e.target.value)}
                    placeholder="your.email@example.com"
                    size="small"
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Manufacturer"
                    value={formData.manufacturer}
                    onChange={(e) => handleChange("manufacturer", e.target.value)}
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

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Model"
                    value={formData.model}
                    onChange={(e) => handleChange("model", e.target.value)}
                    fullWidth
                    size="small"
                    select={!!modelOptions}
                    disabled={!formData.manufacturer}
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

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Region"
                    value={formData.region}
                    onChange={(e) => handleChange("region", e.target.value)}
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

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Year From"
                    type="number"
                    value={formData.yearFrom}
                    onChange={(e) => handleChange("yearFrom", e.target.value)}
                    placeholder="2000"
                    fullWidth
                    size="small"
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Year To"
                    type="number"
                    value={formData.yearTo}
                    onChange={(e) => handleChange("yearTo", e.target.value)}
                    placeholder="2024"
                    fullWidth
                    size="small"
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Odometer From"
                    type="number"
                    value={formData.odometerFrom}
                    onChange={(e) => handleChange("odometerFrom", e.target.value)}
                    placeholder="0"
                    fullWidth
                    size="small"
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Odometer To"
                    type="number"
                    value={formData.odometerTo}
                    onChange={(e) => handleChange("odometerTo", e.target.value)}
                    placeholder="200000"
                    fullWidth
                    size="small"
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Fuel Type"
                    value={formData.fuel}
                    onChange={(e) => handleChange("fuel", e.target.value)}
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

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Transmission"
                    value={formData.transmission}
                    onChange={(e) => handleChange("transmission", e.target.value)}
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

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Condition"
                    value={formData.condition}
                    onChange={(e) => handleChange("condition", e.target.value)}
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

                <Grid item xs={12} sm={6} md={4}>
                  <TextField
                    required
                    label="Vehicle Type"
                    value={formData.type}
                    onChange={(e) => handleChange("type", e.target.value)}
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
              </Grid>
            </>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} variant="outlined">
          {isSuccess ? "Close" : "Cancel"}
        </Button>
        {!isSuccess && (
          <Button
            onClick={handleSubmit}
            variant="contained"
            color="primary"
            disabled={!isFormValid || isLoading}
          >
            {isLoading ? "Subscribing..." : "Subscribe"}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}

export default SubscriptionModal;
