import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { SignalRService } from './signalr.service';
import { ApiService } from './api.service';
import { GameStore } from '../store/game.store';
import { HUB_CONNECTION_FACTORY } from './hub-connection-factory';

describe('SignalRService', () => {
  it('should call syncState after reconnected', async () => {
    const handlers = new Map<string, (...args: any[]) => void>();
    let onReconnectedHandler: (() => void) | null = null;

    const connection: any = {
      state: 0,
      on: (name: string, cb: (...args: any[]) => void) => {
        handlers.set(name, cb);
      },
      onreconnected: (cb: () => void) => {
        onReconnectedHandler = cb;
      },
      start: async () => {},
      stop: async () => {},
      invoke: async () => {}
    };

    const apiMock = {
      getBoardState: jasmine.createSpy().and.returnValue(of({
        boardId: 'b',
        name: 'n',
        status: 0,
        minimumApprovedTilesToStart: 25,
        tiles: [],
        players: []
      }))
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: ApiService, useValue: apiMock },
        { provide: HUB_CONNECTION_FACTORY, useValue: () => connection }
      ]
    });

    const store = TestBed.inject(GameStore);
    store.setBoardState({
      boardId: 'b',
      name: 'n',
      status: 0,
      minimumApprovedTilesToStart: 25,
      tiles: [],
      players: []
    });

    const svc = TestBed.inject(SignalRService);

    await svc.ensureConnected();

    expect(onReconnectedHandler).withContext('onreconnected should be registered').not.toBeNull();

    onReconnectedHandler!();

    expect(apiMock.getBoardState).toHaveBeenCalled();
  });
});
