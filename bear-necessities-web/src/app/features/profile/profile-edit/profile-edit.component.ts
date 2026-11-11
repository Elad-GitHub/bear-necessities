import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, of, map, switchMap } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { MediaService } from '../../../core/services/media.service';
import { ProfileService } from '../../../core/services/profile.service';
import { ToastService } from '../../../core/services/toast.service';
import { resolveMediaUrl } from '../../../core/utils/media-url';

const MAX_NAME_LENGTH = 100;
const MAX_BIO_LENGTH = 500;
const MAX_PHOTO_BYTES = 10 * 1024 * 1024;

/** "Bear-ly Me": the member edits their name, photo and a few words about themselves. */
@Component({
  selector: 'app-profile-edit',
  templateUrl: './profile-edit.component.html',
  styleUrls: ['./profile-edit.component.scss'],
  standalone: false
})
export class ProfileEditComponent implements OnInit, OnDestroy {
  readonly MAX_NAME_LENGTH = MAX_NAME_LENGTH;
  readonly MAX_BIO_LENGTH = MAX_BIO_LENGTH;

  displayName = signal('');
  bio = signal('');
  email = signal('');
  isLoading = signal(true);
  isSaving = signal(false);

  // The photo shown in the preview: a newly picked file, the saved photo, or none
  private savedAvatarUrl = signal<string | null>(null);
  private pickedPhoto = signal<File | null>(null);
  private pickedPhotoPreview = signal<string | null>(null);
  private isPhotoRemoved = signal(false);

  photoPreview = computed(() =>
    this.pickedPhotoPreview() ?? (this.isPhotoRemoved() ? null : resolveMediaUrl(this.savedAvatarUrl())));
  canSave = computed(() => this.displayName().trim().length > 0 && !this.isSaving());

  constructor(
    private authService: AuthService,
    private profileService: ProfileService,
    private mediaService: MediaService,
    private toastService: ToastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.profileService.getMyProfile().subscribe({
      next: (profile) => {
        this.displayName.set(profile.displayName);
        this.bio.set(profile.bio ?? '');
        this.email.set(profile.email);
        this.savedAvatarUrl.set(profile.avatarUrl ?? null);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  ngOnDestroy(): void {
    this.releasePreview();
  }

  onPhotoPicked(fileInput: HTMLInputElement): void {
    const file = fileInput.files?.[0];
    fileInput.value = '';
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      this.toastService.error('That doesn\'t look like a photo');
      return;
    }
    if (file.size > MAX_PHOTO_BYTES) {
      this.toastService.error('Please pick a photo under 10 MB');
      return;
    }
    this.releasePreview();
    this.pickedPhoto.set(file);
    this.pickedPhotoPreview.set(URL.createObjectURL(file));
    this.isPhotoRemoved.set(false);
  }

  removePhoto(): void {
    this.releasePreview();
    this.pickedPhoto.set(null);
    this.isPhotoRemoved.set(true);
  }

  save(): void {
    if (!this.canSave()) return;
    const userId = this.authService.currentUser()?.id;
    if (!userId) return;

    this.isSaving.set(true);
    this.uploadPickedPhoto(userId).pipe(
      switchMap(avatarMediaId => this.profileService.updateMyProfile({
        displayName: this.displayName().trim(),
        bio: this.bio().trim() || null,
        avatarMediaId,
        removeAvatar: this.isPhotoRemoved()
      }))
    ).subscribe({
      next: (profile) => {
        this.authService.updateCurrentUser({ displayName: profile.displayName, avatarUrl: profile.avatarUrl ?? undefined });
        this.isSaving.set(false);
        this.toastService.success('Looking good! 🐻');
        this.router.navigate(['/profile']);
      },
      error: () => this.isSaving.set(false)
    });
  }

  cancel(): void {
    this.router.navigate(['/profile']);
  }

  private uploadPickedPhoto(userId: string): Observable<string | null> {
    const photo = this.pickedPhoto();
    return photo ? this.mediaService.upload('User', userId, photo).pipe(map(uploaded => uploaded.id)) : of(null);
  }

  private releasePreview(): void {
    const preview = this.pickedPhotoPreview();
    if (preview) URL.revokeObjectURL(preview);
    this.pickedPhotoPreview.set(null);
  }
}
