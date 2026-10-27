import { Injectable } from '@angular/core';
import { CategoryTreeDto } from '../models/category.model';

const PARENT_SLUG_ICONS: Record<string, string> = {
  'movement': 'directions_run',
  'events': 'event',
  'volunteering': 'volunteer_activism',
  'co-working': 'laptop',
  'crafting': 'palette',
  'healing': 'medical_services',
  'music-sound': 'music_note',
  'sexuality': 'favorite',
  'spirituality': 'self_improvement',
  'wellness': 'ac_unit',
  'nature': 'forest',
  'ceremonies': 'local_fire_department',
  'markets': 'storefront',
  'food': 'restaurant'
};

const DEFAULT_ICON = 'place';

// Emoji used for map pins (rendered to Mapbox images). Kept parallel to the Material
// icon map so both share one source of truth for "what does this category look like".
const PARENT_SLUG_EMOJI: Record<string, string> = {
  'movement': '🧘',
  'events': '🎉',
  'volunteering': '🌱',
  'co-working': '💻',
  'crafting': '🎨',
  'healing': '💆',
  'music-sound': '🎵',
  'sexuality': '❤️',
  'spirituality': '🔮',
  'wellness': '🌿',
  'nature': '🏕️',
  'ceremonies': '🔥',
  'markets': '🛍️',
  'food': '🥗'
};

const DEFAULT_EMOJI = '📍';

@Injectable({
  providedIn: 'root'
})
export class CategoryIconsService {
  private categoryIdToIcon = new Map<string, string>();
  private categoryIdToName = new Map<string, string>();
  private categoryIdToEmoji = new Map<string, string>();

  getIconForParentSlug(slug: string | null | undefined): string {
    if (!slug) return DEFAULT_ICON;
    return PARENT_SLUG_ICONS[slug.toLowerCase()] ?? DEFAULT_ICON;
  }

  getEmojiForParentSlug(slug: string | null | undefined): string {
    if (!slug) return DEFAULT_EMOJI;
    return PARENT_SLUG_EMOJI[slug.toLowerCase()] ?? DEFAULT_EMOJI;
  }

  /** Every distinct pin emoji, including the fallback — used to pre-register map images once. */
  getAllPinEmojis(): string[] {
    return [...new Set([...Object.values(PARENT_SLUG_EMOJI), DEFAULT_EMOJI])];
  }

  /** Index every category id (parents and children) to its parent's icon, emoji and its own display name. */
  buildIndex(parents: CategoryTreeDto[]): void {
    this.categoryIdToIcon.clear();
    this.categoryIdToName.clear();
    this.categoryIdToEmoji.clear();
    for (const parent of parents) {
      const icon = this.getIconForParentSlug(parent.slug);
      const emoji = this.getEmojiForParentSlug(parent.slug);
      this.categoryIdToIcon.set(parent.id, icon);
      this.categoryIdToName.set(parent.id, parent.name);
      this.categoryIdToEmoji.set(parent.id, emoji);
      for (const child of parent.children || []) {
        this.categoryIdToIcon.set(child.id, icon);
        this.categoryIdToName.set(child.id, child.name);
        this.categoryIdToEmoji.set(child.id, emoji);
      }
    }
  }

  getIconForCategoryId(categoryId: string | null | undefined): string {
    if (!categoryId) return DEFAULT_ICON;
    return this.categoryIdToIcon.get(categoryId) ?? DEFAULT_ICON;
  }

  getEmojiForCategoryId(categoryId: string | null | undefined): string {
    if (!categoryId) return DEFAULT_EMOJI;
    return this.categoryIdToEmoji.get(categoryId) ?? DEFAULT_EMOJI;
  }

  getNameForCategoryId(categoryId: string | null | undefined): string | null {
    if (!categoryId) return null;
    return this.categoryIdToName.get(categoryId) ?? null;
  }
}
