import { describe, expect, it } from 'vitest';
import { theme } from './theme';

// AC-16 — assert the resolved values on the theme object itself, not a
// rendered screenshot.
describe('theme tokens', () => {
  it('carries TK-001 global colors', () => {
    expect(theme.palette.background.default).toBe('#EBEBEB');
    expect(theme.palette.text.primary).toBe('#333');
    expect(theme.palette.dropdownMenu.background).toBe('#006a4e');
    expect(theme.palette.dropdownMenu.itemHoverBackground).toBe('#2e8b57');
    expect(theme.palette.formControl.background).toBe('#f5f5f5');
  });

  it('carries TK-002 global typography', () => {
    expect(theme.typography.fontFamily).toBe(
      '"Helvetica Neue", Helvetica, Arial, sans-serif',
    );
    expect(theme.typography.fontSize).toBe(14);
  });

  it('carries TK-003 navbar background as #218283 per CQ-004', () => {
    expect(theme.palette.navbar.background).toBe('#218283');
  });
});
