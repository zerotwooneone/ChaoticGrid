import { InjectionToken } from '@angular/core';
import { HubConnection } from '@microsoft/signalr';

export type HubConnectionFactory = () => HubConnection;

export const HUB_CONNECTION_FACTORY = new InjectionToken<HubConnectionFactory>('HUB_CONNECTION_FACTORY');
