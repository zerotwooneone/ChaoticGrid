import { Injectable } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { BoardStateDto, VoteRequest } from '../../domain/models';
import { GameStore } from '../store/game.store';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: HubConnection | null = null;

  constructor(private readonly store: GameStore) {}

  async ensureConnected(): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      return;
    }

    if (!this.connection) {
      const url = this.getHubUrl();
      this.connection = new HubConnectionBuilder()
        .withUrl(url)
        .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
        .configureLogging(LogLevel.Information)
        .build();

      this.connection.on('BoardStateUpdated', (state: BoardStateDto) => {
        this.store.setBoardState(state);
      });

      this.connection.on('VoteCast', (vote: VoteRequest) => {
        this.store.onVote(vote);
      });
    }

    await this.connection.start();
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
    await this.connection!.invoke('ProposeTile', boardId, text);
  }

  async castVote(boardId: string, vote: VoteRequest): Promise<void> {
    await this.ensureConnected();
    await this.connection!.invoke('CastVote', boardId, vote);
  }

  private getHubUrl(): string {
    // Assumes same origin; dev can use Angular proxy if desired.
    return `${window.location.origin}/hubs/game`;
  }
}
