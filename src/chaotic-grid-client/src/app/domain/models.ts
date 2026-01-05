export enum BoardStatus {
  Draft = 0,
  Active = 1,
  Finished = 2
}

export enum BoardPermission {
  None = 0,
  SuggestTile = 1 << 0,
  CastVote = 1 << 1,
  ApproveTile = 1 << 2,
  ModifyBoardSettings = 1 << 3,
  ManageBoardRoles = 1 << 4,
  ForceCompleteTile = 1 << 5,
  SelfCompleteTile = 1 << 6,
  ModifyOwnPermissions = 1 << 7
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
  createdByPlayerId: string;
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
  userId: string;
  displayName: string;
  isHost: boolean;
  seed?: number;
}

export interface PlayerContextDto {
  roleId: string | null;
  roleName: string | null;
  rolePermissions: number;
  allowOverrides: number;
  denyOverrides: number;
  effectivePermissions: number;
}

export interface UpdatePermissionOverrideRequest {
  allowOverrideMask: number;
  denyOverrideMask: number;
}

export interface MySystemContextDto {
  nickname: string;
  systemPermissions: number;
}

export interface RoleTemplateDto {
  id: string;
  name: string;
  defaultBoardPermissions: number;
}

export interface CreateRoleTemplateRequest {
  name: string;
  defaultBoardPermissions: number;
}

export interface UpdateRoleTemplateRequest {
  name: string;
  defaultBoardPermissions: number;
}
