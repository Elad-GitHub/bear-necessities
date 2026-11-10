import { Component, OnInit, OnDestroy, AfterViewInit, signal, computed, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { CategoriesService } from '../../core/services/categories.service';
import { ToastService } from '../../core/services/toast.service';
import { CategoryTreeDto } from '../../core/models/category.model';
import { EventsService } from '../../core/services/events.service';
import { GeocodingService, GeocodeResult } from '../../core/services/geocoding.service';
import { MediaService, MediaItemDto } from '../../core/services/media.service';
import { PlaceSummaryDto } from '../../core/models/place.model';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import mapboxgl from 'mapbox-gl';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-submit-listing',
  templateUrl: './submit-listing.component.html',
  styleUrls: ['./submit-listing.component.scss'],
  standalone: false
})
export class SubmitListingComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('mapContainer') mapContainer!: ElementRef;

  form!: FormGroup;
  isSubmitting = signal(false);
  isMapReady = signal(false);
  categories = signal<CategoryTreeDto[]>([]);
  selectedCategoryIds = signal<string[]>([]);
  editPlaceId = signal<string | null>(null);
  editEventId = signal<string | null>(null);
  isEditMode = computed(() => this.editPlaceId() !== null || this.editEventId() !== null);

  isEventMode = signal(false);
  selectedVenue = signal<PlaceSummaryDto | null>(null);
  venueResults = signal<PlaceSummaryDto[]>([]);
  isVenueDropdownOpen = signal(false);
  private venueSearchTerms = new Subject<string>();

  geocodeResults = signal<GeocodeResult[]>([]);
  isGeocodeDropdownOpen = signal(false);
  private geocodeSearchTerms = new Subject<string>();

  photos = signal<MediaItemDto[]>([]);
  isUploadingPhoto = signal(false);

  private mapInstance: mapboxgl.Map | null = null;
  private locationMarker: mapboxgl.Marker | null = null;

  readonly MAX_CATEGORIES = 5;
  readonly TIMEZONES = Intl.supportedValuesOf ? Intl.supportedValuesOf('timeZone') : [];

  selectedCategoryCount = computed(() => this.selectedCategoryIds().length);
  canAddMoreCategories = computed(() => this.selectedCategoryCount() < this.MAX_CATEGORIES);

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private api: ApiService,
    private categoriesService: CategoriesService,
    private eventsService: EventsService,
    private geocodingService: GeocodingService,
    private mediaService: MediaService,
    private toastService: ToastService
  ) {
    this.geocodeSearchTerms.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      switchMap(term => term.trim().length < 3
        ? of([] as GeocodeResult[])
        : this.geocodingService.search(term))
    ).subscribe(results => {
      this.geocodeResults.set(results);
      this.isGeocodeDropdownOpen.set(results.length > 0);
    });

    this.venueSearchTerms.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(term => term.trim().length < 2
        ? of({ items: [] })
        : this.api.get<{ items: PlaceSummaryDto[] }>('/places/search', { query: term, limit: 6 }))
    ).subscribe(response => {
      this.venueResults.set(response?.items || []);
      this.isVenueDropdownOpen.set(true);
    });
  }

  ngOnInit(): void {
    this.buildForm();
    this.loadCategories();

    const editId = this.route.snapshot.queryParamMap.get('edit');
    if (editId) {
      this.editPlaceId.set(editId);
      this.loadPlaceForEdit(editId);
      this.loadPhotos(editId);
    }

    const editEventId = this.route.snapshot.queryParamMap.get('editEvent');
    if (editEventId) {
      this.editEventId.set(editEventId);
      this.setListingType(false);
      this.loadEventForEdit(editEventId);
    }
  }

  private loadEventForEdit(eventId: string): void {
    this.eventsService.getById(eventId).subscribe({
      next: (response) => {
        const eventDetail = response?.event || response;
        if (!eventDetail) return;

        this.form.patchValue({
          name: eventDetail.title,
          description: eventDetail.description || '',
          timezone: eventDetail.timezone || this.form.get('timezone')?.value,
          isPermanent: false,
          startAt: this.toDateTimeLocal(eventDetail.startAt),
          endAt: this.toDateTimeLocal(eventDetail.endAt),
          isFree: eventDetail.isFree ?? true,
          price: eventDetail.price ?? null,
          currency: eventDetail.currency || 'USD',
          maxCapacity: eventDetail.maxCapacity ?? null,
          isOnline: !!eventDetail.onlineUrl,
          onlineUrl: eventDetail.onlineUrl || '',
          recurrence: this.recurrenceFromRRule(eventDetail.rRule)
        });

        this.selectedCategoryIds.set(eventDetail.categoryIds || []);

        if (eventDetail.placeId) {
          this.api.get<any>(`/places/${eventDetail.placeId}`).subscribe({
            next: (placeResponse) => {
              const venue = placeResponse?.place || placeResponse;
              if (venue) this.selectedVenue.set(venue);
            }
          });
        }
      },
      error: () => {
        this.toastService.error('Could not load event for editing');
        this.router.navigate(['/profile'], { queryParams: { tab: 'listings' } });
      }
    });
  }

  private toDateTimeLocal(isoDate?: string): string {
    if (!isoDate) return '';
    return isoDate.slice(0, 16);
  }

  private recurrenceFromRRule(rRule?: string): string {
    if (!rRule) return 'none';
    if (rRule.includes('FREQ=WEEKLY')) return 'weekly';
    if (rRule.includes('FREQ=MONTHLY')) return 'monthly';
    return 'none';
  }

  private loadPhotos(placeId: string): void {
    this.mediaService.getByOwner('Place', placeId).subscribe({
      next: (response) => this.photos.set(response?.items || [])
    });
  }

  onPhotoSelected(fileInput: HTMLInputElement): void {
    const file = fileInput.files?.[0];
    fileInput.value = '';
    if (!file || !this.editPlaceId()) return;

    if (!file.type.startsWith('image/')) {
      this.toastService.error('Only image files are allowed');
      return;
    }

    this.isUploadingPhoto.set(true);
    this.mediaService.upload('Place', this.editPlaceId()!, file).subscribe({
      next: (uploaded) => {
        this.photos.set([...this.photos(), uploaded]);
        this.isUploadingPhoto.set(false);
        this.toastService.success('Photo uploaded!');
      },
      error: () => this.isUploadingPhoto.set(false)
    });
  }

  deletePhoto(photo: MediaItemDto): void {
    this.mediaService.delete(photo.id).subscribe({
      next: () => {
        this.photos.set(this.photos().filter(item => item.id !== photo.id));
        this.toastService.success('Photo removed');
      }
    });
  }

  private loadPlaceForEdit(placeId: string): void {
    this.api.get<any>(`/places/${placeId}`).subscribe({
      next: (response) => {
        const place = response?.place || response;
        if (!place) return;

        this.form.patchValue({
          name: place.name,
          description: place.description || '',
          address: place.address || '',
          city: place.city || '',
          country: place.country || '',
          timezone: place.timezone || this.form.get('timezone')?.value,
          latitude: place.latitude,
          longitude: place.longitude,
          isPermanent: place.isPermanent ?? true
        });

        this.selectedCategoryIds.set(place.categoryIds || []);
        this.patchLinksFromExternalLinks(place.externalLinks || []);
        this.onLatLngInput();
      },
      error: () => {
        this.toastService.error('Could not load listing for editing');
        this.router.navigate(['/profile'], { queryParams: { tab: 'listings' } });
      }
    });
  }

  private patchLinksFromExternalLinks(externalLinks: { label?: string; url?: string }[]): void {
    for (const link of externalLinks) {
      const label = (link.label || '').toLowerCase();
      if (label === 'website') this.form.patchValue({ website: link.url });
      if (label === 'facebook') this.form.patchValue({ facebook: link.url });
      if (label === 'instagram') this.form.patchValue({ instagram: link.url });
    }
  }

  ngAfterViewInit(): void {
    this.initMap();
  }

  ngOnDestroy(): void {
    this.mapInstance?.remove();
  }

  private buildForm(): void {
    const detectedTimezone = Intl.DateTimeFormat().resolvedOptions().timeZone;

    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(2000)]],
      address: ['', [Validators.maxLength(200)]],
      city: ['', [Validators.maxLength(100)]],
      country: ['', [Validators.maxLength(100)]],
      timezone: [detectedTimezone, [Validators.required]],
      latitude: [null as number | null, [Validators.required, Validators.min(-90), Validators.max(90)]],
      longitude: [null as number | null, [Validators.required, Validators.min(-180), Validators.max(180)]],
      isPermanent: [true],
      website: ['', [Validators.pattern(/^https?:\/\/.+/)]],
      facebook: ['', [Validators.pattern(/^https?:\/\/.+/)]],
      instagram: ['', [Validators.pattern(/^https?:\/\/.+/)]],
      startAt: [''],
      endAt: [''],
      isFree: [true],
      price: [null as number | null, [Validators.min(0)]],
      currency: ['USD'],
      maxCapacity: [null as number | null, [Validators.min(1)]],
      isOnline: [false],
      onlineUrl: ['', [Validators.pattern(/^https?:\/\/.+/)]],
      recurrence: ['none']
    });
  }

  setListingType(isPermanent: boolean): void {
    this.form.patchValue({ isPermanent });
    this.isEventMode.set(!isPermanent);

    const latitude = this.form.get('latitude');
    const longitude = this.form.get('longitude');
    const startAt = this.form.get('startAt');

    if (isPermanent) {
      latitude?.setValidators([Validators.required, Validators.min(-90), Validators.max(90)]);
      longitude?.setValidators([Validators.required, Validators.min(-180), Validators.max(180)]);
      startAt?.clearValidators();
    } else {
      latitude?.clearValidators();
      longitude?.clearValidators();
      startAt?.setValidators([Validators.required]);
    }

    latitude?.updateValueAndValidity();
    longitude?.updateValueAndValidity();
    startAt?.updateValueAndValidity();
  }

  onGeocodeSearchInput(term: string): void {
    this.geocodeSearchTerms.next(term);
  }

  selectGeocodeResult(result: GeocodeResult, geocodeInput: HTMLInputElement): void {
    this.isGeocodeDropdownOpen.set(false);
    this.geocodeResults.set([]);
    geocodeInput.value = result.fullName;

    this.form.patchValue({
      latitude: parseFloat(result.latitude.toFixed(6)),
      longitude: parseFloat(result.longitude.toFixed(6)),
      city: result.city || this.form.get('city')?.value,
      country: result.country || this.form.get('country')?.value,
      address: result.fullName
    });

    if (!this.form.get('name')?.value) {
      this.form.patchValue({ name: result.name });
    }

    this.onLatLngInput();
  }

  onVenueSearchInput(term: string): void {
    this.venueSearchTerms.next(term);
  }

  selectVenue(venue: PlaceSummaryDto, venueInput: HTMLInputElement): void {
    this.selectedVenue.set(venue);
    this.isVenueDropdownOpen.set(false);
    venueInput.value = '';
    this.venueResults.set([]);
  }

  clearVenue(): void {
    this.selectedVenue.set(null);
  }

  private loadCategories(): void {
    this.categoriesService.loadCategories().subscribe({
      next: () => this.categories.set(this.categoriesService.categories()),
      error: () => this.toastService.error('Failed to load categories')
    });
  }

  private initMap(): void {
    if (!this.mapContainer?.nativeElement) return;

    (mapboxgl as any).accessToken = environment.mapboxToken;

    this.mapInstance = new mapboxgl.Map({
      container: this.mapContainer.nativeElement,
      style: 'mapbox://styles/mapbox/outdoors-v12',
      center: [20, 20],
      zoom: 1.5,
      minZoom: 1,
      projection: 'mercator',
      renderWorldCopies: false
    });

    this.mapInstance.addControl(new mapboxgl.NavigationControl(), 'top-right');

    this.mapInstance.on('load', () => {
      this.isMapReady.set(true);
    });

    this.mapInstance.on('click', (event) => {
      this.setLocationFromMap(event.lngLat.lat, event.lngLat.lng);
    });
  }

  private setLocationFromMap(lat: number, lng: number): void {
    this.form.patchValue({ latitude: parseFloat(lat.toFixed(6)), longitude: parseFloat(lng.toFixed(6)) });

    if (this.locationMarker) {
      this.locationMarker.setLngLat([lng, lat]);
    } else {
      this.locationMarker = new mapboxgl.Marker({ color: '#00796b' })
        .setLngLat([lng, lat])
        .addTo(this.mapInstance!);
    }
  }

  onLatLngInput(): void {
    const lat = this.form.get('latitude')?.value;
    const lng = this.form.get('longitude')?.value;

    if (lat && lng && lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180) {
      if (this.locationMarker) {
        this.locationMarker.setLngLat([lng, lat]);
      } else {
        this.locationMarker = new mapboxgl.Marker({ color: '#00796b' })
          .setLngLat([lng, lat])
          .addTo(this.mapInstance!);
      }
      this.mapInstance?.flyTo({ center: [lng, lat], zoom: 12, duration: 800 });
    }
  }

  isCategorySelected(categoryId: string): boolean {
    return this.selectedCategoryIds().includes(categoryId);
  }

  toggleCategory(categoryId: string): void {
    const current = this.selectedCategoryIds();
    if (current.includes(categoryId)) {
      this.selectedCategoryIds.set(current.filter(id => id !== categoryId));
    } else if (current.length < this.MAX_CATEGORIES) {
      this.selectedCategoryIds.set([...current, categoryId]);
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.selectedCategoryIds().length === 0) {
      this.toastService.error('Please select at least one category');
      return;
    }

    if (this.isEventMode()) {
      this.submitEvent();
      return;
    }

    this.isSubmitting.set(true);

    const formValue = this.form.value;
    const externalLinks: { label: string; url: string }[] = [];

    if (formValue.website) externalLinks.push({ label: 'Website', url: formValue.website });
    if (formValue.facebook) externalLinks.push({ label: 'Facebook', url: formValue.facebook });
    if (formValue.instagram) externalLinks.push({ label: 'Instagram', url: formValue.instagram });

    const payload = {
      name: formValue.name,
      description: formValue.description || null,
      latitude: formValue.latitude,
      longitude: formValue.longitude,
      address: formValue.address || null,
      city: formValue.city || null,
      country: formValue.country || null,
      countryCode: null,
      timezone: formValue.timezone,
      isPermanent: formValue.isPermanent,
      categoryIds: this.selectedCategoryIds(),
      externalLinks: externalLinks.length > 0 ? externalLinks : null
    };

    const editId = this.editPlaceId();
    const request = editId
      ? this.api.put<any>(`/places/${editId}`, payload)
      : this.api.post<any>('/places', payload);

    request.subscribe({
      next: (response) => {
        this.isSubmitting.set(false);
        this.toastService.success(editId
          ? 'Your changes were saved! Major edits may require a new review.'
          : 'Your listing was submitted for review! 🎉');
        const placeId = editId || response?.placeId || response?.id;
        if (placeId) {
          this.router.navigate(['/places', placeId]);
        } else {
          this.router.navigate(['/map']);
        }
      },
      error: () => {
        this.isSubmitting.set(false);
      }
    });
  }

  private submitEvent(): void {
    const formValue = this.form.value;

    if (!this.selectedVenue() && !formValue.isOnline) {
      this.toastService.error('Pick a venue or mark the event as online');
      return;
    }

    if (formValue.isOnline && !formValue.onlineUrl) {
      this.toastService.error('Please provide the online event URL');
      return;
    }

    this.isSubmitting.set(true);

    const rRule = this.buildRRule(formValue.recurrence, formValue.startAt);

    const payload = {
      placeId: this.selectedVenue()?.id || null,
      title: formValue.name,
      description: formValue.description || null,
      eventType: rRule ? 'Recurring' : 'OneTime',
      startAt: new Date(formValue.startAt).toISOString(),
      endAt: formValue.endAt ? new Date(formValue.endAt).toISOString() : null,
      timezone: formValue.timezone,
      rRule,
      price: formValue.isFree ? null : formValue.price,
      currency: formValue.isFree ? null : formValue.currency,
      isFree: formValue.isFree,
      isOnline: formValue.isOnline,
      onlineUrl: formValue.isOnline ? formValue.onlineUrl : null,
      maxCapacity: formValue.maxCapacity || null,
      categoryIds: this.selectedCategoryIds()
    };

    const editEventId = this.editEventId();
    const request = editEventId
      ? this.api.put<any>(`/events/${editEventId}`, payload)
      : this.eventsService.create(payload);

    request.subscribe({
      next: (response) => {
        this.isSubmitting.set(false);
        this.toastService.success(editEventId
          ? 'Your event was updated!'
          : 'Your event was submitted for review! 🎉');
        const eventId = editEventId || response?.eventId || response?.id;
        this.router.navigate(eventId ? ['/events', eventId] : ['/map']);
      },
      error: () => this.isSubmitting.set(false)
    });
  }

  private buildRRule(recurrence: string, startAt: string): string | null {
    if (recurrence === 'none' || !startAt) return null;

    const weekdayCodes = ['SU', 'MO', 'TU', 'WE', 'TH', 'FR', 'SA'];
    const weekday = weekdayCodes[new Date(startAt).getDay()];

    if (recurrence === 'weekly') return `FREQ=WEEKLY;BYDAY=${weekday}`;
    if (recurrence === 'monthly') {
      const weekOfMonth = Math.ceil(new Date(startAt).getDate() / 7);
      return `FREQ=MONTHLY;BYDAY=${weekOfMonth}${weekday}`;
    }
    return null;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field?.invalid && field?.touched);
  }

  getFieldError(fieldName: string): string {
    const field = this.form.get(fieldName);
    if (!field?.errors) return '';
    if (field.errors['required']) return 'This field is required';
    if (field.errors['minlength']) return `Minimum ${field.errors['minlength'].requiredLength} characters`;
    if (field.errors['maxlength']) return `Maximum ${field.errors['maxlength'].requiredLength} characters`;
    if (field.errors['pattern']) return 'Must be a valid URL starting with http:// or https://';
    return 'Invalid value';
  }
}
