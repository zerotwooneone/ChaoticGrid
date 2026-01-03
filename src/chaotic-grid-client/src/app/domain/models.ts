export enum BoardStatus {
  Draft = 0,
  Active = 1,
  Finished = 2
}

export interface CreateBoardRequest {
  name: string;
}

export interface VoteRequest {
  playerId: string;
  tileId: string;
}

export interface TileDto {
  id: string;
  text: string;
  isApproved: boolean;
}

export interface PlayerDto {
  id: string;
  displayName: string;
  gridTileIds: string[];
  roles: string[];
}

export interface BoardStateDto {
  boardId: string;
  name: string;
  status: BoardStatus;
  minimumApprovedTilesToStart: number;
  tiles: TileDto[];
  players: PlayerDto[];
}

export interface JoinBoardRequest {
  playerId: string;
  displayName: string;
  isHost: boolean;
  seed?: number;
}
