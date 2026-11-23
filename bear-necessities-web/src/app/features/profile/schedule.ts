import { MyRsvpDto } from '../../core/services/profile.service';

/** One event the traveller is going to, on the schedule. */
export interface ScheduleEntry {
  eventId: string;
  title: string;
  placeName?: string;
  startAt: Date;
  status: string;
}

/** The events of one calendar day, in time order. */
export interface ScheduleDay {
  date: Date;
  entries: ScheduleEntry[];
}

// RSVPs that put an event on the traveller's schedule; "NotGoing" ones don't
const SCHEDULED_STATUSES = new Set(['Going', 'Maybe']);
// Events without an end time are assumed to last this long in calendar files
const DEFAULT_EVENT_HOURS = 2;

/** Upcoming RSVP'd events grouped by local day, earliest first; past and undated ones are left out. */
export function buildScheduleDays(rsvps: MyRsvpDto[], now: Date): ScheduleDay[] {
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const entries = rsvps
    .filter(rsvp => SCHEDULED_STATUSES.has(rsvp.status) && rsvp.eventStartAt)
    .map(rsvp => ({
      eventId: rsvp.eventId,
      title: rsvp.eventTitle,
      placeName: rsvp.placeName,
      startAt: new Date(rsvp.eventStartAt!),
      status: rsvp.status
    }))
    .filter(entry => !Number.isNaN(entry.startAt.getTime()) && entry.startAt >= startOfToday)
    .sort((first, second) => first.startAt.getTime() - second.startAt.getTime());

  const days: ScheduleDay[] = [];
  for (const entry of entries) {
    const lastDay = days[days.length - 1];
    if (lastDay && isSameLocalDay(lastDay.date, entry.startAt)) {
      lastDay.entries.push(entry);
    } else {
      days.push({ date: new Date(entry.startAt.getFullYear(), entry.startAt.getMonth(), entry.startAt.getDate()), entries: [entry] });
    }
  }
  return days;
}

/** An iCalendar file with every scheduled event, for phone and desktop calendars. */
export function toCalendarFile(days: ScheduleDay[], eventUrl: (eventId: string) => string, now: Date): string {
  const lines = ['BEGIN:VCALENDAR', 'VERSION:2.0', 'PRODID:-//Bear Necessities//My schedule//EN', 'CALSCALE:GREGORIAN'];
  for (const entry of days.flatMap(day => day.entries)) {
    const endAt = new Date(entry.startAt.getTime() + DEFAULT_EVENT_HOURS * 3_600_000);
    lines.push(
      'BEGIN:VEVENT',
      `UID:${entry.eventId}@bearnecessities.app`,
      `DTSTAMP:${calendarTime(now)}`,
      `DTSTART:${calendarTime(entry.startAt)}`,
      `DTEND:${calendarTime(endAt)}`,
      `SUMMARY:${calendarText(entry.title)}`,
      ...(entry.placeName ? [`LOCATION:${calendarText(entry.placeName)}`] : []),
      `URL:${eventUrl(entry.eventId)}`,
      'END:VEVENT');
  }
  lines.push('END:VCALENDAR');
  return lines.join('\r\n') + '\r\n';
}

function isSameLocalDay(first: Date, second: Date): boolean {
  return first.getFullYear() === second.getFullYear()
    && first.getMonth() === second.getMonth()
    && first.getDate() === second.getDate();
}

// UTC in the compact iCalendar form, 20261128T160000Z
function calendarTime(date: Date): string {
  return date.toISOString().replace(/[-:]/g, '').replace(/\.\d{3}/, '');
}

// iCalendar text escapes backslashes, semicolons, commas and line breaks
function calendarText(text: string): string {
  return text.replace(/\\/g, '\\\\').replace(/;/g, '\\;').replace(/,/g, '\\,').replace(/\r?\n/g, '\\n');
}
