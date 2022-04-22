export class DeviceSettings {
  Id: string;
  Name: string;
  Mode: DeviceMode;
}

export enum DeviceMode{
  Light,
  EQ,
  Pictures
}
