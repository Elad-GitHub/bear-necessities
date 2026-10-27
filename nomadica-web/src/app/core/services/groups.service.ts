import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface GroupSummaryDto {
  id: string;
  slug: string;
  name: string;
  description?: string;
  city?: string;
  country?: string;
  latitude: number;
  longitude: number;
  memberCount: number;
  isMember: boolean;
  myRole?: string;
}

export interface GroupPostDto {
  id: string;
  authorId: string;
  authorDisplayName: string;
  authorAvatarUrl?: string;
  body: string;
  createdAt: string;
}

export interface CreateGroupRequest {
  name: string;
  description?: string;
  city?: string;
  country?: string;
  latitude: number;
  longitude: number;
}

@Injectable({
  providedIn: 'root'
})
export class GroupsService {
  constructor(private api: ApiService) {}

  getGroups(query?: string, limit = 50): Observable<{ items: GroupSummaryDto[] }> {
    const params: Record<string, any> = { limit };
    if (query) params['query'] = query;
    return this.api.get<{ items: GroupSummaryDto[] }>('/groups', params);
  }

  getMyGroups(): Observable<{ items: GroupSummaryDto[] }> {
    return this.api.get<{ items: GroupSummaryDto[] }>('/groups/mine');
  }

  getById(groupId: string): Observable<GroupSummaryDto> {
    return this.api.get<GroupSummaryDto>(`/groups/${groupId}`);
  }

  create(request: CreateGroupRequest): Observable<{ group: GroupSummaryDto }> {
    return this.api.post<{ group: GroupSummaryDto }>('/groups', request);
  }

  join(groupId: string): Observable<{ isMember: boolean; memberCount: number }> {
    return this.api.post<{ isMember: boolean; memberCount: number }>(`/groups/${groupId}/join`, {});
  }

  leave(groupId: string): Observable<{ isMember: boolean; memberCount: number }> {
    return this.api.post<{ isMember: boolean; memberCount: number }>(`/groups/${groupId}/leave`, {});
  }

  getPosts(groupId: string): Observable<{ items: GroupPostDto[]; nextCursor?: string }> {
    return this.api.get<{ items: GroupPostDto[]; nextCursor?: string }>(`/groups/${groupId}/posts`, { pageSize: 50 });
  }

  createPost(groupId: string, body: string): Observable<{ post: GroupPostDto }> {
    return this.api.post<{ post: GroupPostDto }>(`/groups/${groupId}/posts`, { groupId, body });
  }
}
