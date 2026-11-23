import { describeApproximateLocation } from './location-accuracy';

describe('describeApproximateLocation', () => {
  it('says nothing when the position is precise', () => {
    expect(describeApproximateLocation(35)).toBeNull();
    expect(describeApproximateLocation(500)).toBeNull();
  });

  it('rounds to hundreds of metres under a kilometre', () => {
    expect(describeApproximateLocation(740)).toContain('about 700 m');
  });

  it('rounds to kilometres beyond that', () => {
    expect(describeApproximateLocation(2400)).toContain('about 2 km');
  });

  it('ignores a missing accuracy', () => {
    expect(describeApproximateLocation(Number.NaN)).toBeNull();
  });
});
