import { isSafeReturnUrl } from './auth.guard';

describe('isSafeReturnUrl', () => {
  it('accepts paths inside the app', () => {
    expect(isSafeReturnUrl('/submit')).toBe(true);
    expect(isSafeReturnUrl('/places/123?tab=reviews')).toBe(true);
  });

  it('rejects anything that could leave the app', () => {
    expect(isSafeReturnUrl('https://evil.example')).toBe(false);
    expect(isSafeReturnUrl('//evil.example')).toBe(false);
    expect(isSafeReturnUrl('/\\evil.example')).toBe(false);
    expect(isSafeReturnUrl('javascript:alert(1)')).toBe(false);
  });

  it('rejects a missing value', () => {
    expect(isSafeReturnUrl(null)).toBe(false);
    expect(isSafeReturnUrl('')).toBe(false);
  });
});
