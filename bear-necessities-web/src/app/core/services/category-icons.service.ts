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

// Pin fill per parent category, drawn from the festival-map palette (darkened where white needs contrast)
const PARENT_SLUG_PIN_COLOR: Record<string, string> = {
  'movement': '#00796b',
  'spirituality': '#00796b',
  'events': '#F57C00',
  'nature': '#F57C00',
  'ceremonies': '#E65100',
  'food': '#7CB342',
  'volunteering': '#7CB342',
  'wellness': '#26A69A',
  'healing': '#26A69A',
  'co-working': '#4DB6AC',
  'music-sound': '#EC407A',
  'sexuality': '#F06292',
  'markets': '#F9A825',
  'crafting': '#F9A825'
};

const DEFAULT_PIN_COLOR = '#00796b';

@Injectable({
  providedIn: 'root'
})
export class CategoryIconsService {
  private categoryIdToIcon = new Map<string, string>();
  private categoryIdToName = new Map<string, string>();
  private categoryIdToEmoji = new Map<string, string>();
  private categoryIdToPinColor = new Map<string, string>();

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
    this.categoryIdToPinColor.clear();
    for (const parent of parents) {
      const icon = this.getIconForParentSlug(parent.slug);
      const emoji = this.getEmojiForParentSlug(parent.slug);
      const pinColor = PARENT_SLUG_PIN_COLOR[parent.slug?.toLowerCase() ?? ''] ?? DEFAULT_PIN_COLOR;
      this.categoryIdToIcon.set(parent.id, icon);
      this.categoryIdToName.set(parent.id, parent.name);
      this.categoryIdToEmoji.set(parent.id, emoji);
      this.categoryIdToPinColor.set(parent.id, pinColor);
      for (const child of parent.children || []) {
        this.categoryIdToIcon.set(child.id, icon);
        this.categoryIdToName.set(child.id, child.name);
        this.categoryIdToEmoji.set(child.id, emoji);
        this.categoryIdToPinColor.set(child.id, pinColor);
      }
    }
  }

  getPinColorForCategoryId(categoryId: string | null | undefined): string {
    if (!categoryId) return DEFAULT_PIN_COLOR;
    return this.categoryIdToPinColor.get(categoryId) ?? DEFAULT_PIN_COLOR;
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
