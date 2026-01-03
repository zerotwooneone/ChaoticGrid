export interface AuthStatusResponse {
  isSetupRequired: boolean;
}

export interface SetupRequest {
  token: string;
  nickname: string;
}

export interface SetupResponse {
  jwt: string;
}
