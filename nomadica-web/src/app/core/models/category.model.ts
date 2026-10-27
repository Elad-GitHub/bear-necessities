export interface CategoryTreeDto {
  id: string;
  slug: string;
  name: string;
  icon?: string;
  color?: string;
  sortOrder?: number;
  children?: CategoryTreeDto[];
}

export interface CategoryDto {
  id: string;
  slug: string;
  name: string;
  icon?: string;
  parentId?: string;
}
