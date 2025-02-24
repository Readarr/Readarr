import ModelBase from 'App/ModelBase';

export type AuthorStatus = 'continuing' | 'ended';

interface Author extends ModelBase {
  added: string;
  genres: string[];
  monitored: boolean;
  overview: string;
  path: string;
  qualityProfileId: number;
  metadataProfileId: number;
  rootFolderPath: string;
  sortName: string;
  status: AuthorStatus;
  tags: number[];
  authorName: string;
  isSaving?: boolean;
}

export default Author;
