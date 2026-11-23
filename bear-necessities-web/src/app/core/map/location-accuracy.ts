// Beyond this the browser guessed from Wi-Fi or IP rather than GPS, and the dot can be streets away
export const APPROXIMATE_LOCATION_METERS = 500;

const METERS_PER_KM = 1000;

/** A note for the member when their reported position is only an estimate, or null when it is precise enough. */
export function describeApproximateLocation(accuracyMeters: number): string | null {
  if (!(accuracyMeters > APPROXIMATE_LOCATION_METERS)) return null;

  const distance = accuracyMeters < METERS_PER_KM
    ? `${Math.round(accuracyMeters / 100) * 100} m`
    : `${Math.round(accuracyMeters / METERS_PER_KM)} km`;
  return `Your location is approximate (within about ${distance}). Your device has no GPS fix, so it guessed from nearby Wi-Fi.`;
}
