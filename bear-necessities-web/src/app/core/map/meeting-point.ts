import { ParamMap } from '@angular/router';

/** A spot and optional time a traveller shares with friends ("meet me here at 18:00"). */
export interface MeetingPoint {
  latitude: number;
  longitude: number;
  /** Local date and time as typed, "2026-11-28T18:00"; shown as the sender wrote it. */
  at?: string;
  note?: string;
}

export const MAX_MEETING_NOTE_LENGTH = 200;

const PARAM = { latitude: 'meetLat', longitude: 'meetLng', at: 'meetAt', note: 'meetNote' } as const;
const LOCAL_DATE_TIME = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/;

/** A link that opens the map on the meeting point, safe to send over WhatsApp or SMS. */
export function meetingPointLink(origin: string, point: MeetingPoint): string {
  const params = new URLSearchParams({
    [PARAM.latitude]: point.latitude.toFixed(5),
    [PARAM.longitude]: point.longitude.toFixed(5)
  });
  if (point.at) params.set(PARAM.at, point.at);
  if (point.note?.trim()) params.set(PARAM.note, point.note.trim().slice(0, MAX_MEETING_NOTE_LENGTH));
  return `${origin}/map?${params.toString()}`;
}

/** Reads a meeting point from the map's query string; anything malformed is ignored. */
export function readMeetingPoint(params: ParamMap): MeetingPoint | null {
  const latitude = Number(params.get(PARAM.latitude));
  const longitude = Number(params.get(PARAM.longitude));
  if (!params.has(PARAM.latitude) || !params.has(PARAM.longitude)
      || !Number.isFinite(latitude) || !Number.isFinite(longitude)
      || Math.abs(latitude) > 90 || Math.abs(longitude) > 180) {
    return null;
  }

  const at = params.get(PARAM.at) ?? undefined;
  const note = params.get(PARAM.note)?.slice(0, MAX_MEETING_NOTE_LENGTH) || undefined;
  return { latitude, longitude, at: at && LOCAL_DATE_TIME.test(at) ? at : undefined, note };
}

/** "Sat 28 Nov, 18:00" in the reader's language, or null when no time was set. */
export function describeMeetingTime(at: string | undefined, locale?: string): string | null {
  if (!at) return null;
  const date = new Date(at);
  if (Number.isNaN(date.getTime())) return null;
  return date.toLocaleString(locale, { weekday: 'short', day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
}

/** The message that goes with the link. */
export function meetingPointMessage(point: MeetingPoint, link: string, locale?: string): string {
  const when = describeMeetingTime(point.at, locale);
  const lines = [`Meet me here${when ? ` · ${when}` : ''} 🐾`];
  if (point.note?.trim()) lines.push(point.note.trim());
  lines.push(link);
  return lines.join('\n');
}

/** Walking or driving directions from wherever the reader is. */
export function directionsLink(point: MeetingPoint): string {
  return `https://www.google.com/maps/dir/?api=1&destination=${point.latitude.toFixed(5)},${point.longitude.toFixed(5)}`;
}
