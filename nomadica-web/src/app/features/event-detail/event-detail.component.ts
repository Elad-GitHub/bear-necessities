import { Component, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { EventsService } from '../../core/services/events.service';
import { AuthService } from '../../core/services/auth.service';
import { ProfileService } from '../../core/services/profile.service';
import { ToastService } from '../../core/services/toast.service';
import { EventDetailDto, RsvpStatus } from '../../core/models/event.model';

@Component({
  selector: 'app-event-detail',
  templateUrl: './event-detail.component.html',
  styleUrls: ['./event-detail.component.scss'],
  standalone: false
})
export class EventDetailComponent implements OnInit {
  event = signal<EventDetailDto | null>(null);
  isLoading = signal(true);
  myRsvpId = signal<string | null>(null);
  myRsvpStatus = signal<RsvpStatus | null>(null);
  isSubmittingRsvp = signal(false);
  partySize = signal(1);

  hasRsvp = computed(() => this.myRsvpId() !== null);

  eventId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private eventsService: EventsService,
    private profileService: ProfileService,
    private toastService: ToastService,
    public authService: AuthService
  ) {
    this.eventId = this.route.snapshot.paramMap.get('id');
  }

  ngOnInit(): void {
    if (!this.eventId) {
      this.router.navigate(['/map']);
      return;
    }
    this.loadEvent();
    this.loadMyRsvp();
  }

  private loadEvent(): void {
    this.isLoading.set(true);
    this.eventsService.getById(this.eventId!).subscribe({
      next: (response) => {
        this.event.set(response?.event || response);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.router.navigate(['/map']);
      }
    });
  }

  private loadMyRsvp(): void {
    if (!this.authService.isAuthenticated()) return;

    this.profileService.getMyRsvps().subscribe({
      next: (response) => {
        const existing = (response.items || []).find(rsvp => rsvp.eventId === this.eventId);
        if (existing) {
          this.myRsvpId.set(existing.id);
          this.myRsvpStatus.set(existing.status as RsvpStatus);
          this.partySize.set(existing.partySize);
        }
      }
    });
  }

  setRsvp(status: RsvpStatus): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    this.isSubmittingRsvp.set(true);
    this.eventsService.createOrUpdateRsvp({
      eventId: this.eventId!,
      status,
      partySize: this.partySize()
    }).subscribe({
      next: (response) => {
        this.myRsvpId.set(response.rsvpId);
        this.myRsvpStatus.set(status);
        this.isSubmittingRsvp.set(false);
        const messages: Record<RsvpStatus, string> = {
          Going: "You're going! 🎉",
          Maybe: 'Marked as maybe',
          NotGoing: 'Marked as not going'
        };
        this.toastService.success(messages[status]);
      },
      error: () => this.isSubmittingRsvp.set(false)
    });
  }

  cancelRsvp(): void {
    const rsvpId = this.myRsvpId();
    if (!rsvpId) return;

    this.isSubmittingRsvp.set(true);
    this.eventsService.cancelRsvp(rsvpId).subscribe({
      next: () => {
        this.myRsvpId.set(null);
        this.myRsvpStatus.set(null);
        this.isSubmittingRsvp.set(false);
        this.toastService.success('RSVP cancelled');
      },
      error: () => this.isSubmittingRsvp.set(false)
    });
  }

  changePartySize(delta: number): void {
    const updated = Math.max(1, Math.min(20, this.partySize() + delta));
    this.partySize.set(updated);
  }

  isStatus(status: RsvpStatus): boolean {
    return this.myRsvpStatus() === status;
  }

  goBack(): void {
    this.router.navigate(['/map']);
  }

  goToPlace(): void {
    const placeId = this.event()?.placeId;
    if (placeId) this.router.navigate(['/places', placeId]);
  }

  getPriceLabel(): string {
    const eventDetail = this.event();
    if (!eventDetail) return '';
    if (eventDetail.isFree) return 'Free';
    if (eventDetail.price == null) return 'Price TBA';
    return `${eventDetail.price} ${eventDetail.currency || ''}`.trim();
  }

  hasCalendarDate(): boolean {
    return !!this.event()?.startAt;
  }

  addToGoogleCalendar(): void {
    const eventDetail = this.event();
    if (!eventDetail?.startAt) return;

    const start = new Date(eventDetail.startAt);
    // Default to a 2-hour block when the event has no explicit end time
    const end = eventDetail.endAt
      ? new Date(eventDetail.endAt)
      : new Date(start.getTime() + 2 * 60 * 60 * 1000);

    const dates = `${this.toCalendarStamp(start)}/${this.toCalendarStamp(end)}`;
    const params = new URLSearchParams({
      action: 'TEMPLATE',
      text: eventDetail.title,
      dates,
      details: eventDetail.description || 'See you there! — via HippieLand',
      location: eventDetail.onlineUrl || ''
    });

    window.open(`https://calendar.google.com/calendar/render?${params.toString()}`, '_blank');
  }

  private toCalendarStamp(date: Date): string {
    // Google Calendar expects UTC basic format: YYYYMMDDTHHMMSSZ
    return date.toISOString().replace(/[-:]/g, '').replace(/\.\d{3}/, '');
  }
}
