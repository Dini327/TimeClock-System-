import { createTheme } from '@mui/material/styles';

const muiTheme = createTheme({
  palette: {
    primary: {
      main:  '#1565C0',   // Deep Blue
      light: '#1976D2',
      dark:  '#0D47A1',
    },
    secondary: {
      main:  '#F57C00',   // Orange (action buttons, highlights)
      light: '#FF9800',
      dark:  '#E65100',
    },
    background: {
      default: '#F5F7FA',
      paper:   '#FFFFFF',
    },
    error:   { main: '#D32F2F' },
    warning: { main: '#F57C00' },
    success: { main: '#388E3C' },
  },

  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica Neue", Arial, sans-serif',
    h4: { fontWeight: 700 },
    h5: { fontWeight: 600 },
    h6: { fontWeight: 600 },
    subtitle1: { fontWeight: 500 },
    button: {
      textTransform: 'none',
      fontWeight: 600,
    },
  },

  shape: { borderRadius: 10 },

  components: {
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: { borderRadius: 8, padding: '10px 24px' },
        sizeLarge: { padding: '12px 32px', fontSize: '1rem' },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: { boxShadow: '0 2px 12px rgba(0,0,0,0.08)', borderRadius: 12 },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: { boxShadow: '0 1px 4px rgba(0,0,0,0.12)' },
      },
    },
    MuiTextField: {
      defaultProps: { variant: 'outlined', size: 'small' },
    },
    MuiChip: {
      styleOverrides: {
        root: { fontWeight: 600 },
      },
    },
  },
});

export default muiTheme;
