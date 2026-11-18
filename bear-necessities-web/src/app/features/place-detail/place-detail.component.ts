import { Component, HostListener, OnDestroy, OnInit, signal } from '@angular/core';
import { forkJoin, of, map } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { PlacesService } from '../../core/services/places.service';
import { PlaceDetailDto } from '../../core/models/place.model';
import { AuthService } from '../../core/services/auth.service';
import { ProfileService, SaveType } from '../../core/services/profile.service';
import { ToastService } from '../../core/services/toast.service';
import { EventsService } from '../../core/services/events.service';
import { EventSummaryDto } from '../../core/models/event.model';
import { ReviewsService, ReviewDto } from '../../core/services/reviews.service';
import { environment } from '../../../environments/environment';
import { MediaService, MediaItemDto } from '../../core/services/media.service';

const MAX_REVIEW_PHOTOS = 4;
const MAX_PHOTO_BYTES = 10 * 1024 * 1024;

interface PickedPhoto {
  file: File;
  preview: string;
}

@Component({
  selector: 'app-place-detail',
  templateUrl: './place-detail.component.html',
  styleUrls: ['./place-detail.component.scss'],
  standalone: false
})
export class PlaceDetailComponent implements OnInit, OnDestroy {
  place = signal<PlaceDetailDto | null>(null);
  isRemovingPhoto = signal(false);
  isLoading = signal<boolean>(true);
  mySaveType = signal<SaveType | null>(null);
  isTogglingSave = signal<boolean>(false);
  upcomingEvents = signal<EventSummaryDto[]>([]);
  isLoadingEvents = signal<boolean>(true);

  reviews = signal<ReviewDto[]>([]);
  reviewsAverage = signal<number | null>(null);
  reviewsTotal = signal<number>(0);
  isReviewFormOpen = signal<boolean>(false);
  reviewRating = signal<number>(0);
  reviewBody = signal<string>('');
  isSubmittingReview = signal<boolean>(false);
  readonly MAX_REVIEW_PHOTOS = MAX_REVIEW_PHOTOS;
  reviewPhotos = signal<PickedPhoto[]>([]);

  galleryPhotos = signal<MediaItemDto[]>([]);
  isUploadingPhoto = signal<boolean>(false);
  // The full-screen viewer: which photos it steps through and which one is showing
  lightbox = signal<{ urls: string[]; index: number } | null>(null);

  placeId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private placesService: PlacesService,
    private profileService: ProfileService,
    private toastService: ToastService,
    private eventsService: EventsService,
    private reviewsService: ReviewsService,
    private mediaService: MediaService,
    public authService: AuthService
  ) {
    this.placeId = this.route.snapshot.paramMap.get('id');
  }

  ngOnInit(): void {
    if (!this.placeId) {
      this.router.navigate(['/map']);
      return;
    }
    this.loadPlace();
    this.loadSavedState();
    this.loadUpcomingEvents();
    this.loadReviews();
    this.loadGallery();
  }

  private loadReviews(): void {
    this.reviewsService.getByTarget('Place', this.placeId!).subscribe({
      next: (response) => {
        this.reviews.set(response.items || []);
        this.reviewsAverage.set(response.averageRating ?? null);
        this.reviewsTotal.set(response.totalCount || 0);
      }
    });
  }

  openReviewForm(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }
    this.isReviewFormOpen.set(true);
  }

  closeReviewForm(): void {
    this.isReviewFormOpen.set(false);
    this.reviewPhotos().forEach(photo => URL.revokeObjectURL(photo.preview));
    this.reviewPhotos.set([]);
    this.reviewRating.set(0);
    this.reviewBody.set('');
  }

  setReviewRating(rating: number): void {
    this.reviewRating.set(rating);
  }

  submitReview(): void {
    if (this.reviewRating() < 1) {
      this.toastService.error('Please choose a rating');
      return;
    }

    this.isSubmittingReview.set(true);
    const hasPhotos = this.reviewPhotos().length > 0;
    // Review photos join the place's gallery, so they upload there first and the review points at them
    const uploads = hasPhotos
      ? forkJoin(this.reviewPhotos().map(photo => this.mediaService.upload('Place', this.placeId!, photo.file)))
          .pipe(map(uploaded => uploaded.map(item => item.id)))
      : of([] as string[]);

    uploads.subscribe({
      next: (photoMediaIds) => this.reviewsService.create({
        targetType: 'Place',
        targetId: this.placeId!,
        rating: this.reviewRating(),
        body: this.reviewBody().trim() || undefined,
        photoMediaIds
      }).subscribe({
        next: () => {
          this.isSubmittingReview.set(false);
          this.closeReviewForm();
          this.toastService.success(hasPhotos && !this.authService.isAdmin()
            ? 'Thanks for your review! ⭐ Your photos will appear after a quick check 🐾'
            : 'Thanks for your review! ⭐');
          this.loadReviews();
          this.loadGallery();
        },
        error: () => this.isSubmittingReview.set(false)
      }),
      error: () => this.isSubmittingReview.set(false)
    });
  }

  pickReviewPhotos(fileInput: HTMLInputElement): void {
    const files = Array.from(fileInput.files ?? []);
    fileInput.value = '';
    const room = MAX_REVIEW_PHOTOS - this.reviewPhotos().length;
    const accepted = files.filter(file => this.isUsablePhoto(file)).slice(0, room);
    if (files.length > room) this.toastService.error(`Up to ${MAX_REVIEW_PHOTOS} photos per review`);
    this.reviewPhotos.set([
      ...this.reviewPhotos(),
      ...accepted.map(file => ({ file, preview: URL.createObjectURL(file) }))
    ]);
  }

  removeReviewPhoto(index: number): void {
    const photos = [...this.reviewPhotos()];
    const [removed] = photos.splice(index, 1);
    if (removed) URL.revokeObjectURL(removed.preview);
    this.reviewPhotos.set(photos);
  }

  addGalleryPhoto(fileInput: HTMLInputElement): void {
    const file = fileInput.files?.[0];
    fileInput.value = '';
    if (!file || !this.isUsablePhoto(file)) return;

    this.isUploadingPhoto.set(true);
    this.mediaService.upload('Place', this.placeId!, file).subscribe({
      next: () => {
        this.isUploadingPhoto.set(false);
        if (this.authService.isAdmin()) {
          this.toastService.success('Photo added to the gallery 📸');
          this.loadGallery();
        } else {
          this.toastService.success('Thanks! Your photo will appear after a quick check 🐾');
        }
      },
      error: () => this.isUploadingPhoto.set(false)
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  photoUrl(mediaId: string): string {
    return this.mediaService.contentUrl(mediaId);
  }

  openGallery(index: number): void {
    this.lightbox.set({ urls: this.galleryPhotos().map(photo => photo.url), index });
  }

  openReviewPhotos(review: ReviewDto, index: number): void {
    this.lightbox.set({ urls: (review.photoIds ?? []).map(id => this.photoUrl(id)), index });
  }

  closeLightbox(): void {
    this.lightbox.set(null);
  }

  stepLightbox(direction: number): void {
    const current = this.lightbox();
    if (!current || current.urls.length < 2) return;
    const index = (current.index + direction + current.urls.length) % current.urls.length;
    this.lightbox.set({ ...current, index });
  }

  @HostListener('document:keydown', ['$event'])
  onLightboxKey(keyEvent: KeyboardEvent): void {
    if (!this.lightbox()) return;
    if (keyEvent.key === 'Escape') this.closeLightbox();
    if (keyEvent.key === 'ArrowRight') this.stepLightbox(1);
    if (keyEvent.key === 'ArrowLeft') this.stepLightbox(-1);
  }

  ngOnDestroy(): void {
    this.reviewPhotos().forEach(photo => URL.revokeObjectURL(photo.preview));
  }

  private loadGallery(): void {
    this.mediaService.getByOwner('Place', this.placeId!).subscribe({
      next: (response) => this.galleryPhotos.set(response.items || [])
    });
  }

  private isUsablePhoto(file: File): boolean {
    if (!file.type.startsWith('image/')) {
      this.toastService.error('That doesn\'t look like a photo');
      return false;
    }
    if (file.size > MAX_PHOTO_BYTES) {
      this.toastService.error('Please pick a photo under 10 MB');
      return false;
    }
    return true;
  }

  // Anyone may suggest an edit; the form sends it to review unless an admin is editing
  editPlace(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/submit'], { queryParams: { edit: this.placeId } });
  }

  private loadUpcomingEvents(): void {
    this.isLoadingEvents.set(true);
    this.eventsService.getByPlace(this.placeId!).subscribe({
      next: (response) => {
        this.upcomingEvents.set(response?.items || []);
        this.isLoadingEvents.set(false);
      },
      error: () => this.isLoadingEvents.set(false)
    });
  }

  openEvent(eventSummary: EventSummaryDto): void {
    this.router.navigate(['/events', eventSummary.id]);
  }

  getEventPriceLabel(eventSummary: EventSummaryDto): string {
    if (eventSummary.isFree) return 'Free';
    if (eventSummary.price == null) return '';
    return `${eventSummary.price} ${eventSummary.currency || ''}`.trim();
  }

  private loadSavedState(): void {
    if (!this.authService.isAuthenticated()) return;

    this.profileService.getSavedItems('Place').subscribe({
      next: (response) => {
        const mine = (response.items || []).find(item => item.targetId === this.placeId);
        this.mySaveType.set(mine?.saveType || null);
      }
    });
  }

  setSaveType(saveType: SaveType): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    this.isTogglingSave.set(true);
    this.profileService.toggleSave('Place', this.placeId!, saveType).subscribe({
      next: (response) => {
        this.mySaveType.set(response.saveType);
        this.isTogglingSave.set(false);

        const messages: Record<SaveType, string> = {
          WantToGo: 'Added to your "Want to go" list 🌟',
          Visited: 'Marked as visited ✔️',
          VisitedLoved: 'Marked as visited & loved ❤️'
        };
        this.toastService.success(response.isSaved ? messages[saveType] : 'Removed from your list');
      },
      error: () => this.isTogglingSave.set(false)
    });
  }

  isSaveType(saveType: SaveType): boolean {
    return this.mySaveType() === saveType;
  }

  private loadPlace(): void {
    this.isLoading.set(true);
    this.placesService.getById(this.placeId!).subscribe({
      next: (response: any) => {
        const placeData = response.place || response;
        this.place.set(placeData);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load place:', err);
        this.isLoading.set(false);
        this.router.navigate(['/map']);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/map']);
  }

  getDirectionsUrl(): string {
    const place = this.place();
    if (!place) return '';
    return `https://www.google.com/maps/search/?api=1&query=${place.latitude},${place.longitude}`;
  }

  removePhoto(): void {
    if (!this.placeId || !confirm('Remove this photo? The place will show its category image instead.')) return;
    this.isRemovingPhoto.set(true);
    this.placesService.removePhoto(this.placeId).subscribe({
      next: () => {
        this.place.update(place => place && { ...place, photoUrl: undefined, photoCredit: undefined, photoCreditUrl: undefined });
        this.isRemovingPhoto.set(false);
        this.toastService.success('Photo removed');
      },
      error: () => {
        this.isRemovingPhoto.set(false);
        this.toastService.error('Could not remove the photo');
      }
    });
  }

  getHeroImageUrl(): string {
    const place = this.place();
    if (!place?.media || place.media.length === 0) {
      return place?.photoUrl ?? '';
    }
    // Serve through our own API to avoid the public bucket's TLS quirks
    return `${environment.apiUrl}/media/${place.media[0].id}/content`;
  }

  hasExternalLinks(): boolean {
    const place = this.place();
    return !!(place?.website || place?.facebook || place?.instagram);
  }

  getStarRating(rating: number | undefined): number[] {
    if (!rating) return [];
    return Array.from({ length: 5 }, (_, i) => i < Math.round(rating) ? 1 : 0);
  }
}

