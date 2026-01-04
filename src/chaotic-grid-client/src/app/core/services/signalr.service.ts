import { Inject, Injectable } from '@angular/core';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { BoardStateDto, CompletionVoteRequest, CompletionVoteStartedDto, TileConfirmedDto, TileDto, VoteRequest } from '../../domain/models';
import { ApiService } from './api.service';
import { GameStore } from '../store/game.store';
import { HUB_CONNECTION_FACTORY, HubConnectionFactory } from './hub-connection-factory';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: HubConnection | null = null;

  constructor(
    private readonly store: GameStore,
    private readonly api: ApiService,
    @Inject(HUB_CONNECTION_FACTORY) private readonly createConnection: HubConnectionFactory
  ) {}

  async ensureConnected(): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      return;
    }

    if (!this.connection) {
      this.connection = this.createConnection();

      this.connection.onreconnected(() => {
        void this.syncState();
      });

      this.connection.on('BoardStateUpdated', (state: BoardStateDto) => {
        this.store.setBoardState(state);
      });

      this.connection.on('VoteCast', (vote: VoteRequest) => {
        this.store.onVote(vote);
      });

      this.connection.on('VoteRequested', (vote: CompletionVoteStartedDto) => {
        this.store.onCompletionVoteRequested(vote);
      });

      this.connection.on('TileConfirmed', (tile: TileConfirmedDto) => {
        this.store.markTileConfirmed(tile.tileId);
      });

      this.connection.on('TileSuggested', (tile: TileDto) => {
        this.store.upsertTile(tile);
      });

      this.connection.on('TileModerated', (tile: TileDto) => {
        this.store.upsertTile(tile);
      });

      this.connection.on('GameStarted', (state: BoardStateDto) => {
        this.store.setBoardState(state);
      });
    }

    await this.connection.start();
  }

  async syncState(): Promise<void> {
    const boardId = this.store.boardId();
    if (!boardId) {
      return;
    }

    try {
      const state = await firstValueFrom(this.api.getBoardState(boardId));
      this.store.setBoardState(state);
    } catch {
      // ignore; next server push will correct state
    }
  }

  async disconnect(): Promise<void> {
    if (!this.connection) {
      return;
    }

    if (this.connection.state !== HubConnectionState.Disconnected) {
      await this.connection.stop();
    }
  }

  async joinBoard(boardId: string, displayName: string, isHost: boolean, seed?: number): Promise<string> {
    await this.ensureConnected();

    const playerId = await this.connection!.invoke<string>('JoinBoard', boardId, displayName, isHost, seed ?? null);
    this.store.setLocalPlayer(playerId);
    return playerId;
  }

  async proposeTile(boardId: string, text: string): Promise<void> {
    await this.ensureConnected();

    await this.connection!.invoke('ProposeTile', boardId, text);
  }

  async castVote(boardId: string, tileId: string): Promise<void> {
    await this.ensureConnected();
    await this.connection!.invoke('CastVote', boardId, tileId);
  }

  async proposeCompletion(boardId: string, tileId: string): Promise<void> {
    await this.ensureConnected();

    await this.connection!.invoke('ProposeCompletion', boardId, tileId);
  }

  async castCompletionVote(boardId: string, vote: CompletionVoteRequest): Promise<void> {
    await this.ensureConnected();

    await this.connection!.invoke('CastCompletionVote', boardId, vote);
  }
}
