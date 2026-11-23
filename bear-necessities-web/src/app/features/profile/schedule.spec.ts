import { MyRsvpDto } from '../../core/services/profile.service';
import { buildScheduleDays, toCalendarFile } from './schedule';

const rsvp = (overrides: Partial<MyRsvpDto>): MyRsvpDto => ({
  id: 'r', eventId: 'e', eventTitle: 'Kirtan night', status: 'Going', partySize: 1, ...overrides
} as MyRsvpDto);

describe('schedule', () => {
  const now = new Date(2026, 10, 23, 12, 0);

  it('groups upcoming events by day in time order and leaves out past, undated and declined ones', () => {
    const days = buildScheduleDays([
      rsvp({ eventId: 'late', eventStartAt: new Date(2026, 10, 25, 20, 0).toISOString() }),
      rsvp({ eventId: 'early', eventStartAt: new Date(2026, 10, 25, 9, 0).toISOString(), status: 'Maybe' }),
      rsvp({ eventId: 'next', eventStartAt: new Date(2026, 10, 27, 18, 0).toISOString() }),
      rsvp({ eventId: 'past', eventStartAt: new Date(2026, 10, 20, 18, 0).toISOString() }),
      rsvp({ eventId: 'declined', eventStartAt: new Date(2026, 10, 26, 18, 0).toISOString(), status: 'NotGoing' }),
      rsvp({ eventId: 'undated' })
    ], now);

    expect(days.map(day => day.entries.map(entry => entry.eventId))).toEqual([['early', 'late'], ['next']]);
  });

  it('keeps an event from earlier today', () => {
    const days = buildScheduleDays([rsvp({ eventStartAt: new Date(2026, 10, 23, 8, 0).toISOString() })], now);

    expect(days).toHaveLength(1);
  });

  it('writes a calendar file with escaped text and a link back to each event', () => {
    const days = buildScheduleDays([
      rsvp({ eventId: 'e1', eventTitle: 'Cacao, dance; and song', placeName: 'The Yoga Barn',
        eventStartAt: new Date(Date.UTC(2026, 10, 28, 16, 0)).toISOString() })
    ], now);

    const calendar = toCalendarFile(days, id => `https://app.test/events/${id}`, now);

    expect(calendar).toContain('DTSTART:20261128T160000Z');
    expect(calendar).toContain('DTEND:20261128T180000Z');
    expect(calendar).toContain('SUMMARY:Cacao\\, dance\\; and song');
    expect(calendar).toContain('URL:https://app.test/events/e1');
    expect(calendar.startsWith('BEGIN:VCALENDAR\r\n')).toBe(true);
  });
});
