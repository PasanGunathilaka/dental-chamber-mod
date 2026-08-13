import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ThemeProvider } from '@mui/material/styles';
import { BrowserRouter } from 'react-router-dom';
import { theme } from './theme';
import App from './App';

describe('App', () => {
  it('renders the placeholder route', () => {
    render(
      <ThemeProvider theme={theme}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </ThemeProvider>,
    );

    expect(screen.getByText('Dental Management')).toBeInTheDocument();
  });
});
