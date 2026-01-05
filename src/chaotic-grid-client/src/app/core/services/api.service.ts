import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  BoardStateDto,
  CreateBoardRequest,
  CreateRoleTemplateRequest,
  JoinBoardRequest,
  MySystemContextDto,
  PlayerContextDto,
  RoleTemplateDto,
  TileDto,
  UpdatePermissionOverrideRequest,
  UpdateRoleTemplateRequest
} from '../../domain/models';
import { AuthStatusResponse, SetupRequest, SetupResponse } from '../../domain/auth-models';
import { ModerateTileRequest, SuggestTileRequest } from '../../domain/tile-models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  createBoard(name: string): Observable<BoardStateDto> {
    const body: CreateBoardRequest = { name };
    return this.http.post<BoardStateDto>('/boards', body);
  }

  joinBoard(boardId: string, request: JoinBoardRequest): Observable<BoardStateDto> {
    return this.http.post<BoardStateDto>(`/boards/${boardId}/join`, request);
  }

  getBoardState(boardId: string): Observable<BoardStateDto> {
    return this.http.get<BoardStateDto>(`/boards/${boardId}`);
  }

  getAuthStatus(): Observable<AuthStatusResponse> {
    return this.http.get<AuthStatusResponse>('/auth/status');
  }

  setup(request: SetupRequest): Observable<SetupResponse> {
    return this.http.post<SetupResponse>('/auth/setup', request);
  }

  joinByInvite(token: string): Observable<BoardStateDto> {
    return this.http.post<BoardStateDto>('/boards/join', { token });
  }

  suggestTile(request: SuggestTileRequest): Observable<TileDto> {
    return this.http.post<TileDto>('/tiles', request);
  }

  moderateTile(tileId: string, request: ModerateTileRequest): Observable<TileDto> {
    return this.http.put<TileDto>(`/tiles/${tileId}`, request);
  }

  getTiles(boardId: string): Observable<TileDto[]> {
    return this.http.get<TileDto[]>(`/tiles?boardId=${encodeURIComponent(boardId)}`);
  }

  startBoard(boardId: string): Observable<BoardStateDto> {
    return this.http.post<BoardStateDto>(`/boards/${boardId}/start`, {});
  }

  getMyBoardContext(boardId: string): Observable<PlayerContextDto> {
    return this.http.get<PlayerContextDto>(`/api/boards/${boardId}/my-context`);
  }

  updateMyBoardPermissionOverrides(boardId: string, request: UpdatePermissionOverrideRequest): Observable<PlayerContextDto> {
    return this.http.put<PlayerContextDto>(`/api/boards/${boardId}/players/me/permissions`, request);
  }

  getMySystemContext(): Observable<MySystemContextDto> {
    return this.http.get<MySystemContextDto>('/api/me/system-context');
  }

  getMyRoleTemplates(): Observable<RoleTemplateDto[]> {
    return this.http.get<RoleTemplateDto[]>('/api/me/role-templates');
  }

  createRoleTemplate(request: CreateRoleTemplateRequest): Observable<RoleTemplateDto> {
    return this.http.post<RoleTemplateDto>('/api/me/role-templates', request);
  }

  updateRoleTemplate(templateId: string, request: UpdateRoleTemplateRequest): Observable<RoleTemplateDto> {
    return this.http.put<RoleTemplateDto>(`/api/me/role-templates/${templateId}`, request);
  }

  deleteRoleTemplate(templateId: string): Observable<void> {
    return this.http.delete<void>(`/api/me/role-templates/${templateId}`);
  }
}
