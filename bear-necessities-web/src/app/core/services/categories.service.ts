import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { CategoryTreeDto } from '../models/category.model';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class CategoriesService {
  categories = signal<CategoryTreeDto[]>([]);

  constructor(private api: ApiService) {}

  loadCategories(): Observable<CategoryTreeDto[]> {
    return this.api.get<any>('/categories').pipe(
      tap((response: any) => {
        const cats = Array.isArray(response) ? response : (response?.data || response?.categories || []);

        // Deduplicate by slug, keep the one with sortOrder > 0 (seeded categories)
        const deduped = new Map<string, any>();
        cats.forEach((cat: any) => {
          const slug = cat?.slug || cat?.icon || '';
          if (!slug) return;

          const existing = deduped.get(slug);
          if (!existing || (cat.sortOrder > 0 && (existing.sortOrder || 0) <= 0)) {
            deduped.set(slug, cat);
          }
        });

        const filtered = Array.from(deduped.values()).filter(
          (cat: any) => cat && cat.id && cat.name && (cat.icon || cat.slug) && cat.sortOrder > 0
        );
        this.categories.set(filtered);
      })
    );
  }
}

