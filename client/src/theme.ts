import { createTheme } from '@mui/material/styles';

/**
 * Legacy design tokens carried into the MUI theme (spec FR-17).
 *
 * - TK-001 (global colors): body background, body text, dropdown-menu
 *   background/hover, form-control background.
 * - TK-002 (global typography): font family and base size.
 * - TK-003 (navbar background) — PROVISIONAL in
 *   .specclaw/ui/design-tokens.json with two candidate values
 *   (#006A4E from style.css vs #218283 from nav.html). CQ-004 resolved the
 *   contest: use #218283 as the effective legacy navbar colour.
 */
declare module '@mui/material/styles' {
  interface Palette {
    dropdownMenu: {
      background: string;
      itemHoverBackground: string;
    };
    formControl: {
      background: string;
    };
    navbar: {
      background: string;
    };
  }

  interface PaletteOptions {
    dropdownMenu?: {
      background: string;
      itemHoverBackground: string;
    };
    formControl?: {
      background: string;
    };
    navbar?: {
      background: string;
    };
  }
}

export const theme = createTheme({
  palette: {
    background: {
      default: '#EBEBEB', // TK-001 body-background
    },
    text: {
      primary: '#333', // TK-001 body-text-color
    },
    dropdownMenu: {
      background: '#006a4e', // TK-001 dropdown-menu-background
      itemHoverBackground: '#2e8b57', // TK-001 dropdown-menu-item-hover-background
    },
    formControl: {
      background: '#f5f5f5', // TK-001 form-control-background
    },
    navbar: {
      background: '#218283', // TK-003, per CQ-004
    },
  },
  typography: {
    fontFamily: '"Helvetica Neue", Helvetica, Arial, sans-serif', // TK-002
    fontSize: 14, // TK-002 base size (px)
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: '#EBEBEB',
          color: '#333',
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: ({ theme }) => ({
          backgroundColor: theme.palette.navbar.background,
        }),
      },
    },
    MuiMenu: {
      styleOverrides: {
        paper: ({ theme }) => ({
          backgroundColor: theme.palette.dropdownMenu.background,
        }),
      },
    },
    MuiMenuItem: {
      styleOverrides: {
        root: ({ theme }) => ({
          '&:hover': {
            backgroundColor: theme.palette.dropdownMenu.itemHoverBackground,
          },
        }),
      },
    },
    MuiInputBase: {
      styleOverrides: {
        root: ({ theme }) => ({
          backgroundColor: theme.palette.formControl.background,
        }),
      },
    },
  },
});
