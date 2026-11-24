import { HappeningEventDto } from '../../../core/models/happening-now.model';
import { describeDistance, describeEventStart, describeOpenUntil } from './happening-now';

describe('happening now labels', () => {
  const now = new Date('2026-11-23T10:00:00Z');
  const happening = (startAt: string, isOngoing = false): HappeningEventDto => ({
    id: 'e1', title: 'Kirtan', placeId: 'p1', placeName: 'Studio', latitude: 0, longitude: 0,
    distanceKm: 1, startAt, isOngoing
  });

  it('says until when a place is open', () => {
    expect(describeOpenUntil('18:00')).toBe('Open until 18:00');
    expect(describeOpenUntil(null)).toBe('Open 24 hours');
  });

  it('says how soon an event starts', () => {
    expect(describeEventStart(happening('2026-11-23T09:00:00Z', true), now)).toBe('On now');
    expect(describeEventStart(happening('2026-11-23T10:40:00Z'), now)).toBe('In 40 min');
    expect(describeEventStart(happening('2026-11-23T12:00:00Z'), now)).toBe('In 2 h');
  });

  it('gives short distances in metres', () => {
    expect(describeDistance(0.34)).toBe('350 m');
    expect(describeDistance(4.23)).toBe('4.2 km');
  });
});
