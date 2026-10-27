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

export interface PlaceSummaryDto {
  id: string;
  name: string;
  description?: string;
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
