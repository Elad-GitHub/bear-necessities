import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { GroupsService, GroupSummaryDto } from '../../core/services/groups.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { GeocodingService, GeocodeResult } from '../../core/services/geocoding.service';

@Component({
  selector: 'app-groups',
  templateUrl: './groups.component.html',
  styleUrls: ['./groups.component.scss'],
  standalone: false
})
export class GroupsComponent implements OnInit {
  groups = signal<GroupSummaryDto[]>([]);
  isLoading = signal(true);
  isCreateOpen = signal(false);
  isCreating = signal(false);

  createForm!: FormGroup;
  selectedLocation = signal<GeocodeResult | null>(null);
  locationResults = signal<GeocodeResult[]>([]);
  private locationSearchTerms = new Subject<string>();
  private listSearchTerms = new Subject<string>();

  constructor(
    private groupsService: GroupsService,
    private geocodingService: GeocodingService,
    private toastService: ToastService,
    private fb: FormBuilder,
    private router: Router,
    public authService: AuthService
  ) {
    this.locationSearchTerms.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(term => {
      if (term.trim().length < 2) {
        this.locationResults.set([]);
        return;
      }
      this.geocodingService.search(term, 5).subscribe(results => this.locationResults.set(results));
    });

    this.listSearchTerms.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(term => this.loadGroups(term));
  }

  ngOnInit(): void {
    this.createForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(120)]],
      description: ['', [Validators.maxLength(2000)]]
    });

    this.loadGroups();
  }

  private loadGroups(query?: string): void {
    this.isLoading.set(true);
    this.groupsService.getGroups(query).subscribe({
      next: (response) => {
        this.groups.set(response.items || []);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  onListSearch(term: string): void {
    this.listSearchTerms.next(term);
  }

  openGroup(group: GroupSummaryDto): void {
    this.router.navigate(['/groups', group.id]);
  }

  toggleCreate(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }
    this.isCreateOpen.set(!this.isCreateOpen());
  }

  onLocationSearch(term: string): void {
    this.locationSearchTerms.next(term);
  }

  selectLocation(result: GeocodeResult, locationInput: HTMLInputElement): void {
    this.selectedLocation.set(result);
    this.locationResults.set([]);
    locationInput.value = result.fullName;
  }

  clearLocation(locationInput: HTMLInputElement): void {
    this.selectedLocation.set(null);
    locationInput.value = '';
  }

  createGroup(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const location = this.selectedLocation();
    if (!location) {
      this.toastService.error('Please pick the area this community is about');
      return;
    }

    this.isCreating.set(true);
    this.groupsService.create({
      name: this.createForm.value.name,
      description: this.createForm.value.description || undefined,
      city: location.city || location.name,
      country: location.country,
      latitude: location.latitude,
      longitude: location.longitude
    }).subscribe({
      next: (response) => {
        this.isCreating.set(false);
        this.toastService.success(`"${response.group.name}" created! You are the moderator 🌍`);
        this.router.navigate(['/groups', response.group.id]);
      },
      error: () => this.isCreating.set(false)
    });
  }
}
