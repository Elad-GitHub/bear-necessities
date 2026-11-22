import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { GeocodeResult, GeocodingService } from './geocoding.service';

describe('GeocodingService', () => {
  let service: GeocodingService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(GeocodingService);
    http = TestBed.inject(HttpTestingController);
  });

  const searchFor = (feature: object): GeocodeResult[] => {
    let results: GeocodeResult[] = [];
    service.search('somewhere').subscribe(found => (results = found));
    http.expectOne(request => request.url.includes('/geocoding/')).flush({ features: [feature] });
    return results;
  };

  it('takes the country code from the context of a venue', () => {
    const [venue] = searchFor({
      id: 'poi.1',
      text: 'Chapitô',
      place_name: 'Chapitô, Lisbon, Portugal',
      center: [-9.13, 38.71],
      context: [
        { id: 'place.2', text: 'Lisbon' },
        { id: 'country.3', text: 'Portugal', short_code: 'pt' }
      ]
    });

    expect(venue.country).toBe('Portugal');
    expect(venue.countryCode).toBe('PT');
    expect(venue.city).toBe('Lisbon');
  });

  it('takes the country code from a country result itself', () => {
    const [country] = searchFor({
      id: 'country.3',
      text: 'Thailand',
      place_name: 'Thailand',
      center: [101, 15],
      properties: { short_code: 'th' }
    });

    expect(country.country).toBe('Thailand');
    expect(country.countryCode).toBe('TH');
  });

  it('leaves the country code empty for a dropped pin', () => {
    let results: GeocodeResult[] = [];
    service.search('38.7, -9.1').subscribe(found => (results = found));

    expect(results[0].countryCode).toBeUndefined();
  });
});
