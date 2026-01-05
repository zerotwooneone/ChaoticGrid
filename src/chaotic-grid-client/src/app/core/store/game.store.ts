import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { BoardStateDto, CompletionVoteStartedDto, PlayerContextDto, PlayerDto, TileDto, VoteRequest } from '../../domain/models';
import { ApiService } from '../services/api.service';

@Injectable({ providedIn: 'root' })
export class GameStore {
  private readonly api = inject(ApiService);

  private readonly boardStateInternal = signal<BoardStateDto | null>(null);
  private readonly localPlayerIdInternal = signal<string | null>(null);
  private readonly pendingVotesInternal = signal<VoteRequest[]>([]);
  private readonly pendingCompletionVotesInternal = signal<CompletionVoteStartedDto[]>([]);
  private readonly playerContextInternal = signal<PlayerContextDto | null>(null);

  readonly boardState = this.boardStateInternal.asReadonly();
  readonly localPlayerId = this.localPlayerIdInternal.asReadonly();
  readonly pendingVotes = this.pendingVotesInternal.asReadonly();
  readonly pendingCompletionVotes = this.pendingCompletionVotesInternal.asReadonly();
  readonly playerContext = this.playerContextInternal.asReadonly();

  readonly boardId = computed(() => this.boardStateInternal()?.boardId ?? null);

  readonly players = computed(() => this.boardStateInternal()?.players ?? []);
  readonly tiles = computed(() => this.boardStateInternal()?.tiles ?? []);

  readonly localPlayer = computed<PlayerDto | null>(() => {
    const state = this.boardStateInternal();
    const playerId = this.localPlayerIdInternal();
    if (!state || !playerId) {
      return null;
    }

    return state.players.find(p => p.id === playerId) ?? null;
  });

  readonly gridTileIds = computed(() => this.localPlayer()?.gridTileIds ?? []);

  setBoardState(state: BoardStateDto): void {
    this.boardStateInternal.set(state);
  }

  setPlayerContext(ctx: PlayerContextDto | null): void {
    this.playerContextInternal.set(ctx);
  }

  async syncState(): Promise<void> {
    const boardId = this.boardId();
    if (!boardId) {
      return;
    }

    const state = await firstValueFrom(this.api.getBoardState(boardId));
    this.setBoardState(state);
  }

  async syncPlayerContext(boardId?: string): Promise<void> {
    const id = boardId ?? this.boardId();
    if (!id) {
      return;
    }

    try {
      const ctx = await firstValueFrom(this.api.getMyBoardContext(id));
      this.setPlayerContext(ctx);
    } catch {
      // Ignore; next refresh will correct.
    }
  }

  upsertTile(tile: TileDto): void {
    this.boardStateInternal.update(s => {
      if (!s) {
        return s;
      }

      const idx = s.tiles.findIndex(t => t.id === tile.id);
      const tiles = idx >= 0
        ? s.tiles.map(t => (t.id === tile.id ? tile : t))
        : [...s.tiles, tile];

      return { ...s, tiles };
    });
  }

  setLocalPlayer(playerId: string): void {
    this.localPlayerIdInternal.set(playerId);
  }

  clear(): void {
    this.localPlayerIdInternal.set(null);
    this.boardStateInternal.set(null);
    this.pendingVotesInternal.set([]);
    this.pendingCompletionVotesInternal.set([]);
    this.playerContextInternal.set(null);
  }

  onVoteRequested(vote: VoteRequest): void {
    this.pendingVotesInternal.update(v => [...v, vote]);
  }

  onCompletionVoteRequested(vote: CompletionVoteStartedDto): void {
    this.pendingCompletionVotesInternal.update(v => [...v, vote]);
  }

  markTileConfirmed(tileId: string): void {
    this.boardStateInternal.update(s => {
      if (!s) {
        return s;
      }

      const tiles = s.tiles.map(t => (t.id === tileId ? { ...t, isConfirmed: true } : t));
      return { ...s, tiles };
    });
  }

  onVote(_vote: VoteRequest): void {
    // no-op
  }
}
