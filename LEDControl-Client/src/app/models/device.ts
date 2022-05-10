export class Device {
  id: string;
  name: string;
  mode: DeviceMode;
  hostname: string;
  port: number;
  numLeds: number;
}

export enum DeviceMode{
  Light,
  Pictures
}
