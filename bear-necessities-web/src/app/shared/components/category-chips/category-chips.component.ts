import {
  Component, Input, Output, EventEmitter, signal, HostListener,
  ElementRef, ViewChild, AfterViewInit
} from '@angular/core';
import { CategoryTreeDto } from '../../../core/models/category.model';
import { CategoryIconsService } from '../../../core/services/category-icons.service';

const DROPDOWN_WIDTH = 224;
const DROPDOWN_MIN_HEIGHT = 200;
const DROPDOWN_EDGE_GAP = 12;

@Component({
  selector: 'app-category-chips',
  templateUrl: './category-chips.component.html',
  styleUrls: ['./category-chips.component.scss'],
  standalone: false
})
export class CategoryChipsComponent implements AfterViewInit {
  @ViewChild('scroller') private scroller?: ElementRef<HTMLElement>;

  @Input() set categories(parentCategories: CategoryTreeDto[]) {
    this._categories = parentCategories;
    // Content width changes with the category list — recompute the scroll affordances
    queueMicrotask(() => this.updateScrollHints());
  }
  get categories(): CategoryTreeDto[] {
    return this._categories;
  }
  private _categories: CategoryTreeDto[] = [];

  // Lets a parent re-hydrate the highlighted selection (e.g. map filter that survives
  // navigation in PlacesService) so the chips reflect the active filter after re-mount.
  @Input() set selectedIds(ids: string[] | null | undefined) {
    const next = ids ?? [];
    if (next.join('|') !== this.selectedCategoryIds().join('|')) {
      this.selectedCategoryIds.set([...next]);
    }
  }

  // Narrow containers (e.g. the trip planner panel) wrap chips onto several lines so every category stays visible
  @Input() wrap = false;

  @Output() selectionChange = new EventEmitter<string[]>();

  selectedCategoryIds = signal<string[]>([]);
  openDropdownId = signal<string | null>(null);

  // The dropdown is rendered with position:fixed so the horizontally-scrolling chip row
  // (overflow-x) can't clip it. Its screen position is computed from the clicked chip.
  dropdownTop = signal<number | null>(0);
  dropdownBottom = signal<number | null>(null);
  dropdownMaxHeight = signal(DROPDOWN_MIN_HEIGHT);
  dropdownLeft = signal(0);

  canScrollLeft = signal(false);
  canScrollRight = signal(false);

  constructor(
    private elementRef: ElementRef,
    private categoryIcons: CategoryIconsService
  ) {}

  ngAfterViewInit(): void {
    this.updateScrollHints();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(clickEvent: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(clickEvent.target)) {
      this.openDropdownId.set(null);
    }
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.openDropdownId.set(null);
    this.updateScrollHints();
  }

  getIconForCategory(category: CategoryTreeDto): string {
    return this.categoryIcons.getIconForParentSlug(category?.slug || category?.icon);
  }

  toggleDropdown(parentId: string, clickEvent: Event): void {
    clickEvent.stopPropagation();
    if (this.openDropdownId() === parentId) {
      this.openDropdownId.set(null);
      return;
    }
    const chip = (clickEvent.currentTarget as HTMLElement).getBoundingClientRect();
    const spaceBelow = window.innerHeight - chip.bottom - DROPDOWN_EDGE_GAP;
    const spaceAbove = chip.top - DROPDOWN_EDGE_GAP;
    // Chips low on screen (e.g. inside the planner panel) open upward so the list never runs off the bottom
    const opensUpward = spaceBelow < DROPDOWN_MIN_HEIGHT && spaceAbove > spaceBelow;
    this.dropdownTop.set(opensUpward ? null : chip.bottom + 6);
    this.dropdownBottom.set(opensUpward ? window.innerHeight - chip.top + 6 : null);
    // Use all the room on screen so a full subcategory list shows without an inner scrollbar
    this.dropdownMaxHeight.set(Math.max(DROPDOWN_MIN_HEIGHT, opensUpward ? spaceAbove : spaceBelow));
    this.dropdownLeft.set(Math.max(8, Math.min(chip.left, window.innerWidth - DROPDOWN_WIDTH - 8)));
    this.openDropdownId.set(parentId);
  }

  isDropdownOpen(parentId: string): boolean {
    return this.openDropdownId() === parentId;
  }

  // Scrolling the chip row would leave a fixed dropdown stranded, so close it and refresh hints.
  onScrollerScroll(): void {
    if (this.openDropdownId()) this.openDropdownId.set(null);
    this.updateScrollHints();
  }

  scrollChips(direction: -1 | 1): void {
    this.scroller?.nativeElement.scrollBy({ left: direction * 240, behavior: 'smooth' });
  }

  private updateScrollHints(): void {
    const element = this.scroller?.nativeElement;
    if (!element || this.wrap) return;
    const maxScroll = element.scrollWidth - element.clientWidth;
    this.canScrollLeft.set(element.scrollLeft > 4);
    this.canScrollRight.set(element.scrollLeft < maxScroll - 4);
  }

  clearAll(): void {
    this.selectedCategoryIds.set([]);
    this.openDropdownId.set(null);
    this.selectionChange.emit([]);
  }

  isAllSelected(): boolean {
    return this.selectedCategoryIds().length === 0;
  }

  toggleChild(childId: string, clickEvent: Event): void {
    clickEvent.stopPropagation();
    const current = this.selectedCategoryIds();
    const updated = current.includes(childId)
      ? current.filter(id => id !== childId)
      : [...current, childId];

    this.selectedCategoryIds.set(updated);
    this.selectionChange.emit(updated);
  }

  isChildSelected(childId: string): boolean {
    return this.selectedCategoryIds().includes(childId);
  }

  // Selection only ever holds child ids — the parent acts purely as a grouping label,
  // so unchecking every child clears the parent's highlight and its results.
  toggleEntireParent(parent: CategoryTreeDto, clickEvent: Event): void {
    clickEvent.stopPropagation();
    const childIds = this.getChildIds(parent);
    if (childIds.length === 0) return;

    const current = this.selectedCategoryIds();
    const isFullySelected = childIds.every(id => current.includes(id));

    const updated = isFullySelected
      ? current.filter(id => !childIds.includes(id))
      : [...new Set([...current, ...childIds])];

    this.selectedCategoryIds.set(updated);
    this.selectionChange.emit(updated);
  }

  isEntireParentSelected(parent: CategoryTreeDto): boolean {
    const childIds = this.getChildIds(parent);
    const current = this.selectedCategoryIds();
    return childIds.length > 0 && childIds.every(id => current.includes(id));
  }

  getSelectedChildCount(parent: CategoryTreeDto): number {
    const current = this.selectedCategoryIds();
    return this.getChildIds(parent).filter(id => current.includes(id)).length;
  }

  hasSelectionInParent(parent: CategoryTreeDto): boolean {
    return this.getSelectedChildCount(parent) > 0;
  }

  private getChildIds(parent: CategoryTreeDto): string[] {
    return (parent.children || []).map(child => child.id);
  }
}
