export interface FieldChangeDto {
  field: string;
  current?: string | null;
  proposed?: string | null;
}

export interface EditSuggestionDto {
  id: string;
  targetType: 'Place' | 'Event';
  targetId: string;
  targetName: string;
  suggestedBy: string;
  createdAt: string;
  changes: FieldChangeDto[];
}
