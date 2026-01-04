import { TileStatus } from './models';

export interface SuggestTileRequest {
  boardId: string;
  playerId: string;
  text: string;
}

export interface ModerateTileRequest {
  boardId: string;
  status: TileStatus;
}
