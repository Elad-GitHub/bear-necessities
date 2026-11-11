import { Component, OnInit, signal } from '@angular/core';
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

@Component({
  selector: 'app-place-detail',
  templateUrl: './place-detail.component.html',
  styleUrls: ['./place-detail.component.scss'],
  standalone: false
})
export class PlaceDetailComponent implements OnInit {
  place = signal<PlaceDetailDto | null>(null);
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

  placeId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private placesService: PlacesService,
    private profileService: ProfileService,
    private toastService: ToastService,
    private eventsService: EventsService,
    private reviewsService: ReviewsService,
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
    this.reviewsService.create({
      targetType: 'Place',
      targetId: this.placeId!,
      rating: this.reviewRating(),
      body: this.reviewBody().trim() || undefined
    }).subscribe({
      next: () => {
        this.isSubmittingReview.set(false);
        this.closeReviewForm();
        this.toastService.success('Thanks for your review! ⭐');
        this.loadReviews();
      },
      error: () => this.isSubmittingReview.set(false)
    });
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

