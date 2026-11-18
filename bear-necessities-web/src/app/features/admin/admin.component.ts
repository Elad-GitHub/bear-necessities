import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AdminService } from '../../core/services/admin.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { PlaceSummaryDto } from '../../core/models/place.model';
import { EditSuggestionDto } from '../../core/models/edit-suggestion.model';
import { PendingPhotoDto } from '../../core/services/admin.service';
import { MediaService } from '../../core/services/media.service';

type ModerationTab = 'places' | 'events' | 'edits' | 'photos' | 'members';

@Component({
  selector: 'app-admin',
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.scss'],
  standalone: false
})
export class AdminComponent implements OnInit {
  activeTab = signal<ModerationTab>('places');
  pendingPlaces = signal<PlaceSummaryDto[]>([]);
  pendingEvents = signal<any[]>([]);
  pendingMembers = signal<any[]>([]);
  pendingEdits = signal<EditSuggestionDto[]>([]);
  pendingPhotos = signal<PendingPhotoDto[]>([]);
  isLoading = signal(true);
  rejectingId = signal<string | null>(null);
  rejectReason = signal('');
  processingIds = signal<Set<string>>(new Set());

  constructor(
    private adminService: AdminService,
    private toastService: ToastService,
    private mediaService: MediaService,
    private router: Router,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    if (!this.isAdmin()) {
      this.router.navigate(['/map']);
      return;
    }
    this.loadPending();
  }

  isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  setTab(tab: ModerationTab): void {
    this.activeTab.set(tab);
    this.cancelReject();
  }

  totalPending(): number {
    return this.pendingPlaces().length + this.pendingEvents().length + this.pendingEdits().length + this.pendingPhotos().length + this.pendingMembers().length;
  }

  private loadPending(): void {
    this.isLoading.set(true);

    this.adminService.getPendingPlaces().subscribe({
      next: (response) => {
        this.pendingPlaces.set(response.items || []);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.adminService.getPendingEvents().subscribe({
      next: (response) => this.pendingEvents.set(response.items || [])
    });

    this.adminService.getPendingEdits().subscribe({
      next: (response) => this.pendingEdits.set(response.items || [])
    });

    this.adminService.getPendingPhotos().subscribe({
      next: (response) => this.pendingPhotos.set(response.items || [])
    });

    this.adminService.getPendingMembers().subscribe({
      next: (response) => this.pendingMembers.set(response.items || [])
    });
  }

  approveMember(member: any): void {
    this.markProcessing(member.id, true);
    this.adminService.approveMember(member.id).subscribe({
      next: () => {
        this.pendingMembers.set(this.pendingMembers().filter(item => item.id !== member.id));
        this.toastService.success(`${member.displayName} is now a member! 🌍`);
      },
      error: () => this.markProcessing(member.id, false)
    });
  }

  rejectMember(member: any): void {
    if (!confirm(`Reject membership for ${member.displayName}?`)) return;

    this.markProcessing(member.id, true);
    this.adminService.rejectMember(member.id).subscribe({
      next: () => {
        this.pendingMembers.set(this.pendingMembers().filter(item => item.id !== member.id));
        this.toastService.success(`${member.displayName} rejected`);
      },
      error: () => this.markProcessing(member.id, false)
    });
  }

  approvePlace(place: PlaceSummaryDto): void {
    this.markProcessing(place.id, true);
    this.adminService.approvePlace(place.id).subscribe({
      next: () => {
        this.pendingPlaces.set(this.pendingPlaces().filter(item => item.id !== place.id));
        this.toastService.success(`"${place.name}" approved and is now live!`);
      },
      error: () => this.markProcessing(place.id, false)
    });
  }

  approveEvent(eventItem: any): void {
    this.markProcessing(eventItem.id, true);
    this.adminService.approveEvent(eventItem.id).subscribe({
      next: () => {
        this.pendingEvents.set(this.pendingEvents().filter(item => item.id !== eventItem.id));
        this.toastService.success(`"${eventItem.title}" approved and is now live!`);
      },
      error: () => this.markProcessing(eventItem.id, false)
    });
  }

  approveEdit(suggestion: EditSuggestionDto): void {
    this.markProcessing(suggestion.id, true);
    this.adminService.approveEdit(suggestion.id).subscribe({
      next: () => {
        this.removeEdit(suggestion.id);
        this.toastService.success(`Edit to "${suggestion.targetName}" is live`);
      },
      error: () => this.markProcessing(suggestion.id, false)
    });
  }

  rejectEdit(suggestion: EditSuggestionDto): void {
    this.markProcessing(suggestion.id, true);
    this.adminService.rejectEdit(suggestion.id).subscribe({
      next: () => {
        this.removeEdit(suggestion.id);
        this.toastService.success(`Edit to "${suggestion.targetName}" rejected`);
      },
      error: () => this.markProcessing(suggestion.id, false)
    });
  }

  photoUrl(photo: PendingPhotoDto): string {
    return this.mediaService.contentUrl(photo.id);
  }

  approvePhoto(photo: PendingPhotoDto): void {
    this.markProcessing(photo.id, true);
    this.adminService.approvePhoto(photo.id).subscribe({
      next: () => {
        this.removePhoto(photo.id);
        this.toastService.success(`Photo added to ${photo.placeName} 📸`);
      },
      error: () => this.markProcessing(photo.id, false)
    });
  }

  rejectPhoto(photo: PendingPhotoDto): void {
    this.markProcessing(photo.id, true);
    this.adminService.rejectPhoto(photo.id).subscribe({
      next: () => {
        this.removePhoto(photo.id);
        this.toastService.success('Photo removed');
      },
      error: () => this.markProcessing(photo.id, false)
    });
  }

  viewPhotoPlace(photo: PendingPhotoDto): void {
    this.router.navigate(['/places', photo.placeId]);
  }

  private removePhoto(photoId: string): void {
    this.pendingPhotos.set(this.pendingPhotos().filter(item => item.id !== photoId));
  }

  viewEditTarget(suggestion: EditSuggestionDto): void {
    this.router.navigate([suggestion.targetType === 'Place' ? '/places' : '/events', suggestion.targetId]);
  }

  private removeEdit(suggestionId: string): void {
    this.pendingEdits.set(this.pendingEdits().filter(item => item.id !== suggestionId));
  }

  openRejectForm(id: string): void {
    this.rejectingId.set(id);
    this.rejectReason.set('');
  }

  cancelReject(): void {
    this.rejectingId.set(null);
    this.rejectReason.set('');
  }

  confirmRejectPlace(place: PlaceSummaryDto): void {
    const reason = this.rejectReason().trim();
    if (!reason) {
      this.toastService.error('Please provide a rejection reason');
      return;
    }

    this.markProcessing(place.id, true);
    this.adminService.rejectPlace(place.id, reason).subscribe({
      next: () => {
        this.pendingPlaces.set(this.pendingPlaces().filter(item => item.id !== place.id));
        this.cancelReject();
        this.toastService.success(`"${place.name}" rejected`);
      },
      error: () => this.markProcessing(place.id, false)
    });
  }

  confirmRejectEvent(eventItem: any): void {
    const reason = this.rejectReason().trim();
    if (!reason) {
      this.toastService.error('Please provide a rejection reason');
      return;
    }

    this.markProcessing(eventItem.id, true);
    this.adminService.rejectEvent(eventItem.id, reason).subscribe({
      next: () => {
        this.pendingEvents.set(this.pendingEvents().filter(item => item.id !== eventItem.id));
        this.cancelReject();
        this.toastService.success(`"${eventItem.title}" rejected`);
      },
      error: () => this.markProcessing(eventItem.id, false)
    });
  }

  isProcessing(id: string): boolean {
    return this.processingIds().has(id);
  }

  isRejecting(id: string): boolean {
    return this.rejectingId() === id;
  }

  viewPlace(place: PlaceSummaryDto): void {
    this.router.navigate(['/places', place.id]);
  }

  viewEvent(eventItem: any): void {
    this.router.navigate(['/events', eventItem.id]);
  }

  private markProcessing(id: string, processing: boolean): void {
    const ids = new Set(this.processingIds());
    if (processing) ids.add(id); else ids.delete(id);
    this.processingIds.set(ids);
  }
}
