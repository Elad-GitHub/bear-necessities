import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ProfileService, MyRsvpDto, SavedItemDto } from '../../core/services/profile.service';
import { ToastService } from '../../core/services/toast.service';

type ProfileTab = 'rsvps' | 'saved' | 'listings';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
  standalone: false
})
export class ProfileComponent implements OnInit {
  activeTab = signal<ProfileTab>('rsvps');
  rsvps = signal<MyRsvpDto[]>([]);
  savedItems = signal<SavedItemDto[]>([]);
  myListings = signal<any[]>([]);
  myEventListings = signal<any[]>([]);
  isLoadingRsvps = signal(true);
  isLoadingSaved = signal(true);
  isLoadingListings = signal(true);

  constructor(
    public authService: AuthService,
    private profileService: ProfileService,
    private toastService: ToastService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const requestedTab = this.route.snapshot.queryParamMap.get('tab');
    if (requestedTab === 'saved' || requestedTab === 'rsvps' || requestedTab === 'listings') {
      this.activeTab.set(requestedTab);
    }
    this.loadRsvps();
    this.loadSavedItems();
    this.loadMyListings();
  }

  private loadMyListings(): void {
    this.isLoadingListings.set(true);
    this.profileService.getMyPlaces().subscribe({
      next: (response) => {
        this.myListings.set(response.items || []);
        this.isLoadingListings.set(false);
      },
      error: () => this.isLoadingListings.set(false)
    });

    this.profileService.getMyEvents().subscribe({
      next: (response) => this.myEventListings.set(response.items || [])
    });
  }

  totalListings(): number {
    return this.myListings().length + this.myEventListings().length;
  }

  deleteListing(listing: any, clickEvent: Event): void {
    clickEvent.stopPropagation();
    if (!confirm(`Delete "${listing.name}"? This cannot be undone.`)) return;

    this.profileService.deletePlace(listing.id).subscribe({
      next: () => {
        this.myListings.set(this.myListings().filter(item => item.id !== listing.id));
        this.toastService.success('Listing deleted');
      }
    });
  }

  openEventListing(eventListing: any): void {
    this.router.navigate(['/events', eventListing.id]);
  }

  editEventListing(eventListing: any, clickEvent: Event): void {
    clickEvent.stopPropagation();
    this.router.navigate(['/submit'], { queryParams: { editEvent: eventListing.id } });
  }

  cancelEventListing(eventListing: any, clickEvent: Event): void {
    clickEvent.stopPropagation();
    if (!confirm(`Cancel "${eventListing.title}"?`)) return;

    this.profileService.cancelEvent(eventListing.id).subscribe({
      next: () => {
        this.myEventListings.set(
          this.myEventListings().filter(item => item.id !== eventListing.id)
        );
        this.toastService.success('Event cancelled');
      }
    });
  }

  openListing(listing: any): void {
    this.router.navigate(['/places', listing.id]);
  }

  editListing(listing: any, clickEvent: Event): void {
    clickEvent.stopPropagation();
    this.router.navigate(['/submit'], { queryParams: { edit: listing.id } });
  }

  getListingStatusClass(status: string): string {
    return `listing-${(status || '').toLowerCase()}`;
  }

  getListingStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      Live: 'Live',
      PendingReview: 'Pending Review',
      Suspended: 'Rejected',
      Draft: 'Draft',
      Archived: 'Archived',
      Cancelled: 'Cancelled'
    };
    return labels[status] || status;
  }

  setTab(tab: ProfileTab): void {
    this.activeTab.set(tab);
  }

  private loadRsvps(): void {
    this.isLoadingRsvps.set(true);
    this.profileService.getMyRsvps().subscribe({
      next: (response) => {
        this.rsvps.set(response.items || []);
        this.isLoadingRsvps.set(false);
      },
      error: () => this.isLoadingRsvps.set(false)
    });
  }

  private loadSavedItems(): void {
    this.isLoadingSaved.set(true);
    this.profileService.getSavedItems().subscribe({
      next: (response) => {
        this.savedItems.set(response.items || []);
        this.isLoadingSaved.set(false);
      },
      error: () => this.isLoadingSaved.set(false)
    });
  }

  cancelRsvp(rsvp: MyRsvpDto, clickEvent: Event): void {
    clickEvent.stopPropagation();
    this.profileService.cancelRsvp(rsvp.id).subscribe({
      next: () => {
        this.rsvps.set(this.rsvps().filter(item => item.id !== rsvp.id));
        this.toastService.success('RSVP cancelled');
      }
    });
  }

  unsaveItem(saved: SavedItemDto, clickEvent: Event): void {
    clickEvent.stopPropagation();
    this.profileService.toggleSave(saved.targetType, saved.targetId).subscribe({
      next: () => {
        this.savedItems.set(
          this.savedItems().filter(item => item.targetId !== saved.targetId)
        );
        this.toastService.success('Removed from saved');
      }
    });
  }

  openRsvpEvent(rsvp: MyRsvpDto): void {
    this.router.navigate(['/events', rsvp.eventId]);
  }

  openSavedItem(saved: SavedItemDto): void {
    const route = saved.targetType === 'Event' ? '/events' : '/places';
    this.router.navigate([route, saved.targetId]);
  }

  getInitials(): string {
    const user = this.authService.currentUser();
    if (!user?.displayName) return '';
    return user.displayName
      .split(' ')
      .map(part => part[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  getMemberSince(): string {
    const createdAt = this.authService.currentUser()?.createdAt;
    if (!createdAt) return '';
    return new Date(createdAt).toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  }

  getRsvpStatusClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }

  getSaveTypeLabel(saveType: string): string {
    const labels: Record<string, string> = {
      WantToGo: '★ Want to go',
      Visited: '✓ Been there',
      VisitedLoved: '♥ Loved it'
    };
    return labels[saveType] || saveType;
  }

  logout(): void {
    this.authService.logout();
  }
}
