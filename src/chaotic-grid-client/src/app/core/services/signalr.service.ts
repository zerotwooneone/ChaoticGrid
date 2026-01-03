import { Inject, Injectable } from '@angular/core';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { BoardStateDto, VoteRequest } from '../../domain/models';
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

      this.connection.on('VoteRequested', (vote: VoteRequest) => {
        this.store.onVoteRequested(vote);
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

  async joinBoard(boardId: string, playerId: string, displayName: string, isHost: boolean, seed?: number): Promise<void> {
    await this.ensureConnected();
    this.store.setLocalPlayer(playerId);

    await this.connection!.invoke('JoinBoard', boardId, playerId, displayName, isHost, seed ?? null);
  }

  async proposeTile(boardId: string, text: string): Promise<void> {
    await this.ensureConnected();

    const playerId = this.store.localPlayerId();
    if (!playerId) {
      throw new Error('Cannot propose tile without a local player id.');
    }

    await this.connection!.invoke('ProposeTile', boardId, playerId, text);
  }

  async castVote(boardId: string, vote: VoteRequest): Promise<void> {
    await this.ensureConnected();
    await this.connection!.invoke('CastVote', boardId, vote);
  }
}
