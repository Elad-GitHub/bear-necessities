import { environment } from '../../../environments/environment';

const API_ORIGIN = new URL(environment.apiUrl).origin;

/** The API stores some image links as paths ("/api/v1/media/..."); turn them into a full URL for <img>. */
export function resolveMediaUrl(url: string | null | undefined): string | null {
  if (!url) return null;
  return url.startsWith('/') ? API_ORIGIN + url : url;
}
