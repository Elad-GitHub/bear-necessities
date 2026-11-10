export interface ReviewDto {
  id: string;
  placeId: string;
  userId: string;
  userEmail: string;
  rating: number;
  comment: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateReviewRequest {
  placeId: string;
  rating: number;
  comment: string;
}
