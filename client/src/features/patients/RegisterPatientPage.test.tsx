import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { ThemeProvider } from '@mui/material/styles';
import { theme } from '../../theme';
import { RegisterPatientPage } from './RegisterPatientPage';

function renderPage() {
  render(
    <ThemeProvider theme={theme}>
      <RegisterPatientPage />
    </ThemeProvider>,
  );
}

function jsonResponse(status: number, body: unknown) {
  return Promise.resolve({
    status,
    json: () => Promise.resolve(body),
  } as Response);
}

describe('RegisterPatientPage', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  // AC-17, FR-18 — all seven fields plus Save render.
  it('renders all seven fields plus Save', () => {
    renderPage();

    expect(screen.getByLabelText('Name')).toBeInTheDocument();
    expect(screen.getByLabelText('Age')).toBeInTheDocument();
    expect(screen.getByLabelText('Gender')).toBeInTheDocument();
    expect(screen.getByLabelText('Phone')).toBeInTheDocument();
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(screen.getByLabelText('Address')).toBeInTheDocument();
    expect(screen.getByLabelText('Note')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
  });

  // AC-17, FR-19 — a successful submit posts the expected body and
  // displays both returned codes.
  it('posts the expected body and displays the returned patient and bill codes on success', async () => {
    // The Select's Menu mounts in a Popper the first time it opens, which is
    // occasionally slower than the default findBy* timeout under this
    // repo's `pool: 'threads'`/`fileParallelism: false` Vitest config.
    const mockFetch = vi.fn().mockReturnValue(
      jsonResponse(201, { id: '11111111-1111-1111-1111-111111111111', code: 'P000001', billCode: 'BILL001-P000001' }),
    );
    vi.stubGlobal('fetch', mockFetch);

    renderPage();

    fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Jane Doe' } });
    fireEvent.change(screen.getByLabelText('Age'), { target: { value: '30' } });

    // MUI's Select renders a role="combobox" div, not a native input/select,
    // so it is driven by opening the listbox and clicking an option rather
    // than firing a `change` event directly on it.
    fireEvent.mouseDown(screen.getByLabelText('Gender'));
    fireEvent.click(await screen.findByRole('option', { name: 'Female' }, { timeout: 4000 }));

    fireEvent.change(screen.getByLabelText('Phone'), { target: { value: '0771234567' } });
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'jane@example.com' } });
    fireEvent.change(screen.getByLabelText('Address'), { target: { value: '123 Main St' } });
    fireEvent.change(screen.getByLabelText('Note'), { target: { value: 'First visit' } });

    fireEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => {
      expect(screen.getByText(/P000001/)).toBeInTheDocument();
    });
    expect(screen.getByText(/BILL001-P000001/)).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
    const [url, init] = mockFetch.mock.calls[0];
    expect(url).toContain('/api/patients');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body)).toEqual({
      name: 'Jane Doe',
      age: 30,
      gender: 'Female',
      phone: '0771234567',
      email: 'jane@example.com',
      address: '123 Main St',
      note: 'First visit',
    });
  }, 10000);

  // AC-18, FR-19 — a 400 renders the server's own field messages against
  // the offending inputs, not a generic banner.
  it('renders the server field messages against the offending inputs on a 400', async () => {
    const mockFetch = vi.fn().mockReturnValue(
      jsonResponse(400, {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: {
          Name: ['The Name field is required.'],
          Email: ['The Email field is not a valid e-mail address.'],
        },
      }),
    );
    vi.stubGlobal('fetch', mockFetch);

    renderPage();

    fireEvent.change(screen.getByLabelText('Age'), { target: { value: '30' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));

    const nameInput = await screen.findByLabelText('Name');
    const emailInput = screen.getByLabelText('Email');

    await waitFor(() => {
      expect(screen.getByText('The Name field is required.')).toBeInTheDocument();
    });
    expect(screen.getByText('The Email field is not a valid e-mail address.')).toBeInTheDocument();

    // No generic failure banner — only the field-scoped messages appear.
    expect(screen.queryByText(/unable to reach the server/i)).not.toBeInTheDocument();

    // AC-19 — every error message is programmatically associated with its
    // field, not just visually nearby.
    expect(nameInput).toHaveAttribute('aria-invalid', 'true');
    expect(nameInput).toHaveAccessibleDescription('The Name field is required.');
    expect(emailInput).toHaveAttribute('aria-invalid', 'true');
    expect(emailInput).toHaveAccessibleDescription('The Email field is not a valid e-mail address.');

    // Untouched fields stay unaffected.
    expect(screen.getByLabelText('Phone')).not.toHaveAttribute('aria-invalid', 'true');
  });

  // AC-19, NFR-06 — every input has an associated label.
  it('associates every field with its visible label', () => {
    renderPage();

    for (const label of ['Name', 'Age', 'Gender', 'Phone', 'Email', 'Address', 'Note']) {
      const field = screen.getByLabelText(label);
      expect(field).toBeInTheDocument();
      expect(field).toHaveAccessibleName(label);
    }
  });

  // Edge case — a network failure never reports a success it did not
  // receive (mirrors FR-03 on the client side).
  it('surfaces a failure banner when the request cannot reach the server', async () => {
    const mockFetch = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'));
    vi.stubGlobal('fetch', mockFetch);

    renderPage();

    fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Jane Doe' } });
    fireEvent.change(screen.getByLabelText('Age'), { target: { value: '30' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => {
      expect(screen.getByText(/unable to reach the server/i)).toBeInTheDocument();
    });
    expect(screen.queryByText(/registered/i)).not.toBeInTheDocument();
  });
});
