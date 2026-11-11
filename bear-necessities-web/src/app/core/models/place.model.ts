export interface MediaDto {
  id: string;
  url: string;
  type: 'image' | 'video';
}

export interface ReviewDto {
  id: string;
  authorName: string;
  rating: number;
  text: string;
  createdAt: string;
}

export interface PlaceDensityCell {
  latitude: number;
  longitude: number;
  count: number;
  /** [west, south, east, north] of the places inside the cell */
  bounds: [number, number, number, number];
}

export interface PlaceDensityResponse {
  cells: PlaceDensityCell[];
  totalCount: number;
}

export interface PlaceSummaryDto {
  id: string;
  name: string;
  description?: string;
  address?: string;
  latitude: number;
  longitude: number;
  city?: string;
  country?: string;
  status?: string;
  categoryId?: string;
  categoryName?: string;
  categoryIds?: string[];
  imageUrl?: string;
  thumbnailUrl?: string;
  averageRating?: number;
  reviewCount?: number;
  isPermanent?: boolean;
  createdAt?: string;
  nextEvent?: PlaceNextEventDto | null;
}

/** The soonest happening at a place; no startAt means it's ongoing. */
export interface PlaceNextEventDto {
  id: string;
  title: string;
  startAt?: string | null;
}

export interface PlaceDetailDto {
  id: string;
  name: string;
  description: string;
  address: string;
  latitude: number;
  longitude: number;
  categoryId: string;
  categoryName: string;
  imageUrl: string;
  website?: string;
  facebook?: string;
  instagram?: string;
  phoneNumber?: string;
  city?: string;
  country?: string;
  timezone?: string;
  isVerified?: boolean;
  seasonality?: 'permanent' | 'seasonal';
  averageRating?: number;
  reviewCount?: number;
  media?: MediaDto[];
  /** A freely licensed photo hosted elsewhere, shown with its credit (uploaded media come first) */
  photoUrl?: string;
  photoCredit?: string;
  photoCreditUrl?: string;
  reviews?: ReviewDto[];
  createdBy?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreatePlaceRequest {
  name: string;
  description: string;
  address: string;
  latitude: number;
  longitude: number;
  categoryId: string;
  imageUrl?: string;
  website?: string;
  facebook?: string;
  instagram?: string;
  phoneNumber?: string;
}

export interface UpdatePlaceRequest {
  name?: string;
  description?: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  categoryId?: string;
  imageUrl?: string;
  website?: string;
  facebook?: string;
  instagram?: string;
  phoneNumber?: string;
}
