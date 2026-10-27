import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { GroupsService, GroupSummaryDto, GroupPostDto } from '../../../core/services/groups.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-group-detail',
  templateUrl: './group-detail.component.html',
  styleUrls: ['./group-detail.component.scss'],
  standalone: false
})
export class GroupDetailComponent implements OnInit {
  group = signal<GroupSummaryDto | null>(null);
  posts = signal<GroupPostDto[]>([]);
  isLoading = signal(true);
  isJoining = signal(false);
  isPosting = signal(false);
  newPostBody = signal('');

  groupId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private groupsService: GroupsService,
    private toastService: ToastService,
    public authService: AuthService
  ) {
    this.groupId = this.route.snapshot.paramMap.get('id');
  }

  ngOnInit(): void {
    if (!this.groupId) {
      this.router.navigate(['/groups']);
      return;
    }
    this.loadGroup();
    this.loadPosts();
  }

  private loadGroup(): void {
    this.isLoading.set(true);
    this.groupsService.getById(this.groupId!).subscribe({
      next: (group) => {
        this.group.set(group);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.router.navigate(['/groups']);
      }
    });
  }

  private loadPosts(): void {
    this.groupsService.getPosts(this.groupId!).subscribe({
      next: (response) => this.posts.set(response.items || [])
    });
  }

  toggleMembership(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    const currentGroup = this.group();
    if (!currentGroup) return;

    this.isJoining.set(true);
    const action = currentGroup.isMember
      ? this.groupsService.leave(currentGroup.id)
      : this.groupsService.join(currentGroup.id);

    action.subscribe({
      next: (response) => {
        this.group.set({
          ...currentGroup,
          isMember: response.isMember,
          memberCount: response.memberCount
        });
        this.isJoining.set(false);
        this.toastService.success(response.isMember
          ? `Welcome to ${currentGroup.name}! 🌍`
          : `You left ${currentGroup.name}`);
      },
      error: () => this.isJoining.set(false)
    });
  }

  submitPost(): void {
    const body = this.newPostBody().trim();
    if (!body) return;

    this.isPosting.set(true);
    this.groupsService.createPost(this.groupId!, body).subscribe({
      next: (response) => {
        this.posts.set([response.post, ...this.posts()]);
        this.newPostBody.set('');
        this.isPosting.set(false);
      },
      error: () => this.isPosting.set(false)
    });
  }

  getAuthorInitials(displayName: string): string {
    return (displayName || '?')
      .split(' ')
      .map(part => part[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  goBack(): void {
    this.router.navigate(['/groups']);
  }

  viewOnMap(): void {
    this.router.navigate(['/map']);
  }
}
