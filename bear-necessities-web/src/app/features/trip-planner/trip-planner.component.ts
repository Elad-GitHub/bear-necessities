import { Component, OnInit, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { TripsService } from '../../core/services/trips.service';
import { CategoriesService } from '../../core/services/categories.service';
import { CategoryIconsService } from '../../core/services/category-icons.service';
import { GeocodingService, GeocodeResult } from '../../core/services/geocoding.service';
import { PlaceSummaryDto } from '../../core/models/place.model';
import { EventSummaryDto } from '../../core/models/event.model';
import { CategoryTreeDto } from '../../core/models/category.model';

interface CategoryGroup {
  parentName: string;
  icon: string;
  places: PlaceSummaryDto[];
}

@Component({
  selector: 'app-trip-planner',
  templateUrl: './trip-planner.component.html',
  styleUrls: ['./trip-planner.component.scss'],
  standalone: false
})
export class TripPlannerComponent implements OnInit {
  selectedArea = signal<GeocodeResult | null>(null);
  areaResults = signal<GeocodeResult[]>([]);
  selectedCategoryIds = signal<string[]>([]);

  places = signal<PlaceSummaryDto[]>([]);
  events = signal<EventSummaryDto[]>([]);
  hasSearched = signal(false);
  isLoading = signal(false);

  private areaSearchTerms = new Subject<string>();
  private categoryIdToParent = new Map<string, CategoryTreeDto>();

  groupedPlaces = computed<CategoryGroup[]>(() => {
    const groups = new Map<string, CategoryGroup>();

    for (const place of this.places()) {
      const parent = this.findParentCategory(place.categoryIds?.[0]);
      const key = parent?.name || 'Other';

      if (!groups.has(key)) {
        groups.set(key, {
          parentName: key,
          icon: this.categoryIcons.getIconForParentSlug(parent?.slug),
          places: []
        });
      }
      groups.get(key)!.places.push(place);
    }

    return Array.from(groups.values()).sort((a, b) => b.places.length - a.places.length);
  });

  constructor(
    private tripsService: TripsService,
    private geocodingService: GeocodingService,
    private categoryIcons: CategoryIconsService,
    public categoriesService: CategoriesService,
    private router: Router
  ) {
    this.areaSearchTerms.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(term => {
      if (term.trim().length < 2) {
        this.areaResults.set([]);
        return;
      }
      this.geocodingService.search(term, 5).subscribe(results => this.areaResults.set(results));
    });
  }

  ngOnInit(): void {
    this.categoriesService.loadCategories().subscribe({
      next: () => this.buildCategoryIndex()
    });
  }

  private buildCategoryIndex(): void {
    const parents = this.categoriesService.categories();
    this.categoryIcons.buildIndex(parents);
    this.categoryIdToParent.clear();

    for (const parent of parents) {
      this.categoryIdToParent.set(parent.id, parent);
      for (const child of parent.children || []) {
        this.categoryIdToParent.set(child.id, parent);
      }
    }
  }

  private findParentCategory(categoryId?: string): CategoryTreeDto | undefined {
    if (!categoryId) return undefined;
    return this.categoryIdToParent.get(categoryId);
  }

  onAreaSearch(term: string): void {
    this.areaSearchTerms.next(term);
  }

  selectArea(result: GeocodeResult, areaInput: HTMLInputElement): void {
    this.selectedArea.set(result);
    this.areaResults.set([]);
    areaInput.value = result.fullName;
  }

  clearArea(areaInput: HTMLInputElement): void {
    this.selectedArea.set(null);
    areaInput.value = '';
    this.hasSearched.set(false);
    this.places.set([]);
    this.events.set([]);
  }

  onCategorySelectionChange(categoryIds: string[]): void {
    this.selectedCategoryIds.set(categoryIds);
    if (this.hasSearched()) this.planTrip();
  }

  planTrip(): void {
    const area = this.selectedArea();
    if (!area) return;

    this.isLoading.set(true);
    this.hasSearched.set(true);

    // A whole country is searched by name; cities and regions within a radius around their center
    const isCountryResult = !!area.isCountry;
    const options = isCountryResult
      ? { country: area.name, categoryIds: this.selectedCategoryIds() }
      : { latitude: area.latitude, longitude: area.longitude, radiusKm: 300, categoryIds: this.selectedCategoryIds() };

    this.tripsService.getSuggestions(options)
      .subscribe({
        next: (response) => {
          this.places.set(response.places || []);
          this.events.set(response.events || []);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
  }

  openPlace(place: PlaceSummaryDto): void {
    this.router.navigate(['/places', place.id]);
  }

  openEvent(eventItem: EventSummaryDto): void {
    this.router.navigate(['/events', eventItem.id]);
  }
}
