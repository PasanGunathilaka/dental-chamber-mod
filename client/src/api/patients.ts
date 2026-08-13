/**
 * Typed client for `POST /api/patients` (spec A5, FR-09..FR-12).
 *
 * The endpoint's contract, from the design's "API Changes" section:
 *
 * ```
 * Request   { name, age, gender?, phone?, email?, address?, note? }
 * 201       { id, code, billCode }
 * 400       ProblemDetails                  validation, field-scoped
 * 500       ProblemDetails                  the write did not persist (FR-03)
 * ```
 */

/** `CK_Patient_Gender`'s accepted values (CQ-007). Anything else is a 400 (FR-10). */
export type Gender = 'Male' | 'Female' | 'Others';

export interface RegisterPatientRequest {
  name: string;
  age: number;
  gender?: Gender;
  phone?: string;
  email?: string;
  address?: string;
  note?: string;
}

export interface RegisterPatientResponse {
  id: string;
  code: string;
  billCode: string;
}

/** RFC 9457 Problem Details. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
}

/** RFC 9457 `ValidationProblemDetails` — `errors` is keyed by field name. */
export interface ValidationProblemDetails extends ProblemDetails {
  errors: Record<string, string[]>;
}

/**
 * A 400 from `POST /api/patients`. Carries the server's own per-field
 * messages so the caller can render them against the offending inputs
 * instead of flattening them into one string (spec FR-19).
 */
export class PatientValidationError extends Error {
  readonly problem: ValidationProblemDetails;

  constructor(problem: ValidationProblemDetails) {
    super(problem.title ?? 'Validation failed');
    this.name = 'PatientValidationError';
    this.problem = problem;
  }
}

/**
 * A 500 from `POST /api/patients` — the registration write did not persist
 * (spec FR-03). The caller must never present this as a success.
 */
export class PatientRegistrationFailedError extends Error {
  readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? 'Registration failed');
    this.name = 'PatientRegistrationFailedError';
    this.problem = problem;
  }
}

// The API has no launchSettings.json in this repository, so there is no
// declared dev port; this is Kestrel's own fallback when none is configured.
// Program.cs's CORS policy allows the Vite dev client's origin
// (http://localhost:5173) to call it directly.
const DEFAULT_API_BASE_URL = 'http://localhost:5000';

const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? DEFAULT_API_BASE_URL;

export async function registerPatient(
  request: RegisterPatientRequest,
): Promise<RegisterPatientResponse> {
  const response = await fetch(`${API_BASE_URL}/api/patients`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (response.status === 201) {
    return (await response.json()) as RegisterPatientResponse;
  }

  const problem: ProblemDetails = await response.json();

  if (response.status === 400) {
    throw new PatientValidationError(problem as ValidationProblemDetails);
  }

  throw new PatientRegistrationFailedError(problem);
}
