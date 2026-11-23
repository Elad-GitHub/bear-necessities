import { convertToParamMap } from '@angular/router';
import { directionsLink, meetingPointLink, meetingPointMessage, readMeetingPoint } from './meeting-point';

describe('meeting point links', () => {
  it('round-trips a point with time and note through the link', () => {
    const link = meetingPointLink('https://app.test', {
      latitude: 32.24361, longitude: 76.32541, at: '2026-11-28T18:00', note: 'By the prayer wheels'
    });
    const params = convertToParamMap(Object.fromEntries(new URL(link).searchParams));

    expect(readMeetingPoint(params)).toEqual({
      latitude: 32.24361, longitude: 76.32541, at: '2026-11-28T18:00', note: 'By the prayer wheels'
    });
  });

  it('ignores a link with coordinates off the globe', () => {
    expect(readMeetingPoint(convertToParamMap({ meetLat: '120', meetLng: '10' }))).toBeNull();
    expect(readMeetingPoint(convertToParamMap({ meetLat: 'abc', meetLng: '10' }))).toBeNull();
  });

  it('drops a time that is not a plain local date and time', () => {
    const point = readMeetingPoint(convertToParamMap({ meetLat: '10', meetLng: '20', meetAt: '<script>' }));

    expect(point?.at).toBeUndefined();
  });

  it('puts the note and link in the message and builds a directions link', () => {
    const point = { latitude: 15.6, longitude: 73.7, note: 'Blue umbrella' };

    expect(meetingPointMessage(point, 'https://link')).toContain('Blue umbrella\nhttps://link');
    expect(directionsLink(point)).toBe('https://www.google.com/maps/dir/?api=1&destination=15.60000,73.70000');
  });
});
