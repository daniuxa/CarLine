import './styles/App.css';

import {
  CssBaseline,
  ThemeProvider,
} from '@mui/material';
import { createTheme } from '@mui/material/styles';
import {
  QueryClient,
  QueryClientProvider,
} from '@tanstack/react-query';

import { FacetsProvider } from '../contexts/FacetsContext';
import { MainPage } from './MainPage';

const queryClient = new QueryClient();

function App() {
  const theme = createTheme({
    palette: {
      primary: { main: "#1565c0" },
      secondary: { main: "#ff7043" },
    },
    typography: { h5: { fontWeight: 700 } },
  });

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <FacetsProvider>
          <MainPage />
        </FacetsProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}

export default App;
