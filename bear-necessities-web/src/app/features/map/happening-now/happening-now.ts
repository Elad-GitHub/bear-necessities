import { HappeningEventDto } from '../../../core/models/happening-now.model';

const MS_PER_MINUTE = 60_000;
const MINUTES_PER_HOUR = 60;

/** "Open until 18:00", or "Open 24 hours" for a place that never closes. */
export function describeOpenUntil(closesAt: string | null): string {
  return closesAt ? `Open until ${closesAt}` : 'Open 24 hours';
}

/** "On now", "In 40 min", "In 3 h" or the start time, for an event that starts soon. */
export function describeEventStart(happening: HappeningEventDto, now: Date): string {
  if (happening.isOngoing) return 'On now';

  const start = new Date(happening.startAt);
  const minutes = Math.round((start.getTime() - now.getTime()) / MS_PER_MINUTE);
  if (minutes < MINUTES_PER_HOUR) return `In ${Math.max(minutes, 1)} min`;
  if (minutes < 3 * MINUTES_PER_HOUR) return `In ${Math.round(minutes / MINUTES_PER_HOUR)} h`;
  return start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

/** "350 m" or "4.2 km". */
export function describeDistance(distanceKm: number): string {
  return distanceKm < 1 ? `${Math.round(distanceKm * 1000 / 50) * 50} m` : `${distanceKm.toFixed(1)} km`;
}
