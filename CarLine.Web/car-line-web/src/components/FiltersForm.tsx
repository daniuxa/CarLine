import {
  useEffect,
  useState,
} from 'react';

import { Filters } from '@/types/filters';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Collapse from '@mui/material/Collapse';
import Grid from '@mui/material/Grid';
import IconButton from '@mui/material/IconButton';
import MenuItem from '@mui/material/MenuItem';
import Paper from '@mui/material/Paper';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { useFacets } from '../contexts/FacetsContext';

export default function FiltersForm(props) {
  const { onFilter, onReset, initial = {}, compact = false } = props;
  const initialFilters: Filters = initial || {};
  const { facets } = useFacets();
  const [expanded, setExpanded] = useState(false);
  const [manufacturer, setManufacturer] = useState(initialFilters.manufacturer || '');
  const [model, setModel] = useState(initialFilters.model || '');
  const [yearFrom, setYearFrom] = useState(initialFilters.yearFrom || '');
  const [yearTo, setYearTo] = useState(initialFilters.yearTo || '');
  const [priceFrom, setPriceFrom] = useState(initialFilters.priceFrom || '');
  const [priceTo, setPriceTo] = useState(initialFilters.priceTo || '');
  const [fuel, setFuel] = useState(initialFilters.fuel || '');
  const [transmission, setTransmission] = useState(initialFilters.transmission || '');
  const [condition, setCondition] = useState(initialFilters.condition || '');
  const [type, setType] = useState(initialFilters.type || '');
  const [region, setRegion] = useState(initialFilters.region || '');
  const [odometerFrom, setOdometerFrom] = useState(initialFilters.odometerFrom || '');
  const [odometerTo, setOdometerTo] = useState(initialFilters.odometerTo || '');

  const [modelOptions, setModelOptions] = useState(null);

  useEffect(() => {
    setModel('');

    if (!manufacturer || !facets?.manufacturer_models) {
      setModelOptions(null);
      return;
    }

    const modelsForManufacturer = facets.manufacturer_models[manufacturer];
    if (modelsForManufacturer) {
      setModelOptions(modelsForManufacturer);
    } else {
      setModelOptions(null);
    }
  }, [manufacturer, facets]);

  const getFilters = () => ({
    manufacturer,
    model,
    yearFrom,
    yearTo,
    priceFrom,
    priceTo,
    fuel,
    transmission,
    condition,
    type,
    region,
    odometerFrom,
    odometerTo,
  });

  const handleSubmit = (e) => {
    onFilter(getFilters());
  };

  const handleReset = () => {
    setManufacturer('');
    setModel('');
    setYearFrom('');
    setYearTo('');
    setPriceFrom('');
    setPriceTo('');
    setFuel('');
    setTransmission('');
    setCondition('');
    setType('');
    setRegion('');
    setOdometerFrom('');
    setOdometerTo('');
    if (onReset) onReset();
    else onFilter({});
  };

  const content = (
    <Box component="form" onSubmit={handleSubmit}>
      <Grid container spacing={2} sx={{
              display: "grid",
              gap: 3,
              gridTemplateColumns: {
                xs: "1fr",
                sm: "repeat(3, minmax(0, 1fr))",
                md: "repeat(3, minmax(0, 1fr))",
              },
              width: "100%",
            }}>
        <Grid item xs={12} sm={6} md={4}>
          <TextField
            select
            fullWidth
            label="Region"
            value={region}
            onChange={(e) => { setRegion(e.target.value); }}
            size="small"
            SelectProps={{ displayEmpty: true }}
            InputLabelProps={{ shrink: true }}
          >
            <MenuItem value="">Any</MenuItem>
            {facets?.region ? Object.keys(facets.region).map((r) => (
              <MenuItem key={r} value={r}>{r}</MenuItem>
            )) : null}
          </TextField>
        </Grid>

        <Grid item xs={12} sm={6} md={4}>
          <TextField
            select
            fullWidth
            label="Manufacturer"
            value={manufacturer}
            onChange={(e) => { setManufacturer(e.target.value); }}
            size="small"
            SelectProps={{ displayEmpty: true }}
            InputLabelProps={{ shrink: true }}
          >
            <MenuItem value="">Any</MenuItem>
            {facets?.manufacturer ? Object.keys(facets.manufacturer).map((m) => (
              <MenuItem key={m} value={m}>{m}</MenuItem>
            )) : null}
          </TextField>
        </Grid>

        <Grid item xs={12} sm={6} md={4}>
          <TextField
            select
            fullWidth
            label="Model"
            value={model}
            onChange={(e) => { setModel(e.target.value); }}
            size="small"
            disabled={!manufacturer}
            SelectProps={{ displayEmpty: true }}
            InputLabelProps={{ shrink: true }}
          >
            <MenuItem value="">Any</MenuItem>
            {modelOptions ? Object.keys(modelOptions).map((m) => (
              <MenuItem key={m} value={m}>{m}</MenuItem>
            )) : null}
          </TextField>
        </Grid>

        <Grid item xs={6} sm={3} md={2}>
          <TextField
            fullWidth
            label="Year From"
            type="number"
            value={yearFrom}
            onChange={(e) => { setYearFrom(e.target.value); }}
            size="small"
            placeholder="2000"
          />
        </Grid>

        <Grid item xs={6} sm={3} md={2}>
          <TextField
            fullWidth
            label="Year To"
            type="number"
            value={yearTo}
            onChange={(e) => { setYearTo(e.target.value); }}
            size="small"
            placeholder="2024"
          />
        </Grid>

        <Grid item xs={6} sm={3} md={2}>
          <TextField
            fullWidth
            label="Price From"
            type="number"
            value={priceFrom}
            onChange={(e) => { setPriceFrom(e.target.value); }}
            size="small"
            placeholder="5000"
          />
        </Grid>

        <Grid item xs={6} sm={3} md={2}>
          <TextField
            fullWidth
            label="Price To"
            type="number"
            value={priceTo}
            onChange={(e) => { setPriceTo(e.target.value); }}
            size="small"
            placeholder="50000"
          />
        </Grid>

        <Grid item xs={6} sm={4} md={2}>
          <TextField
            fullWidth
            label="Odometer From"
            type="number"
            value={odometerFrom}
            onChange={(e) => { setOdometerFrom(e.target.value); }}
            size="small"
            placeholder="0"
          />
        </Grid>

        <Grid item xs={6} sm={4} md={2}>
          <TextField
            fullWidth
            label="Odometer To"
            type="number"
            value={odometerTo}
            onChange={(e) => { setOdometerTo(e.target.value); }}
            size="small"
            placeholder="200000"
          />
        </Grid>

        <Grid item xs={12} sm={6} md={4}>
          <TextField
            select
            fullWidth
            label="Fuel Type"
            value={fuel}
            onChange={(e) => { setFuel(e.target.value); }}
            size="small"
            SelectProps={{ displayEmpty: true }}
            InputLabelProps={{ shrink: true }}
          >
            <MenuItem value="">Any</MenuItem>
            {facets?.fuel ? Object.keys(facets.fuel).map((f) => (
              <MenuItem key={f} value={f}>{f}</MenuItem>
            )) : null}
          </TextField>
        </Grid>

        <Grid item xs={12} sm={6} md={4}>
          <TextField
            select
            fullWidth
            label="Transmission"
            value={transmission}
            onChange={(e) => { setTransmission(e.target.value); }}
            size="small"
            SelectProps={{ displayEmpty: true }}
            InputLabelProps={{ shrink: true }}
          >
            <MenuItem value="">Any</MenuItem>
            {facets?.transmission ? Object.keys(facets.transmission).map((t) => (
              <MenuItem key={t} value={t}>{t}</MenuItem>
            )) : null}
          </TextField>
        </Grid>

        <Grid item xs={12} sm={6} md={4}>
          <TextField
            select
            fullWidth
            label="Condition"
            value={condition}
            onChange={(e) => { setCondition(e.target.value); }}
            size="small"
            SelectProps={{ displayEmpty: true }}
            InputLabelProps={{ shrink: true }}
          >
            <MenuItem value="">Any</MenuItem>
            {facets?.condition ? Object.keys(facets.condition).map((c) => (
              <MenuItem key={c} value={c}>{c}</MenuItem>
            )) : null}
          </TextField>
        </Grid>

        <Grid item xs={12} sm={6} md={4}>
          <TextField
            select
            fullWidth
            label="Vehicle Type"
            value={type}
            onChange={(e) => { setType(e.target.value); }}
            size="small"
            SelectProps={{ displayEmpty: true }}
            InputLabelProps={{ shrink: true }}
          >
            <MenuItem value="">Any</MenuItem>
            {facets?.type ? Object.keys(facets.type).map((v) => (
              <MenuItem key={v} value={v}>{v}</MenuItem>
            )) : null}
          </TextField>
        </Grid>

        {!compact && (
          <Grid item xs={12}>
            <Stack direction="row" spacing={2} justifyContent="center">
              <Button type="button" variant="outlined" onClick={handleReset}>
                Reset
              </Button>
              <Button type="submit" variant="contained" color="primary">
                Search
              </Button>
            </Stack>
          </Grid>
        )}
      </Grid>
    </Box>
  );

  return (
    <Paper elevation={2} sx={{ mb: 3 }}>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          p: 2,
          cursor: 'pointer',
          bgcolor: 'primary.main',
          color: 'white',
          '&:hover': { bgcolor: 'primary.dark' },
        }}
        onClick={() => setExpanded(!expanded)}
      >
        <Typography variant="h6">Search Filters</Typography>
        <IconButton size="small" sx={{ color: 'white' }}>
          {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
        </IconButton>
      </Box>
      <Collapse in={expanded}>
        <Box sx={{ p: 3 }}>
          {content}
        </Box>
      </Collapse>
    </Paper>
  );
}
