export enum BoardStatus {
  Draft = 0,
  Active = 1,
  Finished = 2
}

export enum TileStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2
}

export interface CreateBoardRequest {
  name: string;
}

export interface VoteRequest {
  playerId: string;
  tileId: string;
}

export interface CompletionVoteRequest {
  tileId: string;
  isYes: boolean;
}

export interface CompletionVoteStartedDto {
  tileId: string;
  proposerId: string;
}

export interface TileConfirmedDto {
  tileId: string;
}

export interface TileDto {
  id: string;
  text: string;
  isApproved: boolean;
  isConfirmed: boolean;
  status: TileStatus;
  createdByUserId: string;
}

export interface PlayerDto {
  id: string;
  displayName: string;
  gridTileIds: string[];
  roles: string[];
  silencedUntilUtc: string | null;
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
