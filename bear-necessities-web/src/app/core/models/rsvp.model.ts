export interface RsvpDto {
  id: string;
  eventId: string;
  userId: string;
  status: 'interested' | 'going' | 'not_going';
  createdAt: string;
  updatedAt: string;
}

export interface CreateOrUpdateRsvpRequest {
  eventId: string;
  status: 'interested' | 'going' | 'not_going';
}

export interface MyRsvpDto {
  eventId: string;
  status: 'interested' | 'going' | 'not_going';
}
