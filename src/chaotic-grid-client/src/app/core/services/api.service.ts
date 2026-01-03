import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BoardStateDto, CreateBoardRequest, JoinBoardRequest } from '../../domain/models';

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
}
