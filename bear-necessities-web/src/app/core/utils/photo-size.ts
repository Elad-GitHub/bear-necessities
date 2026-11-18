// Wikimedia serves any width of a photo from its thumb URL, so cards and popups don't download the full 1280px image
export function sizedPhotoUrl(url: string | null | undefined, widthPixels: number): string | null {
  if (!url) return null;
  return url.replace(/\/\d+px-/, `/${widthPixels}px-`);
}
