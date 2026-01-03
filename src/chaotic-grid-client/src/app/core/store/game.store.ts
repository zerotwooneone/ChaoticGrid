import { Injectable, computed, signal } from '@angular/core';
import { BoardStateDto, PlayerDto, VoteRequest } from '../../domain/models';

@Injectable({ providedIn: 'root' })
export class GameStore {
  private readonly boardStateInternal = signal<BoardStateDto | null>(null);
  private readonly localPlayerIdInternal = signal<string | null>(null);

  readonly boardState = this.boardStateInternal.asReadonly();
  readonly localPlayerId = this.localPlayerIdInternal.asReadonly();

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

  setLocalPlayer(playerId: string): void {
    this.localPlayerIdInternal.set(playerId);
  }

  clear(): void {
    this.localPlayerIdInternal.set(null);
    this.boardStateInternal.set(null);
  }

  // Placeholder until Match/Vote aggregate exists.
  onVote(_vote: VoteRequest): void {
    // no-op
  }
}
