import { Injectable } from '@angular/core';
import { CategoryTreeDto } from '../models/category.model';
import { CATEGORY_BEAR_ICONS } from '../../shared/components/bear-icon/bear-icons';

// Bear icon (see shared/components/bear-icon) per parent category
const PARENT_SLUG_ICONS = CATEGORY_BEAR_ICONS;

const DEFAULT_ICON = 'paw';

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

// Mascot pose per parent category; files live in public/brand/bears/<pose>.png
const PARENT_SLUG_BEAR_POSE: Record<string, string> = {
  'movement': 'yoga',
  'spirituality': 'meditation',
  'food': 'food',
  'music-sound': 'music',
  'nature': 'hiking',
  'markets': 'market',
  'healing': 'healing',
  'wellness': 'hot-spring',
  'crafting': 'painting',
  'volunteering': 'planting',
  'co-working': 'laptop',
  'ceremonies': 'campfire',
  'sexuality': 'heart',
  'events': 'dancing'
};

const BEAR_IMAGE_PREFIX = 'bear-';

@Injectable({
  providedIn: 'root'
})
export class CategoryIconsService {
  private categoryIdToIcon = new Map<string, string>();
  private categoryIdToName = new Map<string, string>();
  private categoryIdToEmoji = new Map<string, string>();
  private categoryIdToPinColor = new Map<string, string>();
  private categoryIdToBearPose = new Map<string, string>();

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
    this.categoryIdToBearPose.clear();
    for (const parent of parents) {
      const slug = parent.slug?.toLowerCase() ?? '';
      const icon = this.getIconForParentSlug(parent.slug);
      const emoji = this.getEmojiForParentSlug(parent.slug);
      const pinColor = PARENT_SLUG_PIN_COLOR[slug] ?? DEFAULT_PIN_COLOR;
      const bearPose = PARENT_SLUG_BEAR_POSE[slug];
      for (const categoryId of [parent.id, ...(parent.children || []).map(child => child.id)]) {
        this.categoryIdToIcon.set(categoryId, icon);
        this.categoryIdToEmoji.set(categoryId, emoji);
        this.categoryIdToPinColor.set(categoryId, pinColor);
        if (bearPose) this.categoryIdToBearPose.set(categoryId, bearPose);
      }
      this.categoryIdToName.set(parent.id, parent.name);
      for (const child of parent.children || []) this.categoryIdToName.set(child.id, child.name);
    }
  }

  /** Map image id of the mascot pose for a category, or '' when the category has no pose (emoji is used instead). */
  getBearImageIdForCategoryId(categoryId: string | null | undefined): string {
    const pose = categoryId ? this.categoryIdToBearPose.get(categoryId) : undefined;
    return pose ? BEAR_IMAGE_PREFIX + pose : '';
  }

  /** Every pose used on the map, as [map image id, image url] pairs for one-time registration. */
  getAllBearPinImages(): [string, string][] {
    return [...new Set(Object.values(PARENT_SLUG_BEAR_POSE))]
      .map(pose => [BEAR_IMAGE_PREFIX + pose, `brand/bears/${pose}.png`]);
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
