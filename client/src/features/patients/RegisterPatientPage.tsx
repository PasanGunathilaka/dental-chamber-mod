import { useState } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import MenuItem from '@mui/material/MenuItem';
import Paper from '@mui/material/Paper';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import {
  registerPatient,
  PatientRegistrationFailedError,
  PatientValidationError,
} from '../../api/patients';
import type { Gender } from '../../api/patients';

// SCR-003's field list and order are fixed by the legacy screen inventory
// (spec FR-18) — Name, Age, Gender, Phone, Email, Address, Note, Save.
const GENDERS: Gender[] = ['Male', 'Female', 'Others'];

interface FormState {
  name: string;
  age: string;
  gender: Gender | '';
  phone: string;
  email: string;
  address: string;
  note: string;
}

const EMPTY_FORM: FormState = {
  name: '',
  age: '',
  gender: '',
  phone: '',
  email: '',
  address: '',
  note: '',
};

interface SuccessState {
  code: string;
  billCode: string;
}

/** Looks up a field's server-reported errors, matching the key case-insensitively. */
function fieldErrorMessage(
  errors: Record<string, string[]> | undefined,
  field: keyof FormState,
): string | undefined {
  if (!errors) {
    return undefined;
  }

  const key = Object.keys(errors).find((k) => k.toLowerCase() === field.toLowerCase());
  return key ? errors[key].join(' ') : undefined;
}

export function RegisterPatientPage() {
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [submitting, setSubmitting] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>();
  const [formError, setFormError] = useState<string>();
  const [success, setSuccess] = useState<SuccessState>();

  const handleChange =
    (field: keyof FormState) => (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const { value } = event.target;
      setForm((prev) => ({ ...prev, [field]: value }));
    };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitting(true);
    setFormError(undefined);
    setFieldErrors(undefined);
    setSuccess(undefined);

    try {
      // An empty/invalid Age yields NaN, which JSON.stringify serializes as
      // `null` — the server's own [Required] validation on the nullable
      // `int?` reports that as a field error rather than the client guessing.
      const age = form.age.trim() === '' ? NaN : Number(form.age);

      const response = await registerPatient({
        name: form.name,
        age,
        gender: form.gender === '' ? undefined : form.gender,
        phone: form.phone || undefined,
        email: form.email || undefined,
        address: form.address || undefined,
        note: form.note || undefined,
      });

      setSuccess({ code: response.code, billCode: response.billCode });
      setForm(EMPTY_FORM);
    } catch (error) {
      if (error instanceof PatientValidationError) {
        setFieldErrors(error.problem.errors);
      } else if (error instanceof PatientRegistrationFailedError) {
        setFormError(error.message);
      } else {
        // Offline / API down (spec edge case) — never claim success it did
        // not receive (mirrors FR-03 on the client side).
        setFormError('Unable to reach the server. Please try again.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'center',
        px: { xs: 2, sm: 4 },
        py: { xs: 4, sm: 6 },
      }}
    >
      <Paper elevation={2} sx={{ width: '100%', maxWidth: 480, p: { xs: 3, sm: 4 } }}>
        <Typography variant="h5" component="h1" gutterBottom>
          New Patient
        </Typography>

        {success && (
          <Alert severity="success" sx={{ mb: 2 }}>
            Patient {success.code} registered. Bill {success.billCode} opened.
          </Alert>
        )}

        {formError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {formError}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} noValidate>
          <Stack spacing={2}>
            <TextField
              label="Name"
              value={form.name}
              onChange={handleChange('name')}
              error={!!fieldErrorMessage(fieldErrors, 'name')}
              helperText={fieldErrorMessage(fieldErrors, 'name')}
              fullWidth
            />
            <TextField
              label="Age"
              type="number"
              value={form.age}
              onChange={handleChange('age')}
              error={!!fieldErrorMessage(fieldErrors, 'age')}
              helperText={fieldErrorMessage(fieldErrors, 'age')}
              fullWidth
            />
            <TextField
              label="Gender"
              select
              value={form.gender}
              onChange={handleChange('gender')}
              error={!!fieldErrorMessage(fieldErrors, 'gender')}
              helperText={fieldErrorMessage(fieldErrors, 'gender')}
              fullWidth
            >
              <MenuItem value="">
                <em>None</em>
              </MenuItem>
              {GENDERS.map((gender) => (
                <MenuItem key={gender} value={gender}>
                  {gender}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Phone"
              value={form.phone}
              onChange={handleChange('phone')}
              error={!!fieldErrorMessage(fieldErrors, 'phone')}
              helperText={fieldErrorMessage(fieldErrors, 'phone')}
              fullWidth
            />
            <TextField
              label="Email"
              value={form.email}
              onChange={handleChange('email')}
              error={!!fieldErrorMessage(fieldErrors, 'email')}
              helperText={fieldErrorMessage(fieldErrors, 'email')}
              fullWidth
            />
            <TextField
              label="Address"
              value={form.address}
              onChange={handleChange('address')}
              error={!!fieldErrorMessage(fieldErrors, 'address')}
              helperText={fieldErrorMessage(fieldErrors, 'address')}
              multiline
              rows={3}
              fullWidth
            />
            <TextField
              label="Note"
              value={form.note}
              onChange={handleChange('note')}
              error={!!fieldErrorMessage(fieldErrors, 'note')}
              helperText={fieldErrorMessage(fieldErrors, 'note')}
              multiline
              rows={3}
              fullWidth
            />
            <Button type="submit" variant="contained" disabled={submitting}>
              Save
            </Button>
          </Stack>
        </Box>
      </Paper>
    </Box>
  );
}

export default RegisterPatientPage;
